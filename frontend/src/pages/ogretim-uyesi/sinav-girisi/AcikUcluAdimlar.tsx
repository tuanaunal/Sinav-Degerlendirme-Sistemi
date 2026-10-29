import { useEffect, useRef, useState } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { ChevronLeft, ChevronRight, Download, FileSpreadsheet, Info, Loader2, Upload, X } from 'lucide-react'
import { Uyari } from '../../../components/form'
import { api, dosyaIndir, hataMesaji } from '../../../lib/api'
import { esitDagit, puanYaz, sinavAnahtari, type SinavBilgisi } from '../../../lib/sinav'

interface SoruAyarlari {
  soruSayisi: number
  maksimumPuan: number
  puanlariDinamikDagit: boolean
  sorular: { soruNo: number; puan: number }[]
}

const kutu =
  'rounded-lg border border-slate-300 bg-white px-3 py-2 text-sm focus:border-selcuk-mavi focus:outline-none focus:ring-2 focus:ring-selcuk-mavi/20 disabled:bg-slate-50 disabled:text-slate-400'

/** Açık uçlu 2. adım: soru sayısı, maksimum puan ve (isteğe bağlı) soru başına puan. */
export function AcikAdim2SoruSayisi({ sinav, geri, ileri }: { sinav: SinavBilgisi; geri: () => void; ileri: () => void }) {
  const queryClient = useQueryClient()
  const veri = useQuery({
    queryKey: ['soru-ayarlari', sinav.id],
    queryFn: async () => (await api.get<SoruAyarlari>(`/sinavlar/${sinav.id}/acik-uclu/soru-ayarlari`)).data,
  })
  const [n, setN] = useState(10)
  const [maks, setMaks] = useState(100)
  const [dinamik, setDinamik] = useState(true)
  const [elle, setElle] = useState<number[]>([])

  useEffect(() => {
    if (!veri.data) return
    setN(veri.data.soruSayisi)
    setMaks(veri.data.maksimumPuan)
    setDinamik(veri.data.puanlariDinamikDagit)
    setElle(veri.data.sorular.map((s) => s.puan))
  }, [veri.data])

  const puanlar = dinamik ? esitDagit(maks, n) : Array.from({ length: n }, (_, i) => elle[i] ?? 0)
  const toplam = Math.round(puanlar.reduce((a, b) => a + b, 0) * 100) / 100

  const kaydet = useMutation({
    meta: { bildirim: 'Soru ayarları kaydedildi' },
    mutationFn: async () =>
      (await api.put<SinavBilgisi>(`/sinavlar/${sinav.id}/acik-uclu/soru-ayarlari`, {
        soruSayisi: n, maksimumPuan: maks, puanlariDinamikDagit: dinamik,
        sorular: puanlar.map((p, i) => ({ soruNo: i + 1, puan: p })),
      })).data,
    onSuccess: (s) => {
      queryClient.setQueryData(sinavAnahtari(s.dersId, s.tur), s)
      queryClient.invalidateQueries({ queryKey: ['soru-ayarlari', s.id] })
      ileri()
    },
  })

  if (veri.isLoading) return <div className="kart flex justify-center p-10"><Loader2 className="h-6 w-6 animate-spin text-selcuk-mavi" /></div>

  return (
    <div className="kart p-5 sm:p-6">
      <h2 className="text-lg font-bold text-selcuk-lacivert">Açık Uçlu Sınav Ayarları</h2>
      <div className="mt-4 max-w-xl rounded-xl border border-slate-200 p-4">
        <div className="font-semibold">Soru Sayısı ve Maksimum Puan</div>
        <div className="mt-3 flex flex-wrap gap-4">
          <label className="text-sm font-semibold">
            Soru Sayısı
            <input type="number" min={1} max={50} className={`${kutu} mt-1.5 block w-32`} value={n}
              onChange={(e) => setN(Math.max(1, Math.min(50, Number(e.target.value))))} />
          </label>
          <label className="text-sm font-semibold">
            Maksimum Puan
            <input type="number" min={1} max={1000} className={`${kutu} mt-1.5 block w-32`} value={maks}
              onChange={(e) => setMaks(Number(e.target.value))} />
          </label>
        </div>
        <label className="mt-3 flex items-center gap-2 text-sm font-semibold">
          <input type="checkbox" className="h-4 w-4 accent-selcuk-altin" checked={dinamik}
            onChange={(e) => { if (!e.target.checked) setElle(puanlar); setDinamik(e.target.checked) }} />
          Puanları sorulara eşit dağıt
        </label>
        <p className="mt-3 flex items-center gap-2 rounded-lg bg-selcuk-zemin px-3 py-2 text-sm text-selcuk-metin-soluk">
          <Info className="h-4 w-4 shrink-0" /> {n} soru, maksimum {puanYaz(maks)} puan olarak tanımlandı
          {dinamik && <> (soru başına {puanYaz(puanlar[0] ?? 0)})</>}.
        </p>
      </div>

      {!dinamik && (
        <div className="mt-4">
          <div className={`text-sm font-semibold ${Math.abs(toplam - maks) < 0.01 ? 'text-emerald-600' : 'text-red-600'}`}>
            Soru puanları toplamı: {puanYaz(toplam)} / {puanYaz(maks)}
          </div>
          <div className="mt-2 grid grid-cols-3 gap-2 sm:grid-cols-5 lg:grid-cols-10">
            {puanlar.map((p, i) => (
              <label key={i} className="text-xs font-semibold text-selcuk-metin-soluk">
                S{i + 1}
                <input type="number" min={0} step={0.5} className={`${kutu} mt-1 w-full px-2 py-1.5`} value={p}
                  onChange={(e) => setElle(puanlar.map((x, j) => (j === i ? Number(e.target.value) : x)))} />
              </label>
            ))}
          </div>
        </div>
      )}

      {sinav.tamamlananAdim >= 3 && veri.data && n !== veri.data.soruSayisi && (
        <div className="mt-4"><Uyari tur="uyari">Soru sayısını değiştirirseniz notları tekrar yüklemeniz gerekir.</Uyari></div>
      )}
      {kaydet.isError && <div className="mt-4"><Uyari tur="hata">{hataMesaji(kaydet.error)}</Uyari></div>}

      <div className="mt-6 flex justify-between">
        <button className="btn-ikincil" onClick={geri}><ChevronLeft className="h-4 w-4" /> Geri</button>
        <button className="btn-birincil" disabled={kaydet.isPending || (!dinamik && Math.abs(toplam - maks) >= 0.01)} onClick={() => kaydet.mutate()}>
          {kaydet.isPending && <Loader2 className="h-4 w-4 animate-spin" />} İleri <ChevronRight className="h-4 w-4" />
        </button>
      </div>
    </div>
  )
}

interface YuklemeSonucu {
  giren: number
  girmeyen: number
  listedeOlmayanlar: string[]
  uyarilar: string[]
}

/** Açık uçlu 3. adım: soru bazlı puanların Excel ile yüklenmesi (sınıf listesinden üretilen şablonla). */
export function AcikAdim3NotYukleme({ sinav, geri, ileri }: { sinav: SinavBilgisi; geri: () => void; ileri: () => void }) {
  const queryClient = useQueryClient()
  const [dosya, setDosya] = useState<File | null>(null)
  const [sablonHata, setSablonHata] = useState<string | null>(null)
  const input = useRef<HTMLInputElement>(null)

  const yukle = useMutation({
    meta: { bildirim: 'Notlar yüklendi' },
    mutationFn: async () => {
      const form = new FormData()
      form.append('dosya', dosya!)
      return (await api.post<YuklemeSonucu>(`/sinavlar/${sinav.id}/acik-uclu/notlar`, form)).data
    },
    onSuccess: () => {
      queryClient.setQueryData(sinavAnahtari(sinav.dersId, sinav.tur), { ...sinav, tamamlananAdim: Math.max(sinav.tamamlananAdim, 3) })
      queryClient.invalidateQueries({ queryKey: ['notlar', sinav.id] })
      queryClient.invalidateQueries({ queryKey: ['dersler'] })
    },
  })

  const sablonIndir = () =>
    dosyaIndir(`/sinavlar/${sinav.id}/acik-uclu/sablon`, 'Acik_Uclu_Not_Sablonu.xlsx').catch((e) => setSablonHata(hataMesaji(e)))

  return (
    <div className="kart p-5 sm:p-6">
      <h2 className="text-lg font-bold text-selcuk-lacivert">Not Yükleme</h2>
      <p className="mt-1 text-sm text-selcuk-metin-soluk">
        Öğrencilerin her sorudan aldığı puanları içeren Excel dosyasını yükleyin. Satırı tamamen boş olan öğrenci sınava girmemiş sayılır.
      </p>

      <div className="mt-5 grid gap-5 lg:grid-cols-2">
        <div className="rounded-xl border border-slate-200 p-4">
          <div className="font-semibold">1. Şablonu indirin ve doldurun</div>
          <p className="mt-1 text-sm text-selcuk-metin-soluk">
            Şablonda sınıf listenizdeki {sinav.sinifMevcudu} öğrenci ve <b>S1 … S{sinav.soruSayisi}</b> sütunları hazır gelir.
          </p>
          <table className="mt-3 w-full overflow-hidden rounded-lg border border-slate-200 text-xs">
            <thead className="bg-amber-50">
              <tr>
                <th className="px-2 py-1 text-left">OGRENCINO</th><th className="px-2 py-1 text-left">ADSOYAD</th>
                <th className="px-2 py-1">S1</th><th className="px-2 py-1">S2</th><th className="px-2 py-1">…</th>
              </tr>
            </thead>
            <tbody><tr><td className="px-2 py-1">233301001</td><td className="px-2 py-1">AYŞE DEMİR</td><td className="px-2 py-1 text-center">8,5</td><td className="px-2 py-1 text-center">10</td><td className="px-2 py-1 text-center">…</td></tr></tbody>
          </table>
          <button className="btn-mavi mt-3" onClick={sablonIndir}><Download className="h-4 w-4" /> Şablonu İndir</button>
          {sablonHata && <div className="mt-2"><Uyari tur="hata">{sablonHata}</Uyari></div>}
        </div>

        <div className="rounded-xl border-2 border-dashed border-amber-300 bg-amber-50/40 p-4">
          <div className="font-semibold">2. Doldurduğunuz dosyayı yükleyin</div>
          <input ref={input} type="file" accept=".xlsx" hidden onChange={(e) => { setDosya(e.target.files?.[0] ?? null); yukle.reset() }} />
          {dosya ? (
            <div className="mt-3 flex items-center gap-3 rounded-lg border border-emerald-200 bg-white px-3 py-2.5 text-sm">
              <FileSpreadsheet className="h-5 w-5 text-emerald-600" />
              <span className="min-w-0 flex-1 truncate font-semibold text-emerald-800">{dosya.name}</span>
              <button onClick={() => { setDosya(null); yukle.reset(); if (input.current) input.current.value = '' }} aria-label="Dosyayı kaldır" className="text-red-600"><X className="h-4 w-4" /></button>
            </div>
          ) : (
            <button className="btn-birincil mt-3 w-full py-2.5" onClick={() => input.current?.click()}><Upload className="h-4 w-4" /> Dosya Seç</button>
          )}
          {dosya && !yukle.isSuccess && (
            <button className="btn-mavi mt-3 w-full" disabled={yukle.isPending} onClick={() => yukle.mutate()}>
              {yukle.isPending ? <Loader2 className="h-4 w-4 animate-spin" /> : <Upload className="h-4 w-4" />} Notları Yükle
            </button>
          )}
        </div>
      </div>

      {yukle.isError && <div className="mt-4"><Uyari tur="hata">{hataMesaji(yukle.error)}</Uyari></div>}
      {yukle.data && (
        <div className="mt-4 space-y-2">
          <Uyari tur="basari">
            Notlar yüklendi: <b>{yukle.data.giren}</b> öğrenci sınava girmiş, <b>{yukle.data.girmeyen}</b> öğrenci girmemiş.
          </Uyari>
          {yukle.data.listedeOlmayanlar.length > 0 && (
            <Uyari tur="uyari">Sınıf listesinde olmayan numaralar atlandı: {yukle.data.listedeOlmayanlar.join(', ')}</Uyari>
          )}
          {yukle.data.uyarilar.map((u) => <Uyari key={u} tur="uyari">{u}</Uyari>)}
        </div>
      )}
      {!yukle.data && sinav.tamamlananAdim >= 3 && (
        <div className="mt-4"><Uyari tur="bilgi">Notlar daha önce yüklendi. Yeni dosya yüklerseniz soru puanları dosyadakilerle değiştirilir.</Uyari></div>
      )}

      <div className="mt-6 flex justify-between">
        <button className="btn-ikincil" onClick={geri}><ChevronLeft className="h-4 w-4" /> Geri</button>
        <button className="btn-birincil" disabled={!(yukle.isSuccess || sinav.tamamlananAdim >= 3)} onClick={ileri}>
          İleri <ChevronRight className="h-4 w-4" />
        </button>
      </div>
    </div>
  )
}
