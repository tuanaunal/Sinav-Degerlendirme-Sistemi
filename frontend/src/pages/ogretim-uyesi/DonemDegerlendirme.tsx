import { useMemo, useState } from 'react'
import { Link, useParams } from 'react-router-dom'
import { useQuery } from '@tanstack/react-query'
import { ArrowDown, ArrowLeft, ArrowUp, ArrowUpDown, CheckCircle2, Download, Loader2, Scale, Search, TrendingUp, Users } from 'lucide-react'
import Sayfa from '../../components/layout/Sayfa'
import { Uyari } from '../../components/form'
import { CubukGrafik, OranCubugu, YuzdeHalkasi } from '../../components/grafikler'
import { api, dosyaIndir, hataMesaji } from '../../lib/api'
import { dersSorgulari } from '../../lib/dersler'
import { puanYaz } from '../../lib/sinav'

interface DonemOgrencisi {
  ogrenciNo: string; adSoyad: string; vize: number | null; final: number | null; butunleme: number | null
  butunlemeListesinde: boolean; etkinSinav: string | null; agirlikli: number; harf: string; durum: string; aciklama: string | null
}
interface SinavIstatistigi {
  sinav: string; format: string; toplam: number; giren: number; ortalama: number | null; medyan: number | null
  standartSapma: number | null; enDusuk: number | null; enYuksek: number | null; basariOrani: number | null
}
interface DonemAnalizi {
  genel: {
    dersKodu: string; dersAdi: string; akademikYil: string; donem: string; ogretimTuru: string; sinif: number; egitmenler: string[]
    vizeAgirligi: number; finalAgirligi: number; finalAltSiniri: number; gecmeSiniri: number
  }
  ozet: {
    toplam: number; agirlikliOrtalama: number | null; medyan: number | null; standartSapma: number | null
    enDusuk: number | null; enYuksek: number | null; basariOrani: number | null
    basarili: number; sartliGecen: number; basarisiz: number; ortalamaKatsayi: number | null
  }
  sinavlar: SinavIstatistigi[]
  harfDagilimi: { harf: string; durum: string; sayi: number }[]
  puanDagilimi: { etiket: string; alt: number; ust: number; sayi: number }[]
  ogrenciler: DonemOgrencisi[]
  ciktilar: { siraNo: number; aciklama: string; sinavlar: { sinav: string; soruSayisi: number; basari: number | null }[]; donemBasarisi: number | null }[]
}

const DURUM_RENGI: Record<string, string> = { 'Başarılı': '#059669', 'Şartlı Geçer': '#D97706', 'Başarısız': '#DC2626' }
const DURUM_ROZETI: Record<string, string> = {
  'Başarılı': 'bg-emerald-50 text-emerald-700', 'Şartlı Geçer': 'bg-amber-50 text-amber-800', 'Başarısız': 'bg-red-50 text-red-700',
}
const SINAV_RENGI: Record<string, string> = { Vize: '#2563EB', Final: '#7C3AED', 'Bütünleme': '#EA580C' }
const SINAV_YOLU: Record<string, string> = { Vize: 'vize', Final: 'final', 'Bütünleme': 'butunleme' }

/** Dönem Analiz Raporu: vize ve final/bütünleme ağırlıklı ortalaması, harf notları ve öğrenci listesi. */
export default function DonemDegerlendirme() {
  const { dersId } = useParams()
  const ders = useQuery(dersSorgulari.detay(Number(dersId)))
  const analiz = useQuery({
    queryKey: ['donem', Number(dersId)],
    queryFn: async () => (await api.get<DonemAnalizi>(`/dersler/${dersId}/donem`)).data,
  })
  const [indirHata, setIndirHata] = useState<string | null>(null)
  // Harf grafiği ve öğrenci listesi aynı filtreyi paylaşır
  const [harf, setHarf] = useState<string | null>(null)

  const breadcrumb = [
    { etiket: 'Sınav Değerlendirme Sistemi', yol: '/panom' },
    { etiket: ders.data?.ad ?? 'Ders', yol: `/dersler/${dersId}` },
    { etiket: 'Dönem Analiz Raporu' },
  ]

  if (analiz.isLoading) {
    return <Sayfa breadcrumb={breadcrumb}><div className="flex justify-center p-10"><Loader2 className="h-6 w-6 animate-spin text-selcuk-mavi" /></div></Sayfa>
  }
  if (analiz.isError || !analiz.data) {
    return (
      <Sayfa breadcrumb={breadcrumb}>
        <Uyari tur="hata">{hataMesaji(analiz.error)}</Uyari>
        <Link to={`/dersler/${dersId}`} className="btn-ikincil mt-4">Derse Geri Dön</Link>
      </Sayfa>
    )
  }

  const a = analiz.data
  const g = a.genel
  const o = a.ozet
  const gecen = o.basarili + o.sartliGecen
  const oran = (n: number) => (o.toplam ? Math.round((n / o.toplam) * 1000) / 10 : 0)
  const indir = () => dosyaIndir(`/dersler/${dersId}/donem/excel`, 'Donem_Analiz_Raporu.xlsx').catch((e) => setIndirHata(hataMesaji(e)))
  const harfler = a.harfDagilimi.filter((h) => h.sayi > 0).sort((x, y) => y.sayi - x.sayi)
  const harfSec = (h: string | null) => setHarf((onceki) => (onceki === h ? null : h))
  const ortAraligi = o.agirlikliOrtalama === null ? null
    : a.puanDagilimi.find((d) => o.agirlikliOrtalama! >= d.alt && (d.ust === 100 ? o.agirlikliOrtalama! <= 100 : o.agirlikliOrtalama! < d.ust))

  return (
    <Sayfa breadcrumb={breadcrumb}>
      <div className="flex flex-wrap items-start justify-between gap-4">
        <div>
          <h1 className="text-2xl font-bold text-selcuk-lacivert">Dönem Analiz Raporu</h1>
          <p className="mt-1 flex items-center gap-2 text-sm text-selcuk-metin-soluk">
            {g.dersKodu} — {g.dersAdi}
            <span className="rounded bg-slate-100 px-1.5 py-0.5 text-xs font-semibold text-selcuk-metin">{g.akademikYil}</span>
          </p>
        </div>
        <div className="yazdirma-gizle flex flex-wrap items-center gap-2">
          {a.sinavlar.map((s) => (
            <Link key={s.sinav} to={`/dersler/${dersId}/sinav/${SINAV_YOLU[s.sinav]}/analiz`} title={`${s.sinav} analiz raporu`}
              className="inline-flex items-center gap-1.5 rounded-full border border-slate-300 bg-white px-3 py-1 text-xs font-semibold hover:bg-slate-50">
              <span className="h-2 w-2 rounded-full" style={{ background: SINAV_RENGI[s.sinav] }} />{s.sinav}
            </Link>
          ))}
          <button className="btn-ikincil py-1.5" onClick={indir}><Download className="h-4 w-4" /> Excel İndir</button>
        </div>
      </div>
      {indirHata && <div className="mt-3"><Uyari tur="hata">{indirHata}</Uyari></div>}

      <div className="mt-5 grid grid-cols-2 gap-3 lg:grid-cols-4">
        <Kart etiket="Toplam Öğrenci" deger={`${o.toplam}`} Ikon={Users} renk="bg-blue-50 text-selcuk-mavi" />
        <Kart etiket="Ağırlıklı Ort." deger={sayi(o.agirlikliOrtalama)} Ikon={TrendingUp} renk="bg-indigo-50 text-indigo-600" />
        <Kart etiket="Başarı Oranı" deger={o.basariOrani === null ? '—' : `%${puanYaz(o.basariOrani)}`} Ikon={CheckCircle2} renk="bg-emerald-50 text-emerald-600" />
        <Kart etiket="Sınav Ağırlığı" deger={`Vize %${g.vizeAgirligi} · Final %${g.finalAgirligi}`} Ikon={Scale} renk="bg-amber-50 text-amber-600" />
      </div>

      <div className="mt-5 grid gap-5 lg:grid-cols-2">
        <section className="kart p-5">
          <h2 className="font-bold">Sınav Ortalamaları Karşılaştırması</h2>
          <p className="text-sm text-selcuk-metin-soluk">Not ortalaması (100 üzerinden)</p>
          <div className="mt-4 space-y-3">
            {[...a.sinavlar.map((s) => ({ ad: `${s.sinav} (${s.giren} öğrenci)`, ort: s.ortalama, renk: SINAV_RENGI[s.sinav] })),
              { ad: `Ağırlıklı Ortalama (${o.toplam} öğrenci)`, ort: o.agirlikliOrtalama, renk: '#059669' }].map((s) => (
              <div key={s.ad}>
                <div className="flex items-baseline justify-between text-sm">
                  <span className="font-semibold">{s.ad}</span>
                  <span className="font-bold tabular-nums">{sayi(s.ort)}</span>
                </div>
                <div className="mt-1 h-2.5 overflow-hidden rounded-full bg-slate-100">
                  <div className="h-full rounded-full" style={{ width: `${Math.min(100, s.ort ?? 0)}%`, background: s.renk }} />
                </div>
              </div>
            ))}
          </div>
          <div className="mt-5 border-t border-slate-100 pt-4">
            <div className="text-xs font-semibold uppercase tracking-wide text-selcuk-metin-soluk">Ağırlıklı Ortalama Detayları</div>
            <IstatistikKutulari degerler={[['Min', o.enDusuk], ['Maks', o.enYuksek], ['Medyan', o.medyan], ['Std Sapma', o.standartSapma]]} />
          </div>
        </section>

        <section className="kart p-5">
          <h2 className="font-bold">Dönem Başarı Özeti</h2>
          <p className="text-sm text-selcuk-metin-soluk">Harf notlarına göre geçen (AA – DD) ve kalan (FF) öğrenciler</p>
          <div className="mt-4 grid grid-cols-3 gap-2 text-center">
            <div className="flex flex-col items-center">
              <YuzdeHalkasi yuzde={oran(gecen)} boyut={72} renk="#059669" />
              <span className="mt-1 text-xs font-semibold text-selcuk-metin-soluk">Başarı Oranı</span>
              <span className="text-xs font-bold text-emerald-700">{gecen} öğrenci</span>
            </div>
            <div className="flex flex-col items-center">
              <YuzdeHalkasi yuzde={oran(o.basarisiz)} boyut={72} renk="#DC2626" />
              <span className="mt-1 text-xs font-semibold text-selcuk-metin-soluk">Başarısızlık</span>
              <span className="text-xs font-bold text-red-700">{o.basarisiz} öğrenci</span>
            </div>
            <div className="flex flex-col items-center">
              <span className="flex h-[72px] w-[72px] items-center justify-center rounded-full border-[6px] border-blue-200 text-lg font-bold text-selcuk-mavi">{o.toplam}</span>
              <span className="mt-1 text-xs font-semibold text-selcuk-metin-soluk">Toplam</span>
              <span className="text-xs font-bold text-selcuk-mavi">öğrenci</span>
            </div>
          </div>
          {o.sartliGecen > 0 && (
            <p className="mt-3 text-center text-xs text-selcuk-metin-soluk">Geçenlerden {o.sartliGecen} öğrenci şartlı geçer (DC – DD).</p>
          )}
          <ul className="mt-4 space-y-2 border-t border-slate-100 pt-3 text-xs">
            {a.sinavlar.map((s) => (
              <li key={s.sinav}>
                <div className="flex items-center justify-between">
                  <span className="font-bold" style={{ color: SINAV_RENGI[s.sinav] }}>{s.sinav}</span>
                  <span className="text-selcuk-metin-soluk">{s.giren} katılımcı</span>
                </div>
                <div className="mt-0.5 flex flex-wrap gap-x-3 tabular-nums text-selcuk-metin-soluk">
                  <span>Ort: <b className="text-selcuk-metin">{sayi(s.ortalama)}</b></span>
                  <span>Min: <b className="text-selcuk-metin">{sayi(s.enDusuk)}</b></span>
                  <span>Maks: <b className="text-selcuk-metin">{sayi(s.enYuksek)}</b></span>
                  <span>Med: <b className="text-selcuk-metin">{sayi(s.medyan)}</b></span>
                  <span>Std: <b className="text-selcuk-metin">{sayi(s.standartSapma)}</b></span>
                </div>
              </li>
            ))}
          </ul>
        </section>
      </div>

      <div className="mt-5 grid gap-5 lg:grid-cols-2">
        <section className="kart p-5">
          <h2 className="font-bold">Harf Notu Dağılımı</h2>
          <p className="text-sm text-selcuk-metin-soluk">Bir barı tıklayarak öğrenci listesini filtreleyin</p>
          <div className="mt-4">
            <CubukGrafik baslik="Harf notu dağılımı" secili={harf} onSec={harfSec}
              veri={harfler.map((h) => ({ etiket: h.harf, deger: h.sayi, renk: DURUM_RENGI[h.durum], aciklama: `${h.sayi} öğrenci · ${h.durum}` }))} />
          </div>
          <HarfFiltresi harfler={harfler} secili={harf} sec={setHarf} sayilarla />
        </section>
        <section className="kart p-5">
          <h2 className="font-bold">Ağırlıklı Puan Dağılımı</h2>
          <p className="text-sm text-selcuk-metin-soluk">Öğrencilerin ağırlıklı ortalama puan aralıklarına göre dağılımı</p>
          <div className="mt-4">
            <CubukGrafik baslik="Ağırlıklı puan dağılımı"
              veri={a.puanDagilimi.map((d) => ({ etiket: d.etiket, deger: d.sayi, renk: '#2563EB', soluk: !!ortAraligi && d.etiket !== ortAraligi.etiket }))} />
          </div>
          <p className="mt-2 flex flex-wrap gap-4 text-xs text-selcuk-metin-soluk">
            <span><span className="mr-1 inline-block h-2.5 w-2.5 rounded-sm bg-blue-600" />Ortalama puanın bulunduğu aralık</span>
            <span><span className="mr-1 inline-block h-2.5 w-2.5 rounded-sm bg-blue-600/35" />Diğer aralıklar</span>
          </p>
        </section>
      </div>

      <OgrenciListesi ogrenciler={a.ogrenciler} harfler={harfler} harf={harf} setHarf={setHarf} />

      {a.ciktilar.length > 0 && (
        <section className="kart mt-5 p-5">
          <h2 className="font-bold">Öğrenme Çıktıları Dönem Başarısı</h2>
          <div className="mt-3 overflow-x-auto">
            <table className="w-full min-w-[720px] text-left text-sm">
              <thead className="text-xs text-selcuk-metin-soluk">
                <tr className="border-b border-slate-200">
                  <th className="py-2 pr-3">#</th><th className="py-2 pr-3">Öğrenme Çıktısı</th>
                  {a.sinavlar.map((s) => <th key={s.sinav} className="py-2 pr-3 text-center">{s.sinav}</th>)}
                  <th className="w-44 py-2">Dönem Başarısı</th>
                </tr>
              </thead>
              <tbody>
                {a.ciktilar.map((c) => (
                  <tr key={c.siraNo} className="border-b border-slate-100 align-top last:border-0">
                    <td className="py-3 pr-3 font-semibold">ÖÇ{c.siraNo}</td>
                    <td className="py-3 pr-3">{c.aciklama}</td>
                    {a.sinavlar.map((s) => {
                      const b = c.sinavlar.find((x) => x.sinav === s.sinav)
                      return <td key={s.sinav} className="py-3 pr-3 text-center tabular-nums">{b?.basari == null ? '—' : `%${puanYaz(b.basari)}`}</td>
                    })}
                    <td className="py-3">{c.donemBasarisi === null ? '—' : <OranCubugu yuzde={c.donemBasarisi} />}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </section>
      )}

      <div className="yazdirma-gizle mt-6 flex justify-center">
        <Link to={`/dersler/${dersId}`} className="btn-ikincil"><ArrowLeft className="h-4 w-4" /> Derse Geri Dön</Link>
      </div>
    </Sayfa>
  )
}

type Harf = { harf: string; durum: string; sayi: number }

function HarfFiltresi({ harfler, secili, sec, sayilarla = false }: { harfler: Harf[]; secili: string | null; sec: (h: string | null) => void; sayilarla?: boolean }) {
  return (
    <div className="yazdirma-gizle mt-3 flex flex-wrap items-center gap-1.5" role="group" aria-label="Harf notuna göre filtrele">
      {[{ harf: null as string | null, durum: '', sayi: 0 }, ...harfler].map((h) => (
        <button key={h.harf ?? 'tumu'} aria-pressed={secili === h.harf} onClick={() => sec(h.harf)}
          className={`inline-flex items-center gap-1.5 rounded-full border px-2.5 py-0.5 text-xs font-semibold transition-colors ${secili === h.harf
            ? 'border-selcuk-lacivert bg-selcuk-lacivert text-white' : 'border-slate-300 bg-white text-selcuk-metin hover:bg-slate-50'}`}>
          {h.harf && sayilarla && <span className="h-2 w-2 rounded-full" style={{ background: DURUM_RENGI[h.durum] }} />}
          {h.harf ?? 'Tümü'}{h.harf && sayilarla && <span className={secili === h.harf ? 'text-white/80' : 'text-selcuk-metin-soluk'}>{h.sayi}</span>}
        </button>
      ))}
    </div>
  )
}

type Siralama = { alan: 'ogrenciNo' | 'adSoyad' | 'vize' | 'final' | 'butunleme' | 'etkin' | 'agirlikli' | 'harf'; artan: boolean }
const etkinPuan = (s: DonemOgrencisi) => (s.etkinSinav === 'Bütünleme' ? s.butunleme : s.etkinSinav === 'Final' ? s.final : null)

/** Tüm öğrenciler: harf notuna göre filtrelenir, numara veya adla aranır, sütunlara göre sıralanır. */
function OgrenciListesi({ ogrenciler, harfler, harf, setHarf }: {
  ogrenciler: DonemOgrencisi[]; harfler: Harf[]; harf: string | null; setHarf: (h: string | null) => void
}) {
  const [arama, setArama] = useState('')
  const [sira, setSira] = useState<Siralama>({ alan: 'agirlikli', artan: false })
  const liste = useMemo(() => {
    const q = arama.trim().toLocaleLowerCase('tr-TR')
    const deger = (s: DonemOgrencisi): string | number => {
      switch (sira.alan) {
        case 'etkin': return etkinPuan(s) ?? -1
        case 'ogrenciNo': case 'adSoyad': case 'harf': return s[sira.alan]
        default: return s[sira.alan] ?? -1
      }
    }
    return ogrenciler
      .filter((s) => (!harf || s.harf === harf) && (!q || s.ogrenciNo.includes(q) || s.adSoyad.toLocaleLowerCase('tr-TR').includes(q)))
      .sort((x, y) => {
        const a = deger(x), b = deger(y)
        const k = typeof a === 'number' && typeof b === 'number' ? a - b : String(a).localeCompare(String(b), 'tr')
        return sira.artan ? k : -k
      })
  }, [ogrenciler, harf, arama, sira])

  const Baslik = ({ alan, children, orta = true }: { alan: Siralama['alan']; children: React.ReactNode; orta?: boolean }) => (
    <th className={`py-2 pr-3 ${orta ? 'text-center' : ''}`} aria-sort={sira.alan === alan ? (sira.artan ? 'ascending' : 'descending') : 'none'}>
      <button className="inline-flex items-center gap-1 hover:text-selcuk-metin" onClick={() => setSira((s) => ({ alan, artan: s.alan === alan ? !s.artan : alan === 'ogrenciNo' || alan === 'adSoyad' || alan === 'harf' }))}>
        {children}
        {sira.alan === alan ? (sira.artan ? <ArrowUp className="h-3 w-3" /> : <ArrowDown className="h-3 w-3" />) : <ArrowUpDown className="h-3 w-3 opacity-40" />}
      </button>
    </th>
  )

  return (
    <section className="kart mt-5">
      <div className="flex flex-wrap items-center gap-2 border-b border-slate-100 px-5 py-3.5">
        <h2 className="font-bold">Tüm Öğrenci Listesi</h2>
        <span className="rounded bg-slate-100 px-1.5 py-0.5 text-xs font-semibold text-selcuk-metin-soluk">{liste.length} / {ogrenciler.length} öğrenci</span>
      </div>
      <div className="flex flex-wrap items-center justify-between gap-3 px-5 pt-3">
        <label className="relative">
          <span className="sr-only">Öğrenci ara</span>
          <Search className="pointer-events-none absolute left-2.5 top-1/2 h-4 w-4 -translate-y-1/2 text-slate-400" />
          <input value={arama} onChange={(e) => setArama(e.target.value)} placeholder="Öğrenci ara (numara veya isim)..."
            className="w-64 rounded-lg border border-slate-300 py-1.5 pl-8 pr-3 text-sm focus:border-selcuk-mavi focus:outline-none focus:ring-2 focus:ring-selcuk-mavi/20" />
        </label>
        <div className="flex items-center gap-2 text-xs text-selcuk-metin-soluk">
          Harf filtresi:
          <HarfFiltresi harfler={[...harfler].sort((x, y) => x.harf.localeCompare(y.harf))} secili={harf} sec={setHarf} />
        </div>
      </div>
      <div className="max-h-[560px] overflow-auto px-5 pb-4 pt-2">
        <table className="w-full min-w-[820px] text-sm">
          <thead className="sticky top-0 z-10 bg-amber-50/95 text-xs text-selcuk-metin-soluk">
            <tr className="border-b border-slate-200 text-left">
              <Baslik alan="ogrenciNo" orta={false}>Öğrenci No</Baslik><Baslik alan="adSoyad" orta={false}>Ad Soyad</Baslik>
              <Baslik alan="vize">Vize</Baslik><Baslik alan="final">Final</Baslik><Baslik alan="butunleme">Büt.</Baslik>
              <Baslik alan="etkin">Etk. Sınav</Baslik><Baslik alan="agirlikli">Ağ. Ort.</Baslik><Baslik alan="harf">Harf</Baslik>
            </tr>
          </thead>
          <tbody>
            {liste.map((s) => (
              <tr key={s.ogrenciNo} className="border-b border-slate-100 last:border-0">
                <td className="py-2 pr-3 font-mono text-xs">{s.ogrenciNo}</td>
                <td className="py-2 pr-3">
                  {s.adSoyad}
                  {s.aciklama && <div className="text-[11px] text-red-600">{s.aciklama}</div>}
                </td>
                <td className="py-2 pr-3 text-center tabular-nums">{puanHucre(s.vize, true)}</td>
                <td className="py-2 pr-3 text-center tabular-nums">{puanHucre(s.final, true)}</td>
                <td className="py-2 pr-3 text-center tabular-nums">{puanHucre(s.butunleme, s.butunlemeListesinde)}</td>
                <td className="py-2 pr-3 text-center tabular-nums" title={s.etkinSinav ?? undefined}>{puanHucre(etkinPuan(s), false)}</td>
                <td className="py-2 pr-3 text-center">
                  <span className={`inline-block min-w-[3rem] rounded px-1.5 py-0.5 text-xs font-bold tabular-nums ${DURUM_ROZETI[s.durum]}`}>{puanYaz(s.agirlikli)}</span>
                </td>
                <td className="py-2 pr-3 text-center">
                  <span className={`inline-block min-w-[2.5rem] rounded-full border px-1.5 py-0.5 text-xs font-bold ${DURUM_ROZETI[s.durum]}`} title={s.durum}>{s.harf}</span>
                </td>
              </tr>
            ))}
            {liste.length === 0 && <tr><td colSpan={8} className="py-6 text-center text-selcuk-metin-soluk">Filtreye uyan öğrenci yok.</td></tr>}
          </tbody>
        </table>
      </div>
    </section>
  )
}

function IstatistikKutulari({ degerler }: { degerler: [string, number | null][] }) {
  return (
    <div className="mt-2 grid grid-cols-2 gap-2 sm:grid-cols-4">
      {degerler.map(([k, v]) => (
        <div key={k} className="rounded-lg border border-slate-200 bg-selcuk-zemin/60 py-2 text-center">
          <div className="text-xs font-semibold text-selcuk-metin-soluk">{k}</div>
          <div className="font-bold tabular-nums">{sayi(v)}</div>
        </div>
      ))}
    </div>
  )
}

function Kart({ etiket, deger, alt, Ikon, renk }: { etiket: string; deger: string; alt?: string; Ikon: typeof Users; renk: string }) {
  return (
    <div className="kart flex items-start justify-between gap-2 p-4">
      <div className="min-w-0">
        <div className="text-[11px] font-bold uppercase leading-tight tracking-wide text-selcuk-metin-soluk">{etiket}</div>
        <div className="mt-1 text-xl font-bold text-selcuk-lacivert">{deger}</div>
        {alt && <div className="text-[11px] text-selcuk-metin-soluk">{alt}</div>}
      </div>
      <span className={`flex h-9 w-9 shrink-0 items-center justify-center rounded-lg ${renk}`}><Ikon className="h-4 w-4" /></span>
    </div>
  )
}

const sayi = (v: number | null) => (v === null ? '—' : puanYaz(v))
/** Girmeyen öğrenci "G", sınav listesinde olmayan öğrenci "—" */
const puanHucre = (p: number | null, listede: boolean) => (p !== null ? puanYaz(p) : listede ? <span className="text-red-600">G</span> : <span className="text-slate-300">—</span>)

