import { useMemo, useState } from 'react'
import { Link, useParams } from 'react-router-dom'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import {
  ArrowLeft, BarChart3, CheckCircle2, Download, FileText, ListChecks, Loader2, Printer, Save, Search, Sparkles, Target, TrendingUp, Users,
} from 'lucide-react'
import Sayfa from '../../components/layout/Sayfa'
import { Uyari } from '../../components/form'
import { CubukGrafik, OranCubugu, YuzdeHalkasi } from '../../components/grafikler'
import { api, dosyaIndir, hataMesaji } from '../../lib/api'
import { dersSorgulari, sinavFormatiAdi, sinavTuruAdi, yoldanSinavTuru, type SinavFormati, type SinavTuru } from '../../lib/dersler'
import { puanYaz, sinavSorgusu } from '../../lib/sinav'

interface Ozet {
  toplam: number; giren: number; ortalama: number | null; medyan: number | null
  standartSapma: number | null; enYuksek: number | null; enDusuk: number | null
}
interface SoruAnalizi {
  soruNo: number; puan: number; dogruCevap: string | null; basariOrani: number; ortalamaPuan: number
  dogru: number; yanlis: number; bos: number; sikOranlari: Record<string, number>
}
interface Analiz {
  genel: {
    dersKodu: string; dersAdi: string; akademikYil: string; donem: string; tur: SinavTuru; format: SinavFormati
    soruSayisi: number; maksimumPuan: number; kitapcikSayisi: number; tarih: string | null
  }
  ozet: Ozet
  basariOrani: number | null
  gecmeSiniri: number
  puanDagilimi: { etiket: string; alt: number; ust: number; sayi: number }[]
  sorular: SoruAnalizi[]
  ciktilar: { siraNo: number; aciklama: string; sorular: number[]; basariOrani: number | null }[]
  harfDagilimi: { harf: string; durum: string; sayi: number }[]
  ogrenciler: { sonucId: number; ogrenciNo: string; adSoyad: string; sinavaGirdi: boolean }[]
}
interface OgrenciAnalizi {
  ogrenciNo: string; adSoyad: string; sinavaGirdi: boolean; kitapcik: string | null
  hamPuan: number; duzeltilmisPuan: number | null; harfNotu: string | null; eklemeler: number
  sorular: { soruNo: number; verilen: string | null; dogruCevap: string | null; dogruMu: boolean | null; puan: number; maksPuan: number }[]
}

const HARF_RENGI: Record<string, string> = { 'Başarılı': '#059669', 'Şartlı Geçer': '#D97706', 'Başarısız': '#DC2626' }

/** Sınav Başarı Analiz Raporu: sınav girişi biter bitmez açılan, sınıfın genel durumunu özetleyen rapor. */
export default function SinavAnalizi() {
  const { dersId, tur } = useParams()
  const sinavTuru = yoldanSinavTuru(tur)
  const ders = useQuery(dersSorgulari.detay(Number(dersId)))
  const sinav = useQuery({ ...sinavSorgusu(Number(dersId), tur ?? 'vize'), enabled: !!sinavTuru })
  const analiz = useQuery({
    queryKey: ['analiz', sinav.data?.id],
    enabled: !!sinav.data,
    queryFn: async () => (await api.get<Analiz>(`/sinavlar/${sinav.data!.id}/analiz`)).data,
  })
  const [indirHata, setIndirHata] = useState<string | null>(null)

  const breadcrumb = [
    { etiket: 'Sınav Değerlendirme Sistemi', yol: '/panom' },
    { etiket: ders.data?.ad ?? 'Ders', yol: `/dersler/${dersId}` },
    { etiket: `Analiz Raporu - ${sinavTuru ? sinavTuruAdi[sinavTuru] : ''}` },
  ]

  if (sinav.isLoading || analiz.isLoading || ders.isLoading) {
    return <Sayfa breadcrumb={breadcrumb}><div className="flex justify-center p-10"><Loader2 className="h-6 w-6 animate-spin text-selcuk-mavi" /></div></Sayfa>
  }
  if (!sinav.data) {
    return (
      <Sayfa breadcrumb={breadcrumb}>
        <Uyari tur="uyari">Bu sınav için henüz giriş yapılmamış.</Uyari>
        <Link to={`/dersler/${dersId}`} className="btn-ikincil mt-4">Derse Geri Dön</Link>
      </Sayfa>
    )
  }
  if (analiz.isError || !analiz.data) {
    return (
      <Sayfa breadcrumb={breadcrumb}>
        <Uyari tur="hata">{hataMesaji(analiz.error ?? sinav.error)}</Uyari>
        <Link to={`/dersler/${dersId}`} className="btn-ikincil mt-4">Derse Geri Dön</Link>
      </Sayfa>
    )
  }

  const a = analiz.data
  const g = a.genel
  const acikUclu = g.format === 'AcikUclu'

  const puanDagilimi = (
  <section className={`kart p-5 ${acikUclu ? '' : 'mt-5'}`}>
      <h2 className="font-bold">Puan Dağılımı Grafiği</h2>
      <p className="text-sm text-selcuk-metin-soluk">Öğrenci puan aralıklarına göre frekans dağılımı (100 üzerinden)</p>
      <div className="mt-4">
        <CubukGrafik baslik="Puan dağılımı" veri={a.puanDagilimi.map((d) => ({ etiket: d.etiket, deger: d.sayi, aciklama: `Öğrenci sayısı: ${d.sayi}` }))} />
      </div>
      <div className="mt-5 border-t border-slate-100 pt-4">
        <div className="text-xs font-semibold uppercase tracking-wide text-selcuk-metin-soluk">{acikUclu ? 'Sınav Detayları' : 'Sınav İstatistik Detayları'}</div>
        <div className="mt-2 grid grid-cols-2 gap-2 sm:grid-cols-4">
          {[['Min', a.ozet.enDusuk], ['Maks', a.ozet.enYuksek], ['Medyan', a.ozet.medyan], ['Std Sapma', a.ozet.standartSapma]].map(([k, v]) => (
            <div key={k as string} className="rounded-lg border border-slate-200 bg-selcuk-zemin/60 py-2 text-center">
              <div className="text-xs font-semibold text-selcuk-metin-soluk">{k}</div>
              <div className="font-bold">{sayi(v as number | null)}</div>
            </div>
          ))}
        </div>
      </div>
    </section>
  )

  return (
    <Sayfa breadcrumb={breadcrumb}>
      <div className="flex flex-wrap items-start justify-between gap-4">
        <div>
          <h1 className="text-2xl font-bold text-selcuk-lacivert">{acikUclu ? 'Açık Uçlu Sınav Analizi' : 'Sınav Başarı Analiz Raporu'}</h1>
          <p className="mt-1 text-sm text-selcuk-metin-soluk">{g.dersKodu} — {g.dersAdi} / {sinavTuruAdi[g.tur]}</p>
          {sinav.data.durum !== 'Tamamlandi' && (
            <p className="mt-1 text-xs text-amber-700">Sınav girişi henüz tamamlanmadı; öğrenme çıktısı analizi eksik olabilir.</p>
          )}
        </div>
        <div className="yazdirma-gizle flex gap-2">
          <button className="btn-ikincil" onClick={() => window.print()}><Printer className="h-4 w-4" /> Yazdır</button>
          <button className="btn-birincil" onClick={() => dosyaIndir(`/sinavlar/${sinav.data!.id}/resmi-form`, 'Sinav_Basarisi_Analiz_Formu.xlsx').catch((e) => setIndirHata(hataMesaji(e)))}>
            <Download className="h-4 w-4" /> Excel İndir
          </button>
        </div>
      </div>
      {indirHata && <div className="mt-3"><Uyari tur="hata">{indirHata}</Uyari></div>}

      {/* İstatistik kartları */}
      <div className={`mt-5 grid grid-cols-2 gap-3 ${acikUclu ? 'lg:grid-cols-4' : 'md:grid-cols-3 xl:grid-cols-6'}`}>
        <Kart etiket="Toplam Öğrenci" deger={`${a.ozet.toplam}`} alt={`${a.ozet.giren} giren`} Ikon={Users} renk="bg-blue-50 text-selcuk-mavi" />
        <Kart etiket="Ortalama Puan" deger={sayi(a.ozet.ortalama)} Ikon={TrendingUp} renk="bg-indigo-50 text-indigo-600" />
        <Kart etiket="Başarı Oranı" deger={a.basariOrani === null ? '—' : `%${puanYaz(a.basariOrani)}`} alt={`${a.gecmeSiniri} ve üzeri`} Ikon={CheckCircle2} renk="bg-emerald-50 text-emerald-600" />
        {acikUclu ? (
          <Kart etiket="Soru Sayısı" deger={`${g.soruSayisi}`} alt={`${puanYaz(g.maksimumPuan)} puan üzerinden`} Ikon={ListChecks} renk="bg-amber-50 text-amber-600" />
        ) : (
          <>
            <Kart etiket="Medyan" deger={sayi(a.ozet.medyan)} Ikon={Target} renk="bg-amber-50 text-amber-600" />
            <Kart etiket="Standart Sapma" deger={sayi(a.ozet.standartSapma)} Ikon={FileText} renk="bg-purple-50 text-purple-600" />
            <Kart etiket="Min / Maks" deger={`${sayi(a.ozet.enDusuk)} / ${sayi(a.ozet.enYuksek)}`} Ikon={BarChart3} renk="bg-rose-50 text-rose-600" />
          </>
        )}
      </div>

      {acikUclu ? (
        <div className="mt-5 grid gap-5 lg:grid-cols-2">
          <OgrenciIncelemesi sinavId={sinav.data.id} ogrenciler={a.ogrenciler} acikUclu />
          {puanDagilimi}
        </div>
      ) : (
      <div className="mt-5 grid gap-5 lg:grid-cols-[minmax(260px,1fr)_2fr]">
        <section className="kart">
          <h2 className="border-b border-slate-100 px-5 py-3.5 font-bold">Genel Bilgiler</h2>
          <dl className="divide-y divide-slate-100 px-5 py-2 text-sm">
            {[
              ['Ders Adı', g.dersAdi], ['Ders Kodu', g.dersKodu], ['Dönem', `${g.akademikYil} ${g.donem}`],
              ['Sınav Türü', sinavTuruAdi[g.tur]], ['Sınav Formatı', sinavFormatiAdi[g.format]],
              ['Soru Sayısı', String(g.soruSayisi)], ['Maks. Puan', puanYaz(g.maksimumPuan)],
              ...(acikUclu ? [] : [['Kitapçık Sayısı', String(g.kitapcikSayisi)]]),
              ...(g.tarih ? [['Sınav Tarihi', new Date(g.tarih).toLocaleDateString('tr-TR')]] : []),
            ].map(([k, v]) => (
              <div key={k} className="flex justify-between gap-4 py-2.5">
                <dt className="text-selcuk-metin-soluk">{k}</dt>
                <dd className="text-right font-semibold">{v}</dd>
              </div>
            ))}
          </dl>
        </section>
        <OgrenciIncelemesi sinavId={sinav.data.id} ogrenciler={a.ogrenciler} acikUclu={false} />
      </div>
      )}

      {!acikUclu && puanDagilimi}

      {!acikUclu && (
      <section className="kart mt-5 p-5">
        <h2 className="font-bold">Soru Bazlı Başarı Analizi</h2>
        <p className="text-sm text-selcuk-metin-soluk">
          {acikUclu ? 'Her sorudan alınan ortalama puanın soru puanına oranı' : 'Her sorunun genel doğruluk oranları (A kitapçığı sırasıyla)'}
        </p>
        <div className="mt-4 grid grid-cols-3 gap-3 sm:grid-cols-5 lg:grid-cols-8 xl:grid-cols-10">
          {a.sorular.map((s) => (
            <div key={s.soruNo} className="flex flex-col items-center rounded-xl border border-slate-200 bg-selcuk-zemin/50 px-2 py-3"
              title={acikUclu ? `Ortalama ${puanYaz(s.ortalamaPuan)} / ${puanYaz(s.puan)} puan` : `${s.dogru} doğru, ${s.yanlis} yanlış, ${s.bos} boş`}>
              <YuzdeHalkasi yuzde={s.basariOrani} />
              <span className="mt-1 text-sm font-semibold">Soru {s.soruNo}</span>
              {acikUclu && <span className="text-[11px] text-selcuk-metin-soluk">ort. {puanYaz(s.ortalamaPuan)}/{puanYaz(s.puan)}</span>}
            </div>
          ))}
        </div>
        <p className="mt-3 flex flex-wrap gap-4 text-xs text-selcuk-metin-soluk">
          <span><span className="mr-1 inline-block h-2 w-2 rounded-full bg-emerald-600" />%70 ve üzeri</span>
          <span><span className="mr-1 inline-block h-2 w-2 rounded-full bg-amber-600" />%40 – %70</span>
          <span><span className="mr-1 inline-block h-2 w-2 rounded-full bg-red-600" />%40'ın altı</span>
        </p>
      </section>
      )}

      {!acikUclu && (
        <section className="kart mt-5 p-5">
          <h2 className="font-bold">Şık Bazlı Seçenek Dağılımı</h2>
          <p className="text-sm text-selcuk-metin-soluk">Her soru için öğrencilerin şık seçim oranları; yanlışlarda hangi seçeneğe yönelindiği</p>
          <div className="mt-4 grid gap-3 sm:grid-cols-2 lg:grid-cols-4">
            {a.sorular.map((s) => <SikKarti key={s.soruNo} soru={s} />)}
          </div>
        </section>
      )}

      {acikUclu && (
        <section className="kart mt-5 p-5">
          <h2 className="font-bold">Harf Notu Dağılımı</h2>
          <p className="text-sm text-selcuk-metin-soluk">Bu sınavın puanına göre öğrencilerin harf notları (Selçuk Üniversitesi mutlak değerlendirme)</p>
          <div className="mt-4">
            <CubukGrafik baslik="Harf notu dağılımı" veri={a.harfDagilimi.map((h) => ({ etiket: h.harf, deger: h.sayi, renk: HARF_RENGI[h.durum], aciklama: `${h.sayi} öğrenci · ${h.durum}` }))} />
          </div>
        </section>
      )}

      <section className="kart mt-5 p-5">
        <h2 className="font-bold">Öğrenme Çıktıları ve Soru Eşleşmeleri</h2>
        {a.ciktilar.length === 0 ? (
          <p className="mt-2 text-sm text-selcuk-metin-soluk">Bu sınav için henüz öğrenme çıktısı ilişkilendirilmedi.</p>
        ) : (
          <div className="mt-3 overflow-x-auto">
            <table className="w-full min-w-[640px] text-left text-sm">
              <thead className="text-xs text-selcuk-metin-soluk">
                <tr className="border-b border-slate-200">
                  <th className="py-2 pr-3">#</th><th className="py-2 pr-3">Öğrenme Çıktısı</th>
                  <th className="py-2 pr-3">İlişkili Sorular</th><th className="w-48 py-2">Başarı Oranı</th>
                </tr>
              </thead>
              <tbody>
                {a.ciktilar.map((c) => (
                  <tr key={c.siraNo} className="border-b border-slate-100 align-top last:border-0">
                    <td className="py-3 pr-3 font-semibold">{c.siraNo}</td>
                    <td className="py-3 pr-3">{c.aciklama}</td>
                    <td className="py-3 pr-3">
                      <div className="flex flex-wrap gap-1">
                        {c.sorular.map((n) => <span key={n} className="rounded bg-slate-100 px-1.5 py-0.5 text-xs font-semibold">S{n}</span>)}
                      </div>
                    </td>
                    <td className="py-3">{c.basariOrani === null ? '—' : <OranCubugu yuzde={c.basariOrani} />}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </section>

      <DegerlendirmeBolumu sinavId={sinav.data.id} />

      <div className="yazdirma-gizle mt-6 flex justify-center">
        <Link to={`/dersler/${dersId}`} className="btn-ikincil"><ArrowLeft className="h-4 w-4" /> Derse Geri Dön</Link>
      </div>
    </Sayfa>
  )
}

interface Degerlendirme { kayitli: string | null; otomatik: string }

/** "Sınavın Değerlendirmesi": resmi formun son bölümü. Boş bırakılırsa forma otomatik metin yazılır. */
function DegerlendirmeBolumu({ sinavId }: { sinavId: number }) {
  const istemci = useQueryClient()
  const anahtar = ['sinav', sinavId, 'degerlendirme']
  const veri = useQuery({ queryKey: anahtar, queryFn: async () => (await api.get<Degerlendirme>(`/sinavlar/${sinavId}/degerlendirme`)).data })
  const [taslak, setTaslak] = useState<string | null>(null)
  const metin = taslak ?? veri.data?.kayitli ?? ''
  const kaydet = useMutation({
    meta: { bildirim: 'Değerlendirme kaydedildi' },
    mutationFn: () => api.put(`/sinavlar/${sinavId}/degerlendirme`, { metin }),
    onSuccess: () => { setTaslak(null); return istemci.invalidateQueries({ queryKey: anahtar }) },
  })
  const degisti = taslak !== null && taslak !== (veri.data?.kayitli ?? '')

  return (
    <section className="kart mt-5 p-5">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <div>
          <h2 className="font-bold">Sınavın Değerlendirmesi</h2>
          <p className="mt-0.5 text-xs text-selcuk-metin-soluk">
            Sınav Başarısı Analiz ve Değerlendirme Formu'nun son bölümüne yazılır. Boş bırakılırsa istatistiklerden üretilen metin kullanılır.
          </p>
        </div>
        <div className="yazdirma-gizle flex gap-2">
          <button className="btn-ikincil" disabled={!veri.data} onClick={() => setTaslak(veri.data!.otomatik)}>
            <Sparkles className="h-4 w-4" /> Otomatik metni kullan
          </button>
          <button className="btn-mavi" disabled={!degisti || kaydet.isPending} onClick={() => kaydet.mutate()}>
            {kaydet.isPending ? <Loader2 className="h-4 w-4 animate-spin" /> : <Save className="h-4 w-4" />} Kaydet
          </button>
        </div>
      </div>
      {veri.isError && <div className="mt-3"><Uyari tur="hata">{hataMesaji(veri.error)}</Uyari></div>}
      {kaydet.isError && <div className="mt-3"><Uyari tur="hata">{hataMesaji(kaydet.error)}</Uyari></div>}
      {kaydet.isSuccess && !degisti && <div className="mt-3"><Uyari tur="basari">Değerlendirme kaydedildi.</Uyari></div>}
      <label htmlFor="degerlendirme" className="sr-only">Sınavın değerlendirmesi</label>
      <textarea id="degerlendirme" rows={8} maxLength={4000} value={metin} onChange={(e) => setTaslak(e.target.value)}
        placeholder={veri.data?.otomatik ?? 'Yükleniyor…'}
        className="mt-3 w-full rounded-lg border border-slate-300 px-3 py-2 text-sm leading-relaxed focus:border-selcuk-mavi focus:outline-none focus:ring-2 focus:ring-selcuk-mavi/20" />
      <p className="mt-1 text-right text-xs text-selcuk-metin-soluk">{metin.length} / 4000</p>
    </section>
  )
}

const sayi = (v: number | null) => (v === null ? '—' : puanYaz(v))

function Kart({ etiket, deger, alt, Ikon, renk }: { etiket: string; deger: string; alt?: string; Ikon: typeof Users; renk: string }) {
  return (
    <div className="kart flex items-start justify-between gap-2 p-4">
      <div className="min-w-0">
        <div className="text-[11px] font-bold uppercase leading-tight tracking-wide text-selcuk-metin-soluk">{etiket}</div>
        <div className="mt-1 truncate text-xl font-bold text-selcuk-lacivert">{deger}</div>
        {alt && <div className="text-[11px] text-selcuk-metin-soluk">{alt}</div>}
      </div>
      <span className={`flex h-9 w-9 shrink-0 items-center justify-center rounded-lg ${renk}`}><Ikon className="h-4 w-4" /></span>
    </div>
  )
}

function SikKarti({ soru }: { soru: SoruAnalizi }) {
  const siralı = Object.entries(soru.sikOranlari).filter(([, v]) => v > 0)
  return (
    <div className="rounded-xl border border-slate-200 p-3">
      <div className="flex items-center justify-between">
        <span className="font-semibold">Soru {soru.soruNo}</span>
        <span className="text-xs text-selcuk-metin-soluk">Doğru: <b>{soru.dogruCevap}</b></span>
      </div>
      <ul className="mt-2 space-y-1">
        {siralı.map(([sik, oran]) => {
          const dogru = soru.dogruCevap?.includes(sik)
          return (
            <li key={sik} className="flex items-center gap-2 text-xs">
              <span className={`w-16 shrink-0 font-semibold ${dogru ? 'text-emerald-700' : ''}`}>{sik}{dogru ? ' (Doğru)' : ''}</span>
              <span className="h-1.5 flex-1 overflow-hidden rounded-full bg-slate-100">
                <span className={`block h-full rounded-full ${dogru ? 'bg-emerald-500' : sik === 'Boş' ? 'bg-slate-400' : 'bg-selcuk-mavi/60'}`} style={{ width: `${oran}%` }} />
              </span>
              <span className="w-10 text-right">%{puanYaz(oran)}</span>
            </li>
          )
        })}
      </ul>
    </div>
  )
}

function OgrenciIncelemesi({ sinavId, ogrenciler, acikUclu }: {
  sinavId: number; ogrenciler: Analiz['ogrenciler']; acikUclu: boolean
}) {
  const [arama, setArama] = useState('')
  const [secili, setSecili] = useState<number | null>(null)
  const [acik, setAcik] = useState(false)
  const detay = useQuery({
    queryKey: ['analiz-ogrenci', sinavId, secili],
    enabled: secili !== null,
    queryFn: async () => (await api.get<OgrenciAnalizi>(`/sinavlar/${sinavId}/analiz/ogrenci/${secili}`)).data,
  })
  const liste = useMemo(() => {
    const a = arama.trim().toLocaleLowerCase('tr-TR')
    return ogrenciler.filter((o) => !a || `${o.ogrenciNo} ${o.adSoyad}`.toLocaleLowerCase('tr-TR').includes(a)).slice(0, 50)
  }, [arama, ogrenciler])
  const seciliOgr = ogrenciler.find((o) => o.sonucId === secili)

  return (
    <section className="kart">
      <h2 className="border-b border-slate-100 px-5 py-3.5 font-bold">Öğrenci Performans İncelemesi</h2>
      <div className="p-5">
        <div className="relative">
          <label className="flex items-center gap-3 text-sm font-semibold">
            <span className="shrink-0">Öğrenci Seçin:</span>
            <span className="relative flex-1">
              <Search className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-slate-400" />
              <input className="w-full rounded-lg border border-slate-200 bg-selcuk-zemin py-2 pl-9 pr-3 text-sm font-normal focus:border-selcuk-mavi focus:outline-none focus:ring-2 focus:ring-selcuk-mavi/20"
                placeholder="Öğrenci arayın veya seçin…" value={acik ? arama : seciliOgr ? `${seciliOgr.ogrenciNo} — ${seciliOgr.adSoyad}` : arama}
                onFocus={() => { setAcik(true); setArama('') }} onBlur={() => setTimeout(() => setAcik(false), 150)}
                onChange={(e) => setArama(e.target.value)} role="combobox" aria-expanded={acik} />
            </span>
          </label>
          {acik && (
            <ul className="absolute right-0 z-20 mt-1 max-h-64 w-full overflow-y-auto rounded-lg border border-slate-200 bg-white py-1 shadow-lg sm:w-[calc(100%-7rem)]" role="listbox">
              {liste.map((o) => (
                <li key={o.sonucId} role="option" aria-selected={o.sonucId === secili}>
                  <button type="button" onMouseDown={() => { setSecili(o.sonucId); setAcik(false) }}
                    className={`flex w-full items-center gap-2 px-3 py-1.5 text-left text-sm hover:bg-selcuk-zemin ${o.sinavaGirdi ? '' : 'text-slate-400'}`}>
                    <span className="font-mono text-xs">{o.ogrenciNo}</span> {o.adSoyad} {!o.sinavaGirdi && <span className="text-xs">(girmedi)</span>}
                  </button>
                </li>
              ))}
              {liste.length === 0 && <li className="px-3 py-2 text-sm text-selcuk-metin-soluk">Sonuç yok</li>}
            </ul>
          )}
        </div>

        {secili === null ? (
          <div className="mt-4 rounded-xl border border-dashed border-slate-300 px-4 py-10 text-center text-sm text-selcuk-metin-soluk">
            Detaylı soru cevaplarını incelemek için yukarıdan bir öğrenci seçiniz.
          </div>
        ) : detay.isLoading ? (
          <div className="flex justify-center p-8"><Loader2 className="h-5 w-5 animate-spin text-selcuk-mavi" /></div>
        ) : detay.data && (
          <div className="mt-4 rounded-xl border border-emerald-200 bg-emerald-50/50 p-4">
            <div className="flex flex-wrap items-start justify-between gap-2">
              <div>
                <div className="text-[11px] font-bold uppercase text-emerald-700">Seçili Öğrenci</div>
                <div className="font-bold">{detay.data.adSoyad}</div>
                <div className="text-xs text-selcuk-metin-soluk">Öğrenci No: {detay.data.ogrenciNo}{detay.data.kitapcik && !acikUclu && ` · ${detay.data.kitapcik} kitapçığı`}</div>
              </div>
              {detay.data.harfNotu && <span className="rounded-full bg-emerald-600 px-3 py-1 text-xs font-bold text-white">Harf Notu: {detay.data.harfNotu}</span>}
            </div>
            {!detay.data.sinavaGirdi ? (
              <p className="mt-3 text-sm text-selcuk-metin-soluk">Öğrenci sınava girmedi.</p>
            ) : (
              <>
                <div className="mt-3 grid grid-cols-3 gap-2 text-sm">
                  <div><div className="text-xs text-selcuk-metin-soluk">Ham Puan</div><b>{puanYaz(detay.data.hamPuan)}</b></div>
                  <div><div className="text-xs text-selcuk-metin-soluk">Eklemeler</div><b>{detay.data.eklemeler ? `+${puanYaz(detay.data.eklemeler)}` : '—'}</b></div>
                  <div><div className="text-xs text-selcuk-metin-soluk">Düzeltilmiş</div><b>{puanYaz(detay.data.duzeltilmisPuan ?? 0)}</b></div>
                </div>
                <div className="mt-3 grid grid-cols-5 gap-1.5 sm:grid-cols-8 xl:grid-cols-10">
                  {detay.data.sorular.map((s) => {
                    const stil = s.dogruMu ? 'border-emerald-300 bg-emerald-100 text-emerald-800' : s.verilen || (acikUclu && s.puan > 0) ? 'border-red-200 bg-red-50 text-red-700' : 'border-slate-200 bg-white text-slate-400'
                    return (
                      <div key={s.soruNo} className={`rounded-md border px-1 py-1 text-center ${stil}`}
                        title={acikUclu ? `S${s.soruNo}: ${puanYaz(s.puan)} / ${puanYaz(s.maksPuan)}` : `S${s.soruNo}: verilen ${s.verilen ?? 'boş'}, doğru ${s.dogruCevap}`}>
                        <div className="text-[10px] opacity-70">S{s.soruNo}</div>
                        <div className="text-xs font-bold">{acikUclu ? puanYaz(s.puan) : (s.verilen ?? '–')}</div>
                      </div>
                    )
                  })}
                </div>
              </>
            )}
          </div>
        )}
      </div>
    </section>
  )
}
