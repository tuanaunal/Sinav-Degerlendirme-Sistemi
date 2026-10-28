import { useEffect, useMemo, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { BookOpen, CheckCircle2, ChevronLeft, Download, Info, ListOrdered, Loader2, Plus, RotateCcw, Save, Trash2 } from 'lucide-react'
import { Uyari } from '../../../components/form'
import { api, hataMesaji } from '../../../lib/api'
import { sinavTuruYolu } from '../../../lib/dersler'
import { sinavAnahtari, type SinavBilgisi } from '../../../lib/sinav'

interface KitapcikEslestirme {
  soruSayisi: number
  gerekli: boolean
  kaydedildi: boolean
  kitapciklar: { kitapcik: string; anaSoruNolari: (number | null)[] }[]
}
interface Cikti {
  id: number
  siraNo: number
  aciklama: string
  sorular: number[]
}
interface CiktiDurumu {
  soruSayisi: number
  kitapcikEslestirmeGerekli: boolean
  kitapcikEslestirmeKaydedildi: boolean
  ciktilar: Cikti[]
  getirilebilecekSayisi: number
  getirilecekDonem: string | null
}

const secimKutusu =
  'w-14 rounded-md border border-slate-300 bg-white px-1 py-1 text-center text-xs font-semibold focus:border-selcuk-mavi focus:outline-none focus:ring-2 focus:ring-selcuk-mavi/20'

/** 5. adım: kitapçık soru eşleştirmesi ve öğrenme çıktısı - soru ilişkilendirmesi. "Kaydet ve Analizi Gör" sınavı tamamlar. */
export default function Adim5OgrenmeCiktilari({ sinav, geri }: { sinav: SinavBilgisi; geri: () => void }) {
  return (
    <div className="space-y-5">
      {sinav.kitapcikSayisi > 1 && <KitapcikSoruEslestirme sinav={sinav} />}
      <OgrenmeCiktilari sinav={sinav} geri={geri} />
    </div>
  )
}

/**
 * Sütunlar A kitapçığının soruları; her kitapçık satırında o A sorusunun bu kitapçıkta kaçıncı soru olduğu seçilir.
 * API kitapçık sırasıyla (B'nin i. sorusu → A numarası) çalışır; burada ters çevrilerek gösterilir.
 */
function KitapcikSoruEslestirme({ sinav }: { sinav: SinavBilgisi }) {
  const queryClient = useQueryClient()
  const veri = useQuery({
    queryKey: ['kitapcik-eslestirme', sinav.id],
    queryFn: async () => (await api.get<KitapcikEslestirme>(`/sinavlar/${sinav.id}/kitapcik-eslestirme`)).data,
  })
  const n = sinav.soruSayisi
  // secim[kitapcik][a-1] = o kitapçıktaki soru no
  const [secim, setSecim] = useState<Record<string, (number | null)[]>>({})

  useEffect(() => {
    if (!veri.data) return
    const s: Record<string, (number | null)[]> = {}
    for (const k of veri.data.kitapciklar) {
      const satir = Array<number | null>(n).fill(null)
      k.anaSoruNolari.forEach((a, i) => { if (a) satir[a - 1] = i + 1 })
      s[k.kitapcik] = satir
    }
    setSecim(s)
  }, [veri.data, n])

  const kaydet = useMutation({
    mutationFn: async () => {
      const kitapciklar = Object.entries(secim).map(([kitapcik, satir]) => {
        const anaSoruNolari = Array<number | null>(n).fill(null)
        satir.forEach((k, a) => { if (k) anaSoruNolari[k - 1] = a + 1 })
        return { kitapcik, anaSoruNolari }
      })
      return (await api.put<KitapcikEslestirme>(`/sinavlar/${sinav.id}/kitapcik-eslestirme`, { kitapciklar })).data
    },
    onSuccess: (d) => {
      queryClient.setQueryData(['kitapcik-eslestirme', sinav.id], d)
      queryClient.invalidateQueries({ queryKey: ['ogrenme-ciktilari', sinav.id] })
      queryClient.invalidateQueries({ queryKey: ['notlar', sinav.id] })
    },
  })

  const degistir = (k: string, a: number, deger: number | null) =>
    setSecim((s) => ({ ...s, [k]: s[k].map((x, i) => (i === a ? deger : x)) }))
  const eksik = Object.values(secim).reduce((t, satir) => t + satir.filter((x) => !x).length, 0)
  const degisti = veri.data && !kaydet.isPending &&
    JSON.stringify(secim) !== JSON.stringify(Object.fromEntries(veri.data.kitapciklar.map((k) => {
      const satir = Array<number | null>(n).fill(null)
      k.anaSoruNolari.forEach((a, i) => { if (a) satir[a - 1] = i + 1 })
      return [k.kitapcik, satir]
    })))
  const kayitli = veri.data?.kaydedildi && !degisti

  if (veri.isLoading) return <div className="kart flex justify-center p-8"><Loader2 className="h-6 w-6 animate-spin text-selcuk-mavi" /></div>

  return (
    <div className="kart p-5 sm:p-6">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <h2 className="flex items-center gap-2 text-lg font-bold text-selcuk-lacivert">
          <BookOpen className="h-5 w-5" /> Kitapçık Soru Eşleştirme
        </h2>
        <div className="flex items-center gap-2">
          {kayitli ? (
            <span className="flex items-center gap-1 text-sm font-semibold text-emerald-600"><CheckCircle2 className="h-4 w-4" /> Kaydedildi</span>
          ) : (
            <span className="text-xs text-selcuk-metin-soluk">{eksik > 0 ? `${eksik} soru eşleştirilmedi` : 'Tüm numaralar dolduruldu'}</span>
          )}
          <button className="btn-ikincil px-2.5 py-1.5" title="Sırayla doldur (1→1, 2→2…)" aria-label="Sırayla doldur"
            onClick={() => setSecim((s) => Object.fromEntries(Object.keys(s).map((k) => [k, Array.from({ length: n }, (_, i) => i + 1)])))}>
            <ListOrdered className="h-4 w-4" />
          </button>
          <button className="btn-ikincil px-2.5 py-1.5" title="Temizle" aria-label="Temizle"
            onClick={() => setSecim((s) => Object.fromEntries(Object.keys(s).map((k) => [k, Array<number | null>(n).fill(null)])))}>
            <RotateCcw className="h-4 w-4" />
          </button>
          <button className="btn-birincil py-1.5" disabled={eksik > 0 || kaydet.isPending || !!kayitli} onClick={() => kaydet.mutate()}>
            {kaydet.isPending ? <Loader2 className="h-4 w-4 animate-spin" /> : <Save className="h-4 w-4" />} Kaydet
          </button>
        </div>
      </div>
      <p className="mt-1 text-sm text-selcuk-metin-soluk">
        Her A kitapçığı sorusunun diğer kitapçıklarda kaçıncı soru olduğunu seçin. Analizler A kitapçığı sırasına göre yapılır.
      </p>

      {kaydet.isError && <div className="mt-3"><Uyari tur="hata">{hataMesaji(kaydet.error)}</Uyari></div>}

      <div className="mt-4 overflow-x-auto pb-2">
        <table className="text-sm">
          <thead>
            <tr>
              <th className="sticky left-0 bg-white pr-3 text-left text-xs font-semibold text-red-700">A Kitapçık</th>
              {Array.from({ length: n }, (_, i) => <th key={i} className="px-1 pb-1 text-center text-xs font-bold">{i + 1}</th>)}
            </tr>
          </thead>
          <tbody>
            {Object.entries(secim).map(([k, satir]) => {
              const kullanilan = new Set(satir.filter(Boolean))
              return (
                <tr key={k}>
                  <td className="sticky left-0 bg-white pr-3 text-xs font-semibold">{k} Kitapçık</td>
                  {satir.map((deger, a) => (
                    <td key={a} className="px-1 py-1">
                      <select className={`${secimKutusu} ${deger ? 'border-selcuk-altin bg-amber-50' : ''}`} value={deger ?? ''} aria-label={`A${a + 1} için ${k} kitapçığı sorusu`}
                        onChange={(e) => degistir(k, a, e.target.value ? Number(e.target.value) : null)}>
                        <option value="">–</option>
                        {Array.from({ length: n }, (_, i) => i + 1).map((no) => (
                          <option key={no} value={no} disabled={kullanilan.has(no) && no !== deger}>{no}</option>
                        ))}
                      </select>
                    </td>
                  ))}
                </tr>
              )
            })}
          </tbody>
        </table>
      </div>
    </div>
  )
}

function OgrenmeCiktilari({ sinav, geri }: { sinav: SinavBilgisi; geri: () => void }) {
  const queryClient = useQueryClient()
  const navigate = useNavigate()
  const anahtar = ['ogrenme-ciktilari', sinav.id]
  const veri = useQuery({
    queryKey: anahtar,
    queryFn: async () => (await api.get<CiktiDurumu>(`/sinavlar/${sinav.id}/ogrenme-ciktilari`)).data,
  })
  const [yeni, setYeni] = useState('')
  const [matris, setMatris] = useState<Record<number, Set<number>>>({})

  useEffect(() => {
    if (veri.data) setMatris((m) => Object.fromEntries(veri.data.ciktilar.map((c) => [c.id, m[c.id] ?? new Set(c.sorular)])))
  }, [veri.data])

  const islem = useMutation({
    mutationFn: async (f: () => Promise<{ data: CiktiDurumu }>) => (await f()).data,
    onSuccess: (d) => queryClient.setQueryData(anahtar, d),
  })

  const kaydet = useMutation({
    mutationFn: async (tamamla: boolean) =>
      (await api.put<CiktiDurumu>(`/sinavlar/${sinav.id}/ogrenme-ciktilari/eslestirme`, {
        tamamla,
        eslestirmeler: Object.entries(matris).map(([id, s]) => ({ ciktiId: Number(id), sorular: [...s].sort((a, b) => a - b) })),
      })).data,
    onSuccess: (d, tamamla) => {
      queryClient.setQueryData(anahtar, d)
      if (tamamla) {
        queryClient.setQueryData(sinavAnahtari(sinav.dersId, sinav.tur), { ...sinav, tamamlananAdim: 5, durum: 'Tamamlandi' })
        queryClient.invalidateQueries({ queryKey: ['dersler'] })
        navigate(`/dersler/${sinav.dersId}/sinav/${sinavTuruYolu(sinav.tur)}/analiz`)
      }
    },
  })

  const kilitli = !!veri.data && veri.data.kitapcikEslestirmeGerekli && !veri.data.kitapcikEslestirmeKaydedildi
  const n = sinav.soruSayisi
  const bosCikti = useMemo(() => (veri.data?.ciktilar ?? []).filter((c) => !matris[c.id]?.size), [veri.data, matris])
  const iliskili = (veri.data?.ciktilar.length ?? 0) - bosCikti.length

  const tikla = (id: number, soru: number) =>
    setMatris((m) => {
      const s = new Set(m[id])
      if (s.has(soru)) s.delete(soru)
      else s.add(soru)
      return { ...m, [id]: s }
    })

  if (veri.isLoading) return <div className="kart flex justify-center p-8"><Loader2 className="h-6 w-6 animate-spin text-selcuk-mavi" /></div>
  if (veri.isError || !veri.data) return <Uyari tur="hata">{hataMesaji(veri.error)}</Uyari>
  const d = veri.data

  return (
    <>
      {kilitli && <Uyari tur="uyari">Kazanım girişi için önce kitapçık soru eşleştirmesini tamamlayıp kaydedin.</Uyari>}

      <div className={`kart p-5 sm:p-6 ${kilitli ? 'pointer-events-none opacity-50' : ''}`} aria-disabled={kilitli}>
        <h2 className="text-lg font-bold text-selcuk-lacivert">Yeni Öğrenme Çıktısı Ekle</h2>
        <label className="mt-3 block text-sm font-semibold" htmlFor="cikti">Çıktı Açıklaması</label>
        <textarea id="cikti" rows={2} value={yeni} onChange={(e) => setYeni(e.target.value)}
          placeholder="ör: Programlama temel kavramlarını anlama ve uygulama"
          className="mt-1.5 w-full rounded-lg border border-slate-300 px-3 py-2.5 text-sm focus:border-selcuk-mavi focus:outline-none focus:ring-2 focus:ring-selcuk-mavi/20" />
        <div className="mt-3 flex flex-wrap gap-2">
          <button className="btn-birincil" disabled={yeni.trim().length < 5 || islem.isPending}
            onClick={() => islem.mutate(() => api.post(`/sinavlar/${sinav.id}/ogrenme-ciktilari`, { aciklama: yeni }), { onSuccess: () => setYeni('') })}>
            <Plus className="h-4 w-4" /> Öğrenme Çıktısı Ekle
          </button>
          <button className="btn-birincil" disabled={d.getirilebilecekSayisi === 0 || islem.isPending}
            title={d.getirilecekDonem ? `${d.getirilecekDonem} döneminden ${d.getirilebilecekSayisi} çıktı` : 'Önceki dönemlerde tanımlı çıktı yok'}
            onClick={() => islem.mutate(() => api.post(`/sinavlar/${sinav.id}/ogrenme-ciktilari/getir`))}>
            <Download className="h-4 w-4" /> Öğrenme Çıktılarını Getir
          </button>
        </div>
        {d.getirilebilecekSayisi > 0 && (
          <p className="mt-2 flex items-center gap-1 text-xs text-selcuk-metin-soluk">
            <Info className="h-3.5 w-3.5" /> Bu dersin {d.getirilecekDonem} döneminde tanımlı {d.getirilebilecekSayisi} öğrenme çıktısı getirilebilir.
          </p>
        )}
        {islem.isError && <div className="mt-3"><Uyari tur="hata">{hataMesaji(islem.error)}</Uyari></div>}
      </div>

      {d.ciktilar.length === 0 ? (
        <Uyari tur="uyari">Analiz için en az bir öğrenme çıktısı eklemelisiniz.</Uyari>
      ) : (
        <div className={`kart p-5 sm:p-6 ${kilitli ? 'pointer-events-none opacity-50' : ''}`}>
          <h2 className="text-lg font-bold text-selcuk-lacivert">Öğrenme Çıktıları - Soru İlişkilendirmesi</h2>
          <p className="mt-1 text-sm text-selcuk-metin-soluk">Her çıktının hangi sorularla ölçüldüğünü işaretleyin (A kitapçığı soru numaraları).</p>
          <div className="mt-4 overflow-x-auto pb-2">
            <table className="text-sm">
              <thead>
                <tr className="border-b border-slate-200">
                  <th className="sticky left-0 min-w-[260px] bg-white py-2 pr-3 text-left font-semibold">Öğrenme Çıktısı</th>
                  {Array.from({ length: n }, (_, i) => <th key={i} className="w-9 py-2 text-center text-xs font-bold">{i + 1}</th>)}
                  <th className="w-10" />
                </tr>
              </thead>
              <tbody>
                {d.ciktilar.map((c) => (
                  <tr key={c.id} className={`border-b border-slate-100 ${matris[c.id]?.size ? '' : 'bg-amber-50/50'}`}>
                    <td className="sticky left-0 max-w-[320px] bg-inherit py-2 pr-3 text-xs leading-snug">
                      <span className="font-semibold">ÖÇ{c.siraNo}.</span> {c.aciklama}
                    </td>
                    {Array.from({ length: n }, (_, i) => (
                      <td key={i} className="text-center">
                        <input type="checkbox" className="h-4 w-4 accent-selcuk-altin" checked={!!matris[c.id]?.has(i + 1)}
                          aria-label={`ÖÇ${c.siraNo} - Soru ${i + 1}`} onChange={() => tikla(c.id, i + 1)} />
                      </td>
                    ))}
                    <td className="text-center">
                      <button className="rounded p-1 text-slate-400 hover:bg-red-50 hover:text-red-600" aria-label={`ÖÇ${c.siraNo} sil`}
                        onClick={() => islem.mutate(() => api.delete(`/sinavlar/${sinav.id}/ogrenme-ciktilari/${c.id}`))}>
                        <Trash2 className="h-4 w-4" />
                      </button>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
          {bosCikti.length > 0 && iliskili > 0 && (
            <p className="mt-2 text-xs text-amber-700">
              {bosCikti.map((c) => `ÖÇ${c.siraNo}`).join(', ')} henüz hiçbir soruyla ilişkilendirilmedi; analizde gösterilmeyecek.
            </p>
          )}
        </div>
      )}

      {kaydet.isError && <Uyari tur="hata">{hataMesaji(kaydet.error)}</Uyari>}
      {kaydet.isSuccess && !kaydet.variables && <Uyari tur="basari">İlişkilendirmeler kaydedildi.</Uyari>}

      <div className="flex flex-wrap items-center justify-between gap-3">
        <button className="btn-ikincil" onClick={geri}><ChevronLeft className="h-4 w-4" /> Geri</button>
        <div className="flex flex-wrap gap-2">
          <button className="btn-ikincil" disabled={kilitli || kaydet.isPending} onClick={() => kaydet.mutate(false)}>
            <Save className="h-4 w-4" /> Kaydet
          </button>
          <button className="btn-birincil px-6" disabled={kilitli || iliskili === 0 || kaydet.isPending} onClick={() => kaydet.mutate(true)}>
            {kaydet.isPending && <Loader2 className="h-4 w-4 animate-spin" />} Kaydet ve Analizi Gör
          </button>
        </div>
      </div>
    </>
  )
}
