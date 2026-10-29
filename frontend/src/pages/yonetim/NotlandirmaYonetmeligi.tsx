import { useEffect, useState } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { CheckCircle2, Loader2, RotateCcw, Save, XCircle } from 'lucide-react'
import { Uyari } from '../../components/form'
import { api, hataMesaji } from '../../lib/api'

interface HarfAraligi {
  harf: string
  katsayi: number
  altSinir: number
  ustSinir: number
  durum: string
}

interface Notlandirma {
  semaAdi: string
  vizeAgirligi: number
  finalAgirligi: number
  butunlemeAgirligi: number
  finalAltSiniri: number
  harfler: HarfAraligi[]
}

/** Selçuk Üniversitesi Ön Lisans ve Lisans Eğitim-Öğretim ve Sınav Yönetmeliği, Madde 13 */
const YONETMELIK_ALT_SINIRLARI: Record<string, number> = { AA: 88, BA: 80, BB: 73, CB: 66, CC: 60, DC: 55, DD: 50, FF: 0 }

const harfRengi = (durum: string) =>
  durum === 'Başarılı'
    ? 'bg-emerald-50 text-emerald-800 border-emerald-200'
    : durum === 'Şartlı Geçer'
      ? 'bg-amber-50 text-amber-800 border-amber-200'
      : 'bg-red-50 text-red-700 border-red-200'

const sayiKutusu =
  'w-20 rounded-lg border border-slate-300 bg-white px-2 py-2 text-center text-sm font-semibold focus:border-selcuk-mavi focus:outline-none focus:ring-2 focus:ring-selcuk-mavi/20'

export default function NotlandirmaYonetmeligi() {
  const queryClient = useQueryClient()
  const veri = useQuery({
    queryKey: ['ayarlar', 'notlandirma'],
    queryFn: async () => (await api.get<Notlandirma>('/ayarlar/notlandirma')).data,
  })

  const [vize, setVize] = useState(40)
  const [final, setFinal] = useState(60)
  const [altSinir, setAltSinir] = useState(30)
  const [harfler, setHarfler] = useState<HarfAraligi[]>([])

  useEffect(() => {
    if (!veri.data) return
    setVize(veri.data.vizeAgirligi)
    setFinal(veri.data.finalAgirligi)
    setAltSinir(veri.data.finalAltSiniri)
    setHarfler(veri.data.harfler)
  }, [veri.data])

  const kaydet = useMutation({
    meta: { bildirim: 'Notlandırma ayarları kaydedildi' },
    mutationFn: async () =>
      (await api.put<{ mesaj: string }>('/ayarlar/notlandirma', {
        vizeAgirligi: vize,
        finalAgirligi: final,
        finalAltSiniri: altSinir,
        harfler,
      })).data,
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['ayarlar', 'notlandirma'] }),
  })

  /** Bir harfin alt sınırı değişince, bir alttaki harfin üst sınırı ona göre otomatik ayarlanır. */
  const altSiniriDegistir = (i: number, deger: number) =>
    setHarfler((h) =>
      h.map((x, j) => (j === i ? { ...x, altSinir: deger } : j === i + 1 ? { ...x, ustSinir: deger - 1 } : x)),
    )

  const yonetmelikVarsayilanlari = () => {
    setVize(40)
    setFinal(60)
    setAltSinir(30)
    setHarfler((h) =>
      h.map((x, i) => ({
        ...x,
        altSinir: YONETMELIK_ALT_SINIRLARI[x.harf] ?? x.altSinir,
        ustSinir: i === 0 ? 100 : (YONETMELIK_ALT_SINIRLARI[h[i - 1].harf] ?? x.ustSinir + 1) - 1,
      })),
    )
  }

  const toplam = vize + final
  const toplamTamam = toplam === 100

  if (veri.isLoading) return <div className="kart flex justify-center p-10"><Loader2 className="h-6 w-6 animate-spin text-selcuk-mavi" /></div>

  return (
    <div className="space-y-5">
      <div>
        <h2 className="text-xl font-bold text-selcuk-lacivert">Notlandırma Yönetmeliği</h2>
        <p className="text-sm text-selcuk-metin-soluk">
          Vize, final ve bütünleme sınavlarının dönem sonu notunu nasıl etkilediğini yapılandırın.
        </p>
      </div>

      {kaydet.isError && <Uyari tur="hata">{hataMesaji(kaydet.error)}</Uyari>}
      {kaydet.isSuccess && <Uyari tur="basari">{kaydet.data.mesaj}</Uyari>}

      <div className="grid gap-5 xl:grid-cols-2">
        <div className="kart p-5 sm:p-6">
          <h3 className="font-bold">Notlandırma Şeması</h3>
          <p className="mt-1 text-xs text-selcuk-metin-soluk">Mutlak Değerlendirme Sistemi · Selçuk Üniversitesi</p>

          <div className="mt-4 text-xs font-semibold text-selcuk-metin-soluk">Seçili Şema</div>
          <div className="mt-1 rounded-lg bg-selcuk-zemin px-3 py-2.5 text-sm font-semibold">
            {harfler.map((h) => h.harf).join(' – ')}
          </div>

          <table className="mt-4 w-full text-sm">
            <thead>
              <tr className="text-left text-xs text-selcuk-metin-soluk">
                <th className="pb-2 font-semibold">Harf</th>
                <th className="pb-2 font-semibold">Katsayı</th>
                <th className="pb-2 font-semibold">Puan aralığı</th>
                <th className="pb-2 font-semibold">Durum</th>
              </tr>
            </thead>
            <tbody>
              {harfler.map((h, i) => (
                <tr key={h.harf} className="border-t border-slate-100">
                  <td className="py-2">
                    <span className={`inline-block w-10 rounded-md border py-1 text-center text-xs font-bold ${harfRengi(h.durum)}`}>
                      {h.harf}
                    </span>
                  </td>
                  <td className="py-2 font-mono">{h.katsayi.toFixed(2)}</td>
                  <td className="py-2">
                    <div className="flex items-center gap-2">
                      {i === harfler.length - 1 ? (
                        <span className="w-20 text-center font-semibold">0</span>
                      ) : (
                        <input
                          type="number"
                          min={0}
                          max={100}
                          aria-label={`${h.harf} alt sınırı`}
                          className={sayiKutusu}
                          value={h.altSinir}
                          onChange={(e) => altSiniriDegistir(i, Number(e.target.value))}
                        />
                      )}
                      <span className="text-selcuk-metin-soluk">–</span>
                      <span className="w-8 font-semibold">{h.ustSinir}</span>
                    </div>
                  </td>
                  <td className="py-2 text-xs text-selcuk-metin-soluk">{h.durum}</td>
                </tr>
              ))}
            </tbody>
          </table>
          <p className="mt-3 text-xs text-selcuk-metin-soluk">
            DC ve DD notları, öğrencinin dönem ağırlıklı not ortalaması 2,00 ve üzerindeyse geçer sayılır.
          </p>
        </div>

        <div className="kart p-5 sm:p-6">
          <h3 className="font-bold">Sınav Ağırlıkları</h3>
          <div className="mt-5 space-y-4">
            <AgirlikSatiri ad="Vize Ağırlığı" deger={vize} degistir={setVize} />
            <AgirlikSatiri ad="Final Ağırlığı" deger={final} degistir={setFinal} />
            <AgirlikSatiri ad="Bütünleme Ağırlığı" deger={final} pasif />
          </div>

          <div className="mt-5 border-t border-slate-100 pt-4">
            <div className={`flex items-center gap-2 text-sm font-semibold ${toplamTamam ? 'text-emerald-600' : 'text-red-600'}`}>
              {toplamTamam ? <CheckCircle2 className="h-4 w-4" /> : <XCircle className="h-4 w-4" />}
              Vize + Final Toplamı: %{toplam}
            </div>
            <p className="mt-1 text-sm text-selcuk-metin-soluk">
              Not: Bütünleme, final sınavının yerini alır ve aynı ağırlığa sahiptir.
            </p>
          </div>

          <div className="mt-5 border-t border-slate-100 pt-4">
            <div className="flex items-center justify-between gap-4">
              <div>
                <div className="text-sm font-semibold">Final / Bütünleme Alt Sınırı</div>
                <p className="text-xs text-selcuk-metin-soluk">Bu puanın altında kalan öğrenci ortalamasına bakılmaksızın FF alır.</p>
              </div>
              <input type="number" min={0} max={100} className={sayiKutusu} value={altSinir}
                aria-label="Final alt sınırı" onChange={(e) => setAltSinir(Number(e.target.value))} />
            </div>
          </div>
        </div>
      </div>

      <div className="flex flex-wrap justify-center gap-3">
        <button className="btn-birincil px-6" disabled={!toplamTamam || kaydet.isPending} onClick={() => kaydet.mutate()}>
          {kaydet.isPending ? <Loader2 className="h-4 w-4 animate-spin" /> : <Save className="h-4 w-4" />} Kaydet
        </button>
        <button className="btn-ikincil" onClick={yonetmelikVarsayilanlari}>
          <RotateCcw className="h-4 w-4" /> Yönetmelik Varsayılanlarına Dön
        </button>
      </div>
    </div>
  )
}

function AgirlikSatiri({ ad, deger, degistir, pasif }: { ad: string; deger: number; degistir?: (n: number) => void; pasif?: boolean }) {
  return (
    <div className="flex items-center justify-between gap-4">
      <span className={`text-sm font-semibold ${pasif ? 'text-selcuk-metin-soluk' : ''}`}>{ad}</span>
      <div className="flex items-center gap-2">
        <input
          type="number"
          min={0}
          max={100}
          aria-label={ad}
          className={`${sayiKutusu} ${pasif ? 'bg-slate-50 text-slate-400' : ''}`}
          value={deger}
          disabled={pasif}
          onChange={(e) => degistir?.(Number(e.target.value))}
        />
        <span className="text-selcuk-metin-soluk">%</span>
      </div>
    </div>
  )
}
