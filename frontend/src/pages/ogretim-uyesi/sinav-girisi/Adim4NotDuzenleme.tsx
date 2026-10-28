import { useMemo, useRef, useState } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { ArrowUpDown, ChevronLeft, ChevronRight, FileSpreadsheet, Loader2, Pencil, Plus, Search, Upload, X } from 'lucide-react'
import Modal from '../../../components/Modal'
import { Alan, Uyari } from '../../../components/form'
import { api, hataMesaji } from '../../../lib/api'
import { puanYaz, sinavAnahtari, type SinavBilgisi } from '../../../lib/sinav'

interface Ekleme {
  id: number
  tur: 'Bonus' | 'ProjeNotu'
  puan: number
  aciklama: string | null
}
interface NotSatiri {
  sonucId: number
  ogrenciNo: string
  adSoyad: string
  sinavaGirdi: boolean
  kitapcik: string | null
  dogru: number
  yanlis: number
  bos: number
  hamPuan: number
  manuelPuan: number | null
  eklemeler: Ekleme[]
  duzeltilmisPuan: number | null
  harfNotu: string | null
}
interface Ozet {
  toplam: number
  giren: number
  ortalama: number | null
  medyan: number | null
  standartSapma: number | null
  enYuksek: number | null
  enDusuk: number | null
}
interface NotListesi {
  maksimumPuan: number
  satirlar: NotSatiri[]
  ozet: Ozet
}

type Siralama = 'ogrenciNo' | 'adSoyad' | 'hamPuan' | 'duzeltilmisPuan'

/** 4. adım: ham puanlar otomatik hesaplanmıştır; elle düzeltme, proje notu ve bonus ile nihai not oluşturulur. */
export default function Adim4NotDuzenleme({ sinav, geri, ileri }: { sinav: SinavBilgisi; geri: () => void; ileri: () => void }) {
  const queryClient = useQueryClient()
  const anahtar = ['notlar', sinav.id]
  const notlar = useQuery({
    queryKey: anahtar,
    queryFn: async () => (await api.get<NotListesi>(`/sinavlar/${sinav.id}/notlar`)).data,
  })
  const [arama, setArama] = useState('')
  const [siralama, setSiralama] = useState<{ alan: Siralama; artan: boolean }>({ alan: 'ogrenciNo', artan: true })
  const [secili, setSecili] = useState<Set<number>>(new Set())
  const [duzenlenen, setDuzenlenen] = useState<NotSatiri | null>(null)
  const [pencere, setPencere] = useState<'proje' | 'bonus' | null>(null)
  const [mesaj, setMesaj] = useState<string | null>(null)

  const guncelle = (d: NotListesi) => queryClient.setQueryData(anahtar, d)

  const eklemeSil = useMutation({
    mutationFn: async (id: number) => (await api.delete<NotListesi>(`/sinavlar/${sinav.id}/notlar/eklemeler/${id}`)).data,
    onSuccess: guncelle,
  })

  const tamamla = useMutation({
    mutationFn: async () => api.post(`/sinavlar/${sinav.id}/notlar/tamamla`),
    onSuccess: () => {
      queryClient.setQueryData(sinavAnahtari(sinav.dersId, sinav.tur), { ...sinav, tamamlananAdim: Math.max(sinav.tamamlananAdim, 4) })
      queryClient.invalidateQueries({ queryKey: ['dersler'] })
      ileri()
    },
  })

  const liste = useMemo(() => {
    const a = arama.trim().toLocaleLowerCase('tr-TR')
    const s = (notlar.data?.satirlar ?? []).filter((x) => !a || `${x.ogrenciNo} ${x.adSoyad}`.toLocaleLowerCase('tr-TR').includes(a))
    const deger = (x: NotSatiri) => (siralama.alan === 'duzeltilmisPuan' ? (x.duzeltilmisPuan ?? -1) : siralama.alan === 'hamPuan' ? (x.sinavaGirdi ? x.hamPuan : -1) : x[siralama.alan])
    return [...s].sort((x, y) => {
      const p = deger(x)
      const q = deger(y)
      const k = typeof p === 'number' && typeof q === 'number' ? p - q : String(p).localeCompare(String(q), 'tr')
      return siralama.artan ? k : -k
    })
  }, [notlar.data, arama, siralama])

  if (notlar.isLoading) return <div className="kart flex justify-center p-10"><Loader2 className="h-6 w-6 animate-spin text-selcuk-mavi" /></div>
  if (notlar.isError || !notlar.data) return <Uyari tur="hata">{hataMesaji(notlar.error)}</Uyari>

  const n = notlar.data
  const girenler = liste.filter((x) => x.sinavaGirdi)
  const tumuSecili = girenler.length > 0 && girenler.every((x) => secili.has(x.sonucId))
  const sirala = (alan: Siralama) => setSiralama((s) => ({ alan, artan: s.alan === alan ? !s.artan : true }))

  const Baslik = ({ alan, children, sag }: { alan: Siralama; children: React.ReactNode; sag?: boolean }) => (
    <th className={`px-3 py-3 font-semibold ${sag ? 'text-right' : ''}`}>
      <button className="inline-flex items-center gap-1 hover:text-selcuk-mavi" onClick={() => sirala(alan)}>
        {children} <ArrowUpDown className={`h-3.5 w-3.5 ${siralama.alan === alan ? 'text-selcuk-mavi' : 'text-slate-400'}`} />
      </button>
    </th>
  )

  return (
    <div className="kart p-5 sm:p-6">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <h2 className="text-lg font-bold text-selcuk-lacivert">Not Düzenleme</h2>
        <div className="flex flex-wrap gap-2">
          <button className="btn-ikincil" onClick={() => setPencere('proje')}><FileSpreadsheet className="h-4 w-4" /> Proje Notları Yükle</button>
          <button className="btn-ikincil" onClick={() => setPencere('bonus')}><Plus className="h-4 w-4" /> Bonus Ekle</button>
        </div>
      </div>

      {mesaj && <div className="mt-4"><Uyari tur="basari">{mesaj}</Uyari></div>}
      {eklemeSil.isError && <div className="mt-4"><Uyari tur="hata">{hataMesaji(eklemeSil.error)}</Uyari></div>}

      <div className="relative mt-4">
        <Search className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-slate-400" />
        <input className="w-full rounded-lg border border-slate-200 bg-selcuk-zemin py-2.5 pl-9 pr-3 text-sm focus:border-selcuk-mavi focus:outline-none focus:ring-2 focus:ring-selcuk-mavi/20"
          placeholder="Öğrenci ara (numara veya isim)…" value={arama} onChange={(e) => setArama(e.target.value)} />
      </div>

      <div className="mt-3 max-h-[560px] overflow-auto rounded-xl border border-slate-200">
        <table className="w-full min-w-[820px] text-left text-sm">
          <thead className="sticky top-0 z-10 bg-amber-50 text-selcuk-metin">
            <tr>
              <th className="w-10 px-3 py-3">
                <input type="checkbox" className="h-4 w-4 accent-selcuk-altin" checked={tumuSecili} aria-label="Tümünü seç"
                  onChange={(e) => setSecili(e.target.checked ? new Set(girenler.map((x) => x.sonucId)) : new Set())} />
              </th>
              <Baslik alan="ogrenciNo">Öğrenci No</Baslik>
              <Baslik alan="adSoyad">Ad Soyad</Baslik>
              <th className="px-3 py-3 text-center font-semibold">D / Y / B</th>
              <Baslik alan="hamPuan" sag>Ham Puan</Baslik>
              <th className="px-3 py-3 font-semibold">Eklemeler</th>
              <Baslik alan="duzeltilmisPuan" sag>Düzeltilmiş Puan</Baslik>
            </tr>
          </thead>
          <tbody>
            {liste.map((x) => (
              <tr key={x.sonucId} className={`border-t border-slate-100 ${!x.sinavaGirdi ? 'bg-slate-50 text-slate-400' : secili.has(x.sonucId) ? 'bg-amber-50/60' : ''}`}>
                <td className="px-3 py-2.5">
                  <input type="checkbox" className="h-4 w-4 accent-selcuk-altin" disabled={!x.sinavaGirdi} checked={secili.has(x.sonucId)}
                    aria-label={`${x.adSoyad} seç`}
                    onChange={(e) => setSecili((s) => { const y = new Set(s); if (e.target.checked) y.add(x.sonucId); else y.delete(x.sonucId); return y })} />
                </td>
                <td className="px-3 py-2.5 font-mono text-xs">{x.ogrenciNo}</td>
                <td className="px-3 py-2.5 font-semibold">{x.adSoyad}</td>
                <td className="px-3 py-2.5 text-center text-xs">{x.sinavaGirdi ? `${x.dogru} / ${x.yanlis} / ${x.bos}` : '—'}</td>
                <td className="px-3 py-2.5 text-right">
                  {x.sinavaGirdi ? <span className="rounded-md bg-slate-100 px-2.5 py-1 font-semibold">{puanYaz(x.hamPuan)}</span> : <span className="text-xs">Girmedi</span>}
                </td>
                <td className="px-3 py-2.5">
                  <div className="flex flex-wrap gap-1">
                    {x.eklemeler.length === 0 && <span className="text-slate-400">-</span>}
                    {x.eklemeler.map((e) => (
                      <span key={e.id} title={`${e.tur === 'Bonus' ? 'Bonus' : 'Proje notu'}${e.aciklama ? `: ${e.aciklama}` : ''}`}
                        className={`inline-flex items-center gap-1 rounded-full px-2 py-0.5 text-xs font-semibold ${e.tur === 'Bonus' ? 'bg-purple-100 text-purple-800' : 'bg-blue-100 text-selcuk-mavi'}`}>
                        {e.puan > 0 ? '+' : ''}{puanYaz(e.puan)}
                        <button aria-label="Eklemeyi sil" className="hover:text-red-600" onClick={() => eklemeSil.mutate(e.id)}><X className="h-3 w-3" /></button>
                      </span>
                    ))}
                  </div>
                </td>
                <td className="px-3 py-2.5 text-right">
                  {x.sinavaGirdi ? (
                    <button onClick={() => setDuzenlenen(x)} title="Puanı elle düzelt"
                      className={`inline-flex items-center gap-1 rounded-md px-2.5 py-1 font-bold ${x.manuelPuan !== null ? 'bg-orange-100 text-orange-800 ring-1 ring-orange-300' : 'bg-amber-100 text-amber-900'} hover:ring-2 hover:ring-selcuk-altin`}>
                      {puanYaz(x.duzeltilmisPuan!)} <Pencil className="h-3 w-3 opacity-60" />
                    </button>
                  ) : '—'}
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
      {secili.size > 0 && <p className="mt-2 text-sm text-selcuk-metin-soluk">{secili.size} öğrenci seçili.</p>}

      <h3 className="mt-6 font-semibold">Özet İstatistikler</h3>
      <div className="mt-2 grid grid-cols-2 gap-3 rounded-xl border border-slate-200 p-4 sm:grid-cols-3 lg:grid-cols-6">
        <Ist etiket="Toplam / Giren" deger={`${n.ozet.giren} / ${n.ozet.toplam}`} />
        <Ist etiket="Ortalama Puan" deger={n.ozet.ortalama} />
        <Ist etiket="Medyan" deger={n.ozet.medyan} />
        <Ist etiket="Std. Sapma" deger={n.ozet.standartSapma} />
        <Ist etiket="En Yüksek Puan" deger={n.ozet.enYuksek} />
        <Ist etiket="En Düşük Puan" deger={n.ozet.enDusuk} />
      </div>

      <div className="mt-6 flex justify-between">
        <button className="btn-ikincil" onClick={geri}><ChevronLeft className="h-4 w-4" /> Geri</button>
        <button className="btn-birincil" disabled={tamamla.isPending} onClick={() => tamamla.mutate()}>
          {tamamla.isPending && <Loader2 className="h-4 w-4 animate-spin" />} İleri <ChevronRight className="h-4 w-4" />
        </button>
      </div>

      {duzenlenen && <ManuelPuan sinav={sinav} satir={duzenlenen} maks={n.maksimumPuan} kapat={() => setDuzenlenen(null)} tamam={guncelle} />}
      {pencere === 'bonus' && (
        <BonusPenceresi sinav={sinav} secili={[...secili]} kapat={() => setPencere(null)}
          tamam={(d, adet) => { guncelle(d); setSecili(new Set()); setMesaj(`${adet} öğrenciye bonus eklendi.`) }} />
      )}
      {pencere === 'proje' && (
        <ProjePenceresi sinav={sinav} kapat={() => setPencere(null)}
          tamam={(m) => { queryClient.invalidateQueries({ queryKey: anahtar }); setMesaj(m) }} />
      )}
    </div>
  )
}

function Ist({ etiket, deger }: { etiket: string; deger: number | string | null }) {
  return (
    <div>
      <div className="text-xs text-selcuk-metin-soluk">{etiket}</div>
      <div className="text-xl font-bold text-selcuk-lacivert">{deger === null ? '—' : typeof deger === 'number' ? puanYaz(deger) : deger}</div>
    </div>
  )
}

function ManuelPuan({ sinav, satir, maks, kapat, tamam }: {
  sinav: SinavBilgisi; satir: NotSatiri; maks: number; kapat: () => void; tamam: (d: NotListesi) => void
}) {
  const [puan, setPuan] = useState(String(satir.manuelPuan ?? satir.hamPuan))
  const kaydet = useMutation({
    mutationFn: async (deger: number | null) =>
      (await api.put<NotListesi>(`/sinavlar/${sinav.id}/notlar/${satir.sonucId}/manuel`, { puan: deger })).data,
    onSuccess: (d) => { tamam(d); kapat() },
  })
  return (
    <Modal baslik="Puanı Elle Düzelt" aciklama={`${satir.ogrenciNo} · ${satir.adSoyad}`} kapat={kapat}>
      <div className="space-y-4">
        {kaydet.isError && <Uyari tur="hata">{hataMesaji(kaydet.error)}</Uyari>}
        <p className="text-sm text-selcuk-metin-soluk">
          Cevap anahtarından hesaplanan ham puan: <b>{puanYaz(satir.hamPuan)}</b> ({satir.dogru} doğru, {satir.yanlis} yanlış, {satir.bos} boş).
          İtiraz veya iptal edilen soru gibi durumlarda puanı düzeltebilirsiniz; eklemeler bu puanın üzerine eklenir.
        </p>
        <Alan etiket={`Yeni puan (0 – ${puanYaz(maks)})`} type="number" min={0} max={maks} step={0.5} value={puan} onChange={(e) => setPuan(e.target.value)} autoFocus />
        <div className="flex flex-wrap justify-between gap-2">
          {satir.manuelPuan !== null ? (
            <button className="btn-ikincil" disabled={kaydet.isPending} onClick={() => kaydet.mutate(null)}>Ham puana dön</button>
          ) : <span />}
          <div className="flex gap-2">
            <button className="btn-ikincil" onClick={kapat}>Vazgeç</button>
            <button className="btn-birincil" disabled={kaydet.isPending || puan === ''} onClick={() => kaydet.mutate(Number(puan))}>Kaydet</button>
          </div>
        </div>
      </div>
    </Modal>
  )
}

function BonusPenceresi({ sinav, secili, kapat, tamam }: {
  sinav: SinavBilgisi; secili: number[]; kapat: () => void; tamam: (d: NotListesi, adet: number) => void
}) {
  const [puan, setPuan] = useState('5')
  const [aciklama, setAciklama] = useState('')
  const kaydet = useMutation({
    mutationFn: async () =>
      (await api.post<NotListesi>(`/sinavlar/${sinav.id}/notlar/bonus`, { sonucIdleri: secili, puan: Number(puan), aciklama })).data,
    onSuccess: (d) => { tamam(d, secili.length); kapat() },
  })
  return (
    <Modal baslik="Bonus Ekle" aciklama={secili.length ? `${secili.length} seçili öğrenciye eklenecek.` : 'Önce tablodan öğrenci seçin.'} kapat={kapat}>
      <div className="space-y-4">
        {secili.length === 0 && <Uyari tur="uyari">Bonus eklemek için tablodaki kutucuklarla öğrenci seçin (başlıktaki kutu tümünü seçer).</Uyari>}
        {kaydet.isError && <Uyari tur="hata">{hataMesaji(kaydet.error)}</Uyari>}
        <Alan etiket="Bonus puan" type="number" step={0.5} value={puan} onChange={(e) => setPuan(e.target.value)} />
        <Alan etiket="Açıklama (isteğe bağlı)" placeholder="Ör. derse katılım" value={aciklama} onChange={(e) => setAciklama(e.target.value)} />
        <div className="flex justify-end gap-2">
          <button className="btn-ikincil" onClick={kapat}>Vazgeç</button>
          <button className="btn-birincil" disabled={secili.length === 0 || !Number(puan) || kaydet.isPending} onClick={() => kaydet.mutate()}>
            {kaydet.isPending && <Loader2 className="h-4 w-4 animate-spin" />} Ekle
          </button>
        </div>
      </div>
    </Modal>
  )
}

function ProjePenceresi({ sinav, kapat, tamam }: { sinav: SinavBilgisi; kapat: () => void; tamam: (mesaj: string) => void }) {
  const [dosya, setDosya] = useState<File | null>(null)
  const input = useRef<HTMLInputElement>(null)
  const yukle = useMutation({
    mutationFn: async () => {
      const form = new FormData()
      form.append('dosya', dosya!)
      return (await api.post<{ eklenen: number; bulunamayanNumaralar: string[]; uyarilar: string[] }>(`/sinavlar/${sinav.id}/notlar/proje`, form)).data
    },
    onSuccess: (r) => {
      tamam(`${r.eklenen} öğrencinin proje notu eklendi.${r.bulunamayanNumaralar.length ? ` Listede bulunamayan numaralar: ${r.bulunamayanNumaralar.join(', ')}.` : ''}`)
      kapat()
    },
  })
  return (
    <Modal baslik="Proje / Ek Sınav Notu Yükleme" aciklama="Öğrencilerin proje veya ödev notlarını Excel (.xlsx) dosyası ile toplu olarak sisteme ekleyin." kapat={kapat}>
      <div className="space-y-4">
        <Uyari tur="hata">
          Yükleyeceğiniz Excel dosyası OBİS sisteminden indirilen sınıf listesi yapısında olmalı ve öğrencilerin proje notları <b>SINAVNOTU</b> sütununa girilmiş olmalıdır.
        </Uyari>
        <div className="text-sm">
          <div className="font-semibold">Dosya Kuralları ve Eşleşme</div>
          <ul className="mt-1 list-disc space-y-1 pl-5 text-selcuk-metin-soluk">
            <li>Dosyada sütun başlıkları birebir <b>OGRENCINO, ADSOYAD</b> ve <b>SINAVNOTU</b> olmalıdır.</li>
            <li>Sistem öğrenci numarasına göre eşleşme yapar; SINAVNOTU mevcut notların üzerine eklenir.</li>
            <li>Dosyayı tekrar yüklerseniz önceki proje eklemelerinin yerini alır.</li>
          </ul>
        </div>
        <table className="w-full overflow-hidden rounded-lg border border-slate-200 text-xs">
          <thead className="bg-slate-50"><tr><th className="px-2 py-1 text-left">OGRENCINO</th><th className="px-2 py-1 text-left">ADSOYAD</th><th className="px-2 py-1 text-left">SINAVNOTU</th></tr></thead>
          <tbody><tr><td className="px-2 py-1">233301001</td><td className="px-2 py-1">AYŞE DEMİR</td><td className="px-2 py-1">5</td></tr><tr><td className="px-2 py-1">233301002</td><td className="px-2 py-1">ALİ VELİ</td><td className="px-2 py-1">3</td></tr></tbody>
        </table>
        <input ref={input} type="file" accept=".xlsx" hidden onChange={(e) => setDosya(e.target.files?.[0] ?? null)} />
        {dosya ? (
          <div className="flex items-center gap-3 rounded-lg border border-emerald-200 bg-emerald-50 px-3 py-2.5 text-sm">
            <FileSpreadsheet className="h-5 w-5 text-emerald-600" /> <span className="flex-1 truncate font-semibold">{dosya.name}</span>
            <button onClick={() => setDosya(null)} aria-label="Dosyayı kaldır" className="text-red-600"><X className="h-4 w-4" /></button>
          </div>
        ) : (
          <button onClick={() => input.current?.click()}
            className="flex w-full flex-col items-center gap-1 rounded-xl border-2 border-dashed border-selcuk-altin bg-amber-50/40 py-6 text-sm text-selcuk-metin-soluk hover:bg-amber-50">
            <Upload className="h-5 w-5" /> Dosya Seçmek İçin Tıklayın
          </button>
        )}
        {yukle.isError && <Uyari tur="hata">{hataMesaji(yukle.error)}</Uyari>}
        <div className="flex justify-end gap-2">
          <button className="btn-ikincil" onClick={kapat}>İptal</button>
          <button className="btn-birincil" disabled={!dosya || yukle.isPending} onClick={() => yukle.mutate()}>
            {yukle.isPending && <Loader2 className="h-4 w-4 animate-spin" />} Notları Yükle
          </button>
        </div>
      </div>
    </Modal>
  )
}
