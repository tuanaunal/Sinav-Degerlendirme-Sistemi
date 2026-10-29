import { useEffect, useMemo, useState } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { CheckCircle2, ChevronLeft, ChevronRight, Loader2 } from 'lucide-react'
import { Uyari } from '../../../components/form'
import { api, hataMesaji } from '../../../lib/api'
import { esitDagit, puanYaz, sinavAnahtari, type CevapAnahtari, type SinavBilgisi } from '../../../lib/sinav'

const SIKLAR = ['A', 'B', 'C', 'D', 'E']
const harf = (i: number) => String.fromCharCode(65 + i)

const kutu =
  'rounded-lg border border-slate-300 bg-white px-3 py-2 text-sm focus:border-selcuk-mavi focus:outline-none focus:ring-2 focus:ring-selcuk-mavi/20 disabled:bg-slate-50 disabled:text-slate-400'

/** 2. adım: maksimum puan, kitapçık sayısı, soru puanları ve her kitapçığın cevap anahtarı. */
export default function Adim2CevapAnahtari({ sinav, geri, ileri }: { sinav: SinavBilgisi; geri: () => void; ileri: () => void }) {
  const queryClient = useQueryClient()
  const veri = useQuery({
    queryKey: ['cevap-anahtari', sinav.id],
    queryFn: async () => (await api.get<CevapAnahtari>(`/sinavlar/${sinav.id}/cevap-anahtari`)).data,
  })

  const [maks, setMaks] = useState(100)
  const [soruSayisi, setSoruSayisi] = useState(25)
  const [kitapcikSayisi, setKitapcikSayisi] = useState(1)
  const [dinamik, setDinamik] = useState(true)
  const [coklu, setCoklu] = useState(false)
  const [elPuanlari, setElPuanlari] = useState<number[]>([])
  const [cevaplar, setCevaplar] = useState<string[][]>([])

  useEffect(() => {
    if (!veri.data) return
    const d = veri.data
    setMaks(d.maksimumPuan)
    setSoruSayisi(d.soruSayisi)
    setKitapcikSayisi(d.kitapcikSayisi)
    setDinamik(d.puanlariDinamikDagit)
    setCoklu(d.birdenFazlaDogruKabul)
    setElPuanlari(d.sorular.map((s) => s.puan))
    setCevaplar(d.kitapciklar.map((k) => k.cevaplar))
  }, [veri.data])

  // Soru veya kitapçık sayısı değişince dizileri boyutlandır (girilenler korunur)
  const boyutla = (dizi: string[], n: number) => Array.from({ length: n }, (_, i) => dizi[i] ?? '')
  const tumCevaplar = useMemo(
    () => Array.from({ length: kitapcikSayisi }, (_, k) => boyutla(cevaplar[k] ?? [], soruSayisi)),
    [cevaplar, kitapcikSayisi, soruSayisi],
  )
  const puanlar = dinamik ? esitDagit(maks, soruSayisi) : Array.from({ length: soruSayisi }, (_, i) => elPuanlari[i] ?? 0)
  const puanToplami = Math.round(puanlar.reduce((a, b) => a + b, 0) * 100) / 100

  const sikSec = (k: number, soru: number, sik: string) =>
    setCevaplar(() => {
      const yeni = tumCevaplar.map((x) => [...x])
      const mevcut = yeni[k][soru]
      if (coklu) yeni[k][soru] = mevcut.includes(sik) ? mevcut.replace(sik, '') : [...mevcut + sik].sort().join('')
      else yeni[k][soru] = mevcut === sik ? '' : sik
      return yeni
    })

  const kitapcigaYukle = (k: number, metin: string) =>
    setCevaplar(() => {
      const yeni = tumCevaplar.map((x) => [...x])
      // Boş (' ') ve çoklu işaretleme ('*') optikte anahtar olamaz; o sorular boş bırakılır
      yeni[k] = Array.from({ length: soruSayisi }, (_, i) => (SIKLAR.includes(metin[i] ?? '') ? metin[i] : ''))
      return yeni
    })

  const kaydet = useMutation({
    meta: { bildirim: 'Cevap anahtarı kaydedildi' },
    mutationFn: async () =>
      (await api.put<SinavBilgisi>(`/sinavlar/${sinav.id}/cevap-anahtari`, {
        soruSayisi,
        maksimumPuan: maks,
        kitapcikSayisi,
        puanlariDinamikDagit: dinamik,
        birdenFazlaDogruKabul: coklu,
        sorular: puanlar.map((p, i) => ({ soruNo: i + 1, puan: p })),
        kitapciklar: tumCevaplar.map((c, i) => ({ kitapcik: harf(i), cevaplar: c })),
      })).data,
    onSuccess: (s) => {
      queryClient.setQueryData(sinavAnahtari(s.dersId, s.tur), s)
      queryClient.invalidateQueries({ queryKey: ['cevap-anahtari', s.id] })
      ileri()
    },
  })

  if (veri.isLoading) return <div className="kart flex justify-center p-10"><Loader2 className="h-6 w-6 animate-spin text-selcuk-mavi" /></div>
  if (veri.isError) return <Uyari tur="hata">{hataMesaji(veri.error)}</Uyari>

  const eksik = tumCevaplar.reduce((t, c) => t + c.filter((x) => !x).length, 0)

  return (
    <div className="kart p-5 sm:p-6">
      <div className="flex flex-wrap items-end gap-x-6 gap-y-4 border-b border-slate-100 pb-5">
        <label className="text-sm font-semibold">
          Maksimum Puan
          <input type="number" min={1} max={1000} className={`${kutu} mt-1.5 block w-32`} value={maks}
            onChange={(e) => setMaks(Number(e.target.value))} />
        </label>
        <label className="text-sm font-semibold">
          Soru Sayısı
          <input type="number" min={1} max={200} className={`${kutu} mt-1.5 block w-28`} value={soruSayisi}
            onChange={(e) => setSoruSayisi(Math.max(1, Math.min(200, Number(e.target.value))))} />
        </label>
        <label className="text-sm font-semibold">
          Kitapçık Sayısı
          <select className={`${kutu} mt-1.5 block w-40`} value={kitapcikSayisi} onChange={(e) => setKitapcikSayisi(Number(e.target.value))}>
            {[1, 2, 3, 4, 5].map((n) => (
              <option key={n} value={n}>{n} ({Array.from({ length: n }, (_, i) => harf(i)).join(', ')})</option>
            ))}
          </select>
        </label>
        <label className="flex items-center gap-2 pb-2 text-sm font-semibold">
          <input type="checkbox" className="h-4 w-4 accent-selcuk-altin" checked={dinamik} onChange={(e) => {
            if (!e.target.checked) setElPuanlari(puanlar)
            setDinamik(e.target.checked)
          }} />
          Puanları Dinamik Dağıt
        </label>
        <label className="flex items-center gap-2 pb-2 text-sm font-semibold">
          <input type="checkbox" className="h-4 w-4 accent-selcuk-altin" checked={coklu} onChange={(e) => setCoklu(e.target.checked)} />
          Birden fazla şıkkı doğru kabul et
        </label>
      </div>

      {!dinamik && (
        <p className={`mt-3 text-sm font-semibold ${Math.abs(puanToplami - maks) < 0.01 ? 'text-emerald-600' : 'text-red-600'}`}>
          Soru puanları toplamı: {puanYaz(puanToplami)} / {puanYaz(maks)}
        </p>
      )}

      <div className={`mt-5 grid gap-5 ${kitapcikSayisi > 1 ? 'xl:grid-cols-2' : ''}`}>
        {tumCevaplar.map((kitapcik, k) => (
          <KitapcikKarti
            key={k}
            harf={harf(k)}
            cevaplar={kitapcik}
            puanlar={puanlar}
            dinamik={dinamik}
            secenekler={veri.data!.optikSecenekleri}
            sikSec={(soru, sik) => sikSec(k, soru, sik)}
            puanDegistir={(soru, p) => setElPuanlari(puanlar.map((x, i) => (i === soru ? p : x)))}
            optiktenYukle={(m) => kitapcigaYukle(k, m)}
          />
        ))}
      </div>

      {kaydet.isError && <div className="mt-4"><Uyari tur="hata">{hataMesaji(kaydet.error)}</Uyari></div>}

      <div className="mt-6 flex flex-wrap items-center justify-between gap-3">
        <button className="btn-ikincil" onClick={geri}><ChevronLeft className="h-4 w-4" /> Geri</button>
        <div className="flex items-center gap-3">
          {eksik > 0 ? (
            <span className="text-sm text-amber-700">{eksik} cevap eksik</span>
          ) : (
            <span className="flex items-center gap-1 text-sm text-emerald-600"><CheckCircle2 className="h-4 w-4" /> Tüm cevaplar girildi</span>
          )}
          <button className="btn-birincil" disabled={kaydet.isPending || eksik > 0} onClick={() => kaydet.mutate()}>
            {kaydet.isPending ? <Loader2 className="h-4 w-4 animate-spin" /> : null} Kaydet ve İleri <ChevronRight className="h-4 w-4" />
          </button>
        </div>
      </div>
    </div>
  )
}

function KitapcikKarti({
  harf, cevaplar, puanlar, dinamik, secenekler, sikSec, puanDegistir, optiktenYukle,
}: {
  harf: string
  cevaplar: string[]
  puanlar: number[]
  dinamik: boolean
  secenekler: CevapAnahtari['optikSecenekleri']
  sikSec: (soru: number, sik: string) => void
  puanDegistir: (soru: number, puan: number) => void
  optiktenYukle: (cevaplar: string) => void
}) {
  const [manuel, setManuel] = useState(false)
  const [secili, setSecili] = useState('')
  // Önce bu kitapçığın cevap anahtarı kağıdı, sonra diğer anahtarlar, sonra öğrenci kağıtları
  const sirali = [...secenekler].sort(
    (a, b) => Number(b.cevapAnahtariMi && b.kitapcik === harf) - Number(a.cevapAnahtariMi && a.kitapcik === harf),
  )
  const girilen = cevaplar.filter(Boolean).length

  return (
    <div className="rounded-xl border border-slate-200 p-4">
      <div className="flex items-center justify-between">
        <h3 className="text-lg font-semibold">Kitapçık {harf}</h3>
        <span className="text-xs text-selcuk-metin-soluk">{girilen}/{cevaplar.length} cevap</span>
      </div>

      <div className="mt-3 rounded-xl border border-selcuk-altin bg-amber-50/60 p-3">
        <label className="text-sm font-semibold">
          Optikten seç
          <select
            className={`${kutu} mt-1.5 block w-full`}
            disabled={manuel}
            value={secili}
            onChange={(e) => {
              setSecili(e.target.value)
              const s = secenekler.find((x) => String(x.id) === e.target.value)
              if (s) optiktenYukle(s.cevaplar)
            }}
          >
            <option value="">Satır seçin…</option>
            {sirali.map((s) => <option key={s.id} value={s.id}>{s.etiket}</option>)}
          </select>
        </label>
        <label className="mt-2 flex items-center gap-2 text-sm font-semibold">
          <input type="checkbox" className="h-4 w-4 accent-selcuk-altin" checked={manuel} onChange={(e) => {
            setManuel(e.target.checked)
            if (e.target.checked) setSecili('')
          }} />
          Optik dosyası kullanmadan manuel giriş yap
        </label>
      </div>

      <div className="mt-4 text-sm font-semibold">Sorular ({cevaplar.length})</div>
      <div className="mt-2 max-h-[560px] space-y-2 overflow-y-auto pr-1">
        {cevaplar.map((c, i) => (
          <div key={i} className={`rounded-lg border px-3 py-2.5 ${c ? 'border-slate-200' : 'border-amber-300 bg-amber-50/40'}`}>
            <div className="flex items-center justify-between gap-3">
              <span className="text-sm font-semibold">Soru {i + 1}</span>
              <label className="flex items-center gap-2 text-xs font-semibold text-selcuk-metin-soluk">
                Puan
                {dinamik ? (
                  <span className="w-14 text-right text-sm text-selcuk-metin">{puanYaz(puanlar[i])}</span>
                ) : (
                  <input type="number" min={0} step={0.5} className={`${kutu} w-20 px-2 py-1 text-right`} value={puanlar[i]}
                    aria-label={`Soru ${i + 1} puanı`} onChange={(e) => puanDegistir(i, Number(e.target.value))} />
                )}
              </label>
            </div>
            <div className="mt-2 flex gap-2">
              {SIKLAR.map((s) => {
                const secik = c.includes(s)
                return (
                  <button
                    key={s}
                    type="button"
                    onClick={() => sikSec(i, s)}
                    aria-pressed={secik}
                    aria-label={`Soru ${i + 1} şık ${s}`}
                    className={`h-9 w-10 rounded-lg border text-sm font-bold transition-colors ${
                      secik ? 'border-selcuk-altin bg-selcuk-altin text-selcuk-lacivert' : 'border-slate-200 bg-slate-50 text-selcuk-metin hover:bg-slate-100'
                    }`}
                  >
                    {s}
                  </button>
                )
              })}
            </div>
          </div>
        ))}
      </div>
    </div>
  )
}
