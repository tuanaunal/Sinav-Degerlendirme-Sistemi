import { useState } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import {
  CheckCircle2, ChevronDown, ChevronLeft, ChevronRight, EyeOff, Info, Link2, Loader2, PartyPopper, RotateCcw, Undo2, Unlink, Users,
} from 'lucide-react'
import { Uyari } from '../../../components/form'
import { api, hataMesaji } from '../../../lib/api'
import { sinavAnahtari, type SinavBilgisi } from '../../../lib/sinav'

interface EslesenKayit {
  sonucId: number
  ogrenciNo: string
  adSoyad: string
  optikNo: string | null
  optikAdSoyad: string | null
  kitapcik: string | null
  numaraFarkli: boolean
  manuel: boolean
}
interface BekleyenKagit {
  id: number
  ogrenciNo: string
  adSoyad: string
  kitapcik: string | null
  satirNo: number
  oneriSonucId: number | null
  oneriAdSoyad: string | null
}
interface BekleyenOgrenci {
  sonucId: number
  ogrenciNo: string
  adSoyad: string
}
interface EslestirmeDurumu {
  sinifMevcudu: number
  kagitSayisi: number
  eslesenler: EslesenKayit[]
  bekleyenKagitlar: BekleyenKagit[]
  gozArdiEdilenler: BekleyenKagit[]
  bekleyenOgrenciler: BekleyenOgrenci[]
  kitapcikGerekli: boolean
}

const harf = (i: number) => String.fromCharCode(65 + i)

/**
 * 3. adım: optik kağıtları sınıf listesiyle eşleştirilir. Otomatik eşleşmeyenler iki panelde gösterilir;
 * soldan bir kağıt, sağdan bir öğrenci seçilip "Seçili Kayıtları Eşleştir" ile bağlanır veya kağıt göz ardı edilir.
 */
export default function Adim3VeriEslestirme({ sinav, geri, ileri }: { sinav: SinavBilgisi; geri: () => void; ileri: () => void }) {
  const queryClient = useQueryClient()
  const anahtar = ['eslestirme', sinav.id]
  const durum = useQuery({
    queryKey: anahtar,
    queryFn: async () => (await api.get<EslestirmeDurumu>(`/sinavlar/${sinav.id}/eslestirme`)).data,
  })
  const [seciliKagit, setSeciliKagit] = useState<number | null>(null)
  const [seciliOgrenci, setSeciliOgrenci] = useState<number | null>(null)
  const [eslesenlerAcik, setEslesenlerAcik] = useState(false)
  const [renkKodlari, setRenkKodlari] = useState(false)

  const islem = useMutation({
    mutationFn: async (f: () => Promise<{ data: EslestirmeDurumu }>) => (await f()).data,
    onSuccess: (d) => {
      queryClient.setQueryData(anahtar, d)
      setSeciliKagit(null)
      setSeciliOgrenci(null)
    },
  })
  const yap = (yol: string, govde?: object) => islem.mutate(() => api.post<EslestirmeDurumu>(`/sinavlar/${sinav.id}/eslestirme/${yol}`, govde))

  const tamamla = useMutation({
    meta: { bildirim: 'Eşleştirme kaydedildi' },
    mutationFn: async () => api.post(`/sinavlar/${sinav.id}/eslestirme/tamamla`),
    onSuccess: () => {
      queryClient.setQueryData(sinavAnahtari(sinav.dersId, sinav.tur), { ...sinav, tamamlananAdim: Math.max(sinav.tamamlananAdim, 3) })
      queryClient.invalidateQueries({ queryKey: ['notlar', sinav.id] })
      queryClient.invalidateQueries({ queryKey: ['dersler'] })
      ileri()
    },
  })

  if (durum.isLoading) return <div className="kart flex justify-center p-10"><Loader2 className="h-6 w-6 animate-spin text-selcuk-mavi" /></div>
  if (durum.isError || !durum.data) return <Uyari tur="hata">{hataMesaji(durum.error)}</Uyari>

  const d = durum.data
  const kitapcikBos = d.eslesenler.filter((e) => d.kitapcikGerekli && !e.kitapcik)
  const herkesTamam = d.bekleyenKagitlar.length === 0 && d.bekleyenOgrenciler.length === 0 && kitapcikBos.length === 0
  const hata = islem.error ?? tamamla.error

  return (
    <div className="kart p-5 sm:p-6">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <h2 className="text-lg font-bold text-selcuk-lacivert">Veri Eşleştirme</h2>
        <button className="flex items-center gap-1.5 text-sm font-semibold text-selcuk-metin-soluk hover:text-selcuk-mavi" onClick={() => setRenkKodlari((a) => !a)}>
          <Info className="h-4 w-4" /> Renk Kodları
        </button>
      </div>

      {renkKodlari && (
        <div className="mt-3 grid gap-2 rounded-lg border border-slate-200 p-3 text-xs sm:grid-cols-2">
          <span className="flex items-center gap-2"><Rozet renk="yesil">Otomatik</Rozet> Numarası birebir eşleşti</span>
          <span className="flex items-center gap-2"><Rozet renk="sari">Numara farklı</Rozet> Bir hanesi yanlış kodlanmış, sistem tanıdı</span>
          <span className="flex items-center gap-2"><Rozet renk="mavi">Elle</Rozet> Öğretim üyesi eşleştirdi</span>
          <span className="flex items-center gap-2"><Rozet renk="gri">Göz ardı</Rozet> Kağıt değerlendirmeye alınmayacak</span>
        </div>
      )}

      <div className="mt-4 grid gap-3 sm:grid-cols-3">
        <Sayac etiket="Sınıf mevcudu" deger={d.sinifMevcudu} />
        <Sayac etiket="Optik kağıdı" deger={d.kagitSayisi} />
        <Sayac etiket="Eşleşen" deger={d.eslesenler.length} vurgu />
      </div>

      {hata && <div className="mt-4"><Uyari tur="hata">{hataMesaji(hata)}</Uyari></div>}

      {d.bekleyenKagitlar.length === 0 && kitapcikBos.length === 0 ? (
        <div className="mt-6 flex flex-col items-center py-6 text-center">
          <div className="relative">
            <span className="flex h-24 w-24 items-center justify-center rounded-full bg-emerald-500 text-white shadow-lg shadow-emerald-200">
              <CheckCircle2 className="h-12 w-12" />
            </span>
            <span className="absolute -right-2 -top-1 flex h-9 w-9 items-center justify-center rounded-full bg-selcuk-altin text-selcuk-lacivert"><PartyPopper className="h-4 w-4" /></span>
            <span className="absolute -bottom-1 -left-2 flex h-8 w-8 items-center justify-center rounded-full bg-blue-100 text-selcuk-mavi"><Users className="h-4 w-4" /></span>
          </div>
          {herkesTamam ? (
            <p className="mt-5 text-sm text-selcuk-metin-soluk">
              Optik verilerinize göre sınava <b className="text-emerald-600">herkes katılmış</b> ve bilgilerini <b className="text-emerald-600">doğru girmiş</b>.
            </p>
          ) : (
            <p className="mt-5 max-w-md text-sm text-selcuk-metin-soluk">
              Tüm optik kağıtları işlendi. Sınıf listesinde eşleşmeyen <b>{d.bekleyenOgrenciler.length} öğrenci</b> "sınava girmedi" olarak kaydedilecek.
            </p>
          )}
          <span className="mt-3 inline-flex items-center gap-2 rounded-full border border-emerald-300 bg-emerald-50 px-4 py-1.5 text-sm font-semibold text-emerald-700">
            <CheckCircle2 className="h-4 w-4" /> Eşleştirme adımı tamamlandı
          </span>
        </div>
      ) : (
        <>
          <div className="mt-5 grid gap-5 xl:grid-cols-2">
            {/* Optik verisi */}
            <div className="rounded-xl border border-slate-200">
              <div className="border-b border-slate-100 px-4 py-3 font-semibold">
                Optik Verisi <span className="text-sm font-normal text-selcuk-metin-soluk">({d.bekleyenKagitlar.length} eşleşmemiş)</span>
              </div>
              <ul className="divide-y divide-slate-100">
                {d.bekleyenKagitlar.length === 0 && (
                  <li className="flex flex-col items-center gap-1 px-4 py-6 text-center text-sm text-emerald-700">
                    <CheckCircle2 className="h-6 w-6" /> Tüm kayıtlar işlendi
                  </li>
                )}
                {d.bekleyenKagitlar.map((k) => (
                  <li key={k.id} className={`flex items-start gap-3 px-4 py-3 ${seciliKagit === k.id ? 'bg-amber-50' : ''}`}>
                    <input type="radio" name="kagit" className="mt-1 h-4 w-4 accent-selcuk-altin" checked={seciliKagit === k.id}
                      onChange={() => setSeciliKagit(k.id)} aria-label={`${k.adSoyad} kağıdını seç`} />
                    <div className="min-w-0 flex-1 text-sm">
                      <div className="font-semibold">{k.adSoyad || '(isimsiz)'}</div>
                      <div className="text-xs text-selcuk-metin-soluk">
                        {k.ogrenciNo ? <span className="font-mono">{k.ogrenciNo}</span> : <span className="text-red-600">Numara kodlanmamış</span>}
                        {' · '}{k.kitapcik ? `${k.kitapcik} kitapçığı` : 'kitapçık boş'} · {k.satirNo}. satır
                      </div>
                      {k.oneriSonucId && (
                        <button className="mt-1.5 inline-flex items-center gap-1 rounded-md bg-blue-50 px-2 py-1 text-xs font-semibold text-selcuk-mavi hover:bg-blue-100"
                          disabled={islem.isPending} onClick={() => yap('eslestir', { optikKaydiId: k.id, sinavSonucuId: k.oneriSonucId })}>
                          <Link2 className="h-3.5 w-3.5" /> Öneri: {k.oneriAdSoyad} ile eşleştir
                        </button>
                      )}
                    </div>
                    <button className="shrink-0 rounded-md border border-slate-200 px-2 py-1 text-xs font-semibold text-selcuk-metin-soluk hover:bg-slate-50"
                      disabled={islem.isPending} onClick={() => yap(`gozardi/${k.id}`)}>
                      <EyeOff className="mr-1 inline h-3.5 w-3.5" />Göz ardı et
                    </button>
                  </li>
                ))}
              </ul>
            </div>

            {/* Sınıf listesi */}
            <div className="rounded-xl border border-slate-200">
              <div className="border-b border-slate-100 px-4 py-3 font-semibold">
                Sınıf Listesi <span className="text-sm font-normal text-selcuk-metin-soluk">({d.bekleyenOgrenciler.length} eşleşmemiş)</span>
              </div>
              <ul className="max-h-80 divide-y divide-slate-100 overflow-y-auto">
                {d.bekleyenOgrenciler.map((o) => (
                  <li key={o.sonucId}>
                    <label className={`flex cursor-pointer items-center gap-3 px-4 py-2.5 text-sm ${seciliOgrenci === o.sonucId ? 'bg-amber-50' : 'hover:bg-slate-50'}`}>
                      <input type="radio" name="ogrenci" className="h-4 w-4 accent-selcuk-altin" checked={seciliOgrenci === o.sonucId}
                        onChange={() => setSeciliOgrenci(o.sonucId)} />
                      <span className="font-mono text-xs text-selcuk-metin-soluk">{o.ogrenciNo}</span>
                      <span className="font-semibold">{o.adSoyad}</span>
                    </label>
                  </li>
                ))}
              </ul>
              <p className="border-t border-slate-100 px-4 py-2 text-xs text-selcuk-metin-soluk">
                Eşleşmeyen öğrenciler adımı tamamladığınızda "sınava girmedi" olarak kaydedilir.
              </p>
            </div>
          </div>

          <div className="mt-4 flex justify-center">
            <button className="btn-birincil" disabled={!seciliKagit || !seciliOgrenci || islem.isPending}
              onClick={() => yap('eslestir', { optikKaydiId: seciliKagit, sinavSonucuId: seciliOgrenci })}>
              <Link2 className="h-4 w-4" /> Seçili Kayıtları Eşleştir
            </button>
          </div>
        </>
      )}

      {kitapcikBos.length > 0 && (
        <div className="mt-5 space-y-2">
          <Uyari tur="uyari">Aşağıdaki öğrenciler kitapçık türünü boş bırakmış. Puanlama için kitapçığı seçin.</Uyari>
          {kitapcikBos.map((e) => (
            <div key={e.sonucId} className="flex flex-wrap items-center gap-3 rounded-lg border border-amber-200 px-3 py-2 text-sm">
              <span className="font-mono text-xs">{e.ogrenciNo}</span>
              <span className="font-semibold">{e.adSoyad}</span>
              <span className="ml-auto flex gap-1">
                {Array.from({ length: sinav.kitapcikSayisi }, (_, i) => (
                  <button key={i} className="btn-ikincil px-3 py-1" disabled={islem.isPending}
                    onClick={() => yap('kitapcik', { sinavSonucuId: e.sonucId, kitapcik: harf(i) })}>
                    {harf(i)} kitapçığı
                  </button>
                ))}
              </span>
            </div>
          ))}
        </div>
      )}

      {d.gozArdiEdilenler.length > 0 && (
        <div className="mt-5 rounded-lg border border-slate-200 p-3 text-sm">
          <div className="font-semibold text-selcuk-metin-soluk">Göz ardı edilen kağıtlar</div>
          {d.gozArdiEdilenler.map((k) => (
            <div key={k.id} className="mt-2 flex items-center gap-3">
              <span className="font-mono text-xs">{k.ogrenciNo || '—'}</span>
              <span>{k.adSoyad}</span>
              <button className="ml-auto text-xs font-semibold text-selcuk-mavi hover:underline" onClick={() => yap(`gozardi/${k.id}?geriAl=true`)}>
                <Undo2 className="mr-1 inline h-3.5 w-3.5" />Geri al
              </button>
            </div>
          ))}
        </div>
      )}

      {d.eslesenler.some((e) => e.manuel) && (
        <div className="mt-5 rounded-xl border border-amber-300 bg-amber-50/60 p-4">
          <div className="flex items-center gap-2 text-sm font-semibold">
            Elle Yapılan Eşleştirmeler
            <span className="rounded-full bg-selcuk-altin px-2 text-xs text-selcuk-lacivert">{d.eslesenler.filter((e) => e.manuel).length}</span>
          </div>
          <p className="text-xs text-selcuk-metin-soluk">Bu eşleştirmeler kaydedildi; yanlış yapılanı geri alabilirsiniz.</p>
          <ul className="mt-3 space-y-2">
            {d.eslesenler.filter((e) => e.manuel).map((e) => (
              <li key={e.sonucId} className="flex flex-wrap items-center gap-3 rounded-lg border border-amber-200 bg-white px-3 py-2 text-sm">
                <div><div className="text-[11px] text-selcuk-metin-soluk">Optik</div><span className="font-mono text-xs">{e.optikNo || '—'}</span></div>
                <span className="text-selcuk-metin-soluk">→</span>
                <div><div className="text-[11px] text-selcuk-metin-soluk">Sınıf listesi</div><span className="font-mono text-xs">{e.ogrenciNo}</span> {e.adSoyad}</div>
                <button className="ml-auto text-xs font-semibold text-selcuk-mavi hover:underline" disabled={islem.isPending} onClick={() => yap(`kaldir/${e.sonucId}`)}>
                  <Undo2 className="mr-1 inline h-3.5 w-3.5" />Geri Al
                </button>
              </li>
            ))}
          </ul>
        </div>
      )}

      <div className="mt-5 rounded-lg border border-slate-200">
        <button className="flex w-full items-center justify-between px-3 py-2.5 text-sm font-semibold" onClick={() => setEslesenlerAcik((a) => !a)}>
          Eşleşen kayıtlar ({d.eslesenler.length})
          <ChevronDown className={`h-4 w-4 transition-transform ${eslesenlerAcik ? 'rotate-180' : ''}`} />
        </button>
        {eslesenlerAcik && (
          <div className="max-h-96 overflow-auto border-t border-slate-100">
            <table className="w-full min-w-[640px] text-left text-sm">
              <thead className="sticky top-0 bg-white text-xs text-selcuk-metin-soluk">
                <tr>
                  <th className="px-3 py-2">Öğrenci No</th><th className="px-3 py-2">Ad Soyad</th><th className="px-3 py-2">Optikteki No</th>
                  <th className="px-3 py-2">Kitapçık</th><th className="px-3 py-2">Durum</th><th className="px-3 py-2" />
                </tr>
              </thead>
              <tbody>
                {d.eslesenler.map((e) => (
                  <tr key={e.sonucId} className="border-t border-slate-100">
                    <td className="px-3 py-2 font-mono text-xs">{e.ogrenciNo}</td>
                    <td className="px-3 py-2">{e.adSoyad}</td>
                    <td className={`px-3 py-2 font-mono text-xs ${e.numaraFarkli ? 'text-amber-700' : 'text-selcuk-metin-soluk'}`}>{e.optikNo || '—'}</td>
                    <td className="px-3 py-2">{e.kitapcik ?? <span className="text-red-600">Boş</span>}</td>
                    <td className="px-3 py-2">
                      {e.manuel ? <Rozet renk="mavi">Elle</Rozet> : e.numaraFarkli ? <Rozet renk="sari">Numara farklı</Rozet> : <Rozet renk="yesil">Otomatik</Rozet>}
                    </td>
                    <td className="px-3 py-2 text-right">
                      <button className="text-xs font-semibold text-red-600 hover:underline" onClick={() => yap(`kaldir/${e.sonucId}`)}>
                        <Unlink className="mr-1 inline h-3.5 w-3.5" />Kaldır
                      </button>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </div>

      <div className="mt-6 flex flex-wrap items-center justify-between gap-3">
        <div className="flex gap-2">
          <button className="btn-ikincil" onClick={geri}><ChevronLeft className="h-4 w-4" /> Geri</button>
          <button className="btn-ikincil" disabled={islem.isPending} onClick={() => yap('sifirla')}><RotateCcw className="h-4 w-4" /> Sıfırla</button>
        </div>
        <button className="btn-birincil" disabled={d.bekleyenKagitlar.length > 0 || kitapcikBos.length > 0 || tamamla.isPending} onClick={() => tamamla.mutate()}>
          {tamamla.isPending && <Loader2 className="h-4 w-4 animate-spin" />} İleri <ChevronRight className="h-4 w-4" />
        </button>
      </div>
    </div>
  )
}

function Sayac({ etiket, deger, vurgu }: { etiket: string; deger: number; vurgu?: boolean }) {
  return (
    <div className={`rounded-lg border px-4 py-3 ${vurgu ? 'border-emerald-200 bg-emerald-50' : 'border-slate-200'}`}>
      <div className="text-xs text-selcuk-metin-soluk">{etiket}</div>
      <div className={`text-2xl font-bold ${vurgu ? 'text-emerald-700' : 'text-selcuk-lacivert'}`}>{deger}</div>
    </div>
  )
}

function Rozet({ renk, children }: { renk: 'yesil' | 'sari' | 'mavi' | 'gri'; children: React.ReactNode }) {
  const stil = { yesil: 'bg-emerald-100 text-emerald-800', sari: 'bg-amber-100 text-amber-800', mavi: 'bg-blue-100 text-selcuk-mavi', gri: 'bg-slate-100 text-slate-600' }[renk]
  return <span className={`inline-block whitespace-nowrap rounded-full px-2 py-0.5 text-xs font-semibold ${stil}`}>{children}</span>
}
