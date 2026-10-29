import { useEffect, useMemo, useState } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { Loader2, Save } from 'lucide-react'
import { Uyari } from '../../components/form'
import { api, hataMesaji } from '../../lib/api'

type AlanAdi = 'OgrenciNo' | 'AdSoyad' | 'KitapcikTuru' | 'SinavTuru' | 'Cevaplar'

interface OptikAlan {
  alan: AlanAdi
  baslangic: number
  bitis: number | null
}

const alanBilgisi: Record<AlanAdi, { ad: string; renk: string; nokta: string }> = {
  OgrenciNo: { ad: 'Öğrenci Numarası', renk: 'bg-blue-100 text-blue-900', nokta: 'bg-blue-500' },
  SinavTuru: { ad: 'Sınav Türü', renk: 'bg-purple-100 text-purple-900', nokta: 'bg-purple-500' },
  AdSoyad: { ad: 'İsim Soyisim', renk: 'bg-emerald-100 text-emerald-900', nokta: 'bg-emerald-500' },
  KitapcikTuru: { ad: 'Kitapçık Türü', renk: 'bg-amber-100 text-amber-900', nokta: 'bg-amber-500' },
  Cevaplar: { ad: 'Cevaplar', renk: 'bg-rose-100 text-rose-900', nokta: 'bg-rose-500' },
}

const siralama: AlanAdi[] = ['OgrenciNo', 'AdSoyad', 'KitapcikTuru', 'SinavTuru', 'Cevaplar']

const ORNEK_SATIR = '2133010421' + 'AYŞE DEMİR'.padEnd(21, ' ') + 'BACEDBBADCEABDCEBADCBAEDCAB'

const sayiKutusu =
  'w-full rounded-lg border border-slate-300 bg-white px-3 py-2 text-sm focus:border-selcuk-mavi focus:outline-none focus:ring-2 focus:ring-selcuk-mavi/20'

export default function OptikFormAyarlari() {
  const queryClient = useQueryClient()
  const veri = useQuery({
    queryKey: ['ayarlar', 'optik-form'],
    queryFn: async () => (await api.get<OptikAlan[]>('/ayarlar/optik-form')).data,
  })
  const [alanlar, setAlanlar] = useState<OptikAlan[]>([])
  const [ornek, setOrnek] = useState(ORNEK_SATIR)

  useEffect(() => {
    if (veri.data) setAlanlar(siralama.map((a) => veri.data.find((x) => x.alan === a)!).filter(Boolean))
  }, [veri.data])

  const kaydet = useMutation({
    meta: { bildirim: 'Optik form ayarları kaydedildi' },
    mutationFn: async () => (await api.put<{ mesaj: string }>('/ayarlar/optik-form', alanlar)).data,
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['ayarlar', 'optik-form'] }),
  })

  const guncelle = (alan: AlanAdi, anahtar: 'baslangic' | 'bitis', deger: string) =>
    setAlanlar((l) => l.map((x) => (x.alan === alan ? { ...x, [anahtar]: deger === '' ? 0 : Number(deger) } : x)))

  /** Örnek satırın her karakterinin hangi alana düştüğü (önizleme için). */
  const karakterAlani = useMemo(() => {
    return [...ornek].map((_, i) => {
      const a = alanlar.find((x) => i >= x.baslangic && (x.bitis === null ? true : i <= x.bitis))
      return a?.alan
    })
  }, [ornek, alanlar])

  const ayristir = (a: OptikAlan) => ornek.slice(a.baslangic, a.bitis === null ? undefined : a.bitis + 1)

  if (veri.isLoading) return <div className="kart flex justify-center p-10"><Loader2 className="h-6 w-6 animate-spin text-selcuk-mavi" /></div>

  return (
    <div className="space-y-5">
      <div className="kart p-5 sm:p-6">
        <h2 className="text-lg font-bold text-selcuk-lacivert">Optik Form Ayarları</h2>
        <p className="mt-1 text-sm text-selcuk-metin-soluk">
          Optik form dosyanızdaki alanların başlangıç ve bitiş indislerini (0 tabanlı) giriniz. Örneğin bir alan 0'da başlayıp
          8'de bitiyorsa Başlangıç: 0, Bitiş: 8 giriniz. Kurum farklı bir optik form kullanmaya başlarsa yalnızca bu ayarlar güncellenir.
        </p>

        {kaydet.isError && <div className="mt-4"><Uyari tur="hata">{hataMesaji(kaydet.error)}</Uyari></div>}
        {kaydet.isSuccess && <div className="mt-4"><Uyari tur="basari">{kaydet.data.mesaj}</Uyari></div>}

        <div className="mt-5 space-y-3">
          {alanlar.map((a) => (
            <div key={a.alan} className="grid items-center gap-3 rounded-xl border border-slate-200 bg-selcuk-zemin/60 p-4 sm:grid-cols-[1fr_160px_160px]">
              <div className="flex items-center gap-2">
                <span className={`h-3 w-3 rounded-full ${alanBilgisi[a.alan].nokta}`} />
                <span className="font-semibold">{alanBilgisi[a.alan].ad}</span>
              </div>
              <label className="text-xs font-semibold text-selcuk-metin-soluk">
                Başlangıç İndisi
                <input type="number" min={0} className={`${sayiKutusu} mt-1`} value={a.baslangic}
                  onChange={(e) => guncelle(a.alan, 'baslangic', e.target.value)} />
              </label>
              <label className="text-xs font-semibold text-selcuk-metin-soluk">
                Bitiş İndisi (Dahil)
                {a.alan === 'Cevaplar' ? (
                  <div className="mt-1 rounded-lg border border-dashed border-slate-300 px-3 py-2 text-sm font-normal">
                    Soru sayısı kadar
                  </div>
                ) : (
                  <input type="number" min={0} className={`${sayiKutusu} mt-1`} value={a.bitis ?? ''}
                    onChange={(e) => guncelle(a.alan, 'bitis', e.target.value)} />
                )}
              </label>
            </div>
          ))}
        </div>

        <div className="mt-5 flex justify-end">
          <button className="btn-birincil px-6" disabled={kaydet.isPending} onClick={() => kaydet.mutate()}>
            {kaydet.isPending ? <Loader2 className="h-4 w-4 animate-spin" /> : <Save className="h-4 w-4" />} Kaydet
          </button>
        </div>
      </div>

      <div className="kart p-5 sm:p-6">
        <h3 className="font-bold">Örnek Satırla Dene</h3>
        <p className="mt-1 text-sm text-selcuk-metin-soluk">
          Optik dosyanızdan bir satır yapıştırın; ayarların bu satırı nasıl okuyacağını kaydetmeden önce görün.
        </p>
        <input
          className={`${sayiKutusu} mt-3 font-mono`}
          value={ornek}
          spellCheck={false}
          aria-label="Örnek optik satırı"
          onChange={(e) => setOrnek(e.target.value)}
        />
        <div className="mt-3 overflow-x-auto rounded-lg bg-slate-900 p-3">
          <div className="flex w-max font-mono text-sm leading-6">
            {[...ornek].map((c, i) => {
              const alan = karakterAlani[i]
              return (
                <span key={i} title={`${i}`} className={`inline-block w-[1.1ch] text-center ${alan ? alanBilgisi[alan].renk : 'text-slate-500'}`}>
                  {c === ' ' ? ' ' : c}
                </span>
              )
            })}
          </div>
        </div>
        <dl className="mt-4 grid gap-2 sm:grid-cols-2">
          {alanlar.map((a) => (
            <div key={a.alan} className="rounded-lg border border-slate-200 px-3 py-2">
              <dt className="flex items-center gap-1.5 text-xs font-semibold text-selcuk-metin-soluk"><span className={`h-2 w-2 rounded-full ${alanBilgisi[a.alan].nokta}`} />{alanBilgisi[a.alan].ad}</dt>
              <dd className="mt-0.5 break-all font-mono text-sm">{ayristir(a).trim() || <span className="text-slate-400">(boş)</span>}</dd>
            </div>
          ))}
        </dl>
      </div>
    </div>
  )
}
