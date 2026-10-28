import { useMemo, useState } from 'react'
import { Link, useParams } from 'react-router-dom'
import { useQuery } from '@tanstack/react-query'
import { ArrowLeft, Award, CheckCircle2, Download, Loader2, Printer, Scale, Search, TrendingUp, Users } from 'lucide-react'
import Sayfa from '../../components/layout/Sayfa'
import { Uyari } from '../../components/form'
import { CubukGrafik, OranCubugu } from '../../components/grafikler'
import { api, dosyaIndir, hataMesaji } from '../../lib/api'
import { donemAdi } from '../../lib/bicim'
import { dersSorgulari } from '../../lib/dersler'
import { puanYaz } from '../../lib/sinav'

interface DonemOgrencisi {
  ogrenciNo: string; adSoyad: string; vize: number | null; final: number | null; butunleme: number | null
  butunlemeListesinde: boolean; etkinSinav: string | null; agirlikli: number; harf: string; durum: string; aciklama: string | null
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
  sinavlar: { sinav: string; format: string; toplam: number; giren: number; ortalama: number | null; basariOrani: number | null }[]
  harfDagilimi: { harf: string; durum: string; sayi: number }[]
  puanDagilimi: { etiket: string; sayi: number }[]
  ogrenciler: DonemOgrencisi[]
  ciktilar: { siraNo: number; aciklama: string; sinavlar: { sinav: string; soruSayisi: number; basari: number | null }[]; donemBasarisi: number | null }[]
}

const DURUM_RENGI: Record<string, string> = { 'Başarılı': '#059669', 'Şartlı Geçer': '#D97706', 'Başarısız': '#DC2626' }
const DURUM_ROZETI: Record<string, string> = {
  'Başarılı': 'bg-emerald-50 text-emerald-700', 'Şartlı Geçer': 'bg-amber-50 text-amber-800', 'Başarısız': 'bg-red-50 text-red-700',
}

/** Dönem Analiz Raporu: vize ve final/bütünleme ağırlıklı ortalaması, harf notları ve öğrenme çıktısı başarısı. */
export default function DonemDegerlendirme() {
  const { dersId } = useParams()
  const ders = useQuery(dersSorgulari.detay(Number(dersId)))
  const analiz = useQuery({
    queryKey: ['donem', Number(dersId)],
    queryFn: async () => (await api.get<DonemAnalizi>(`/dersler/${dersId}/donem`)).data,
  })
  const [indirHata, setIndirHata] = useState<string | null>(null)

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
  const indir = () => dosyaIndir(`/dersler/${dersId}/donem/excel`, 'Donem_Analiz_Raporu.xlsx').catch((e) => setIndirHata(hataMesaji(e)))

  return (
    <Sayfa breadcrumb={breadcrumb}>
      <div className="flex flex-wrap items-start justify-between gap-4">
        <div>
          <h1 className="text-2xl font-bold text-selcuk-lacivert">Dönem Analiz Raporu</h1>
          <p className="mt-1 text-sm text-selcuk-metin-soluk">{g.dersKodu} — {g.dersAdi} / {g.akademikYil} {donemAdi(g.donem)}</p>
        </div>
        <div className="yazdirma-gizle flex gap-2">
          <button className="btn-ikincil" onClick={() => window.print()}><Printer className="h-4 w-4" /> Yazdır</button>
          <button className="btn-birincil" onClick={indir}><Download className="h-4 w-4" /> Excel İndir</button>
        </div>
      </div>
      {indirHata && <div className="mt-3"><Uyari tur="hata">{indirHata}</Uyari></div>}

      <div className="mt-5 grid grid-cols-2 gap-3 lg:grid-cols-4">
        <Kart etiket="Toplam Öğrenci" deger={`${o.toplam}`} Ikon={Users} renk="bg-blue-50 text-selcuk-mavi" />
        <Kart etiket="Ağırlıklı Ortalama" deger={sayi(o.agirlikliOrtalama)} alt={`Medyan ${sayi(o.medyan)} · Std. sapma ${sayi(o.standartSapma)}`} Ikon={TrendingUp} renk="bg-indigo-50 text-indigo-600" />
        <Kart etiket="Başarı Oranı" deger={o.basariOrani === null ? '—' : `%${puanYaz(o.basariOrani)}`} alt="FF dışındaki harf notları" Ikon={CheckCircle2} renk="bg-emerald-50 text-emerald-600" />
        <Kart etiket="Sınav Ağırlığı (Vize / Final)" deger={`%${g.vizeAgirligi} / %${g.finalAgirligi}`} alt={`Bütünleme finalin yerine geçer · Final alt sınırı ${g.finalAltSiniri}`} Ikon={Scale} renk="bg-amber-50 text-amber-600" />
      </div>

      <div className="mt-5 grid gap-5 lg:grid-cols-2">
        <section className="kart p-5">
          <h2 className="font-bold">Sınav Ortalamaları Karşılaştırması</h2>
          <p className="text-sm text-selcuk-metin-soluk">100 üzerinden; sınava girenlerin ortalaması</p>
          <div className="mt-4 space-y-3">
            {[...a.sinavlar.map((s) => ({ ad: s.sinav, ort: s.ortalama, alt: `${s.giren} / ${s.toplam} öğrenci girdi` })),
              { ad: 'Ağırlıklı Ortalama', ort: o.agirlikliOrtalama, alt: `${o.toplam} öğrenci` }].map((s, i, l) => (
              <div key={s.ad}>
                <div className="flex items-baseline justify-between text-sm">
                  <span className={i === l.length - 1 ? 'font-bold' : 'font-semibold'}>{s.ad}</span>
                  <span className="text-xs text-selcuk-metin-soluk">{s.alt}</span>
                </div>
                <div className="mt-1 flex items-center gap-2">
                  <div className="h-3 flex-1 overflow-hidden rounded-full bg-slate-100">
                    <div className="h-full rounded-full" style={{ width: `${Math.min(100, s.ort ?? 0)}%`, background: i === l.length - 1 ? '#0D2D5E' : '#1C458B' }} />
                  </div>
                  <span className="w-12 text-right text-sm font-bold">{sayi(s.ort)}</span>
                </div>
              </div>
            ))}
          </div>
        </section>

        <section className="kart p-5">
          <h2 className="font-bold">Dönem Başarı Özeti</h2>
          <p className="text-sm text-selcuk-metin-soluk">Harf notlarına göre öğrencilerin durumu</p>
          <div className="mt-5 flex h-4 overflow-hidden rounded-full bg-slate-100" role="img"
            aria-label={`Başarılı ${o.basarili}, şartlı geçen ${o.sartliGecen}, başarısız ${o.basarisiz}`}>
            {[['Başarılı', o.basarili], ['Şartlı Geçer', o.sartliGecen], ['Başarısız', o.basarisiz]].map(([d, n]) => (
              <div key={d} style={{ width: `${o.toplam ? (Number(n) / o.toplam) * 100 : 0}%`, background: DURUM_RENGI[d] }} className="border-r-2 border-white last:border-0" />
            ))}
          </div>
          <dl className="mt-5 grid grid-cols-3 gap-3 text-center">
            {[['Başarılı', o.basarili, 'AA – CC'], ['Şartlı Geçer', o.sartliGecen, 'DC – DD'], ['Başarısız', o.basarisiz, 'FF']].map(([d, n, h]) => (
              <div key={d} className="rounded-lg border border-slate-200 p-3">
                <dt className="flex items-center justify-center gap-1.5 text-xs font-semibold text-selcuk-metin-soluk">
                  <span className="h-2.5 w-2.5 rounded-full" style={{ background: DURUM_RENGI[d as string] }} />{d}
                </dt>
                <dd className="mt-1 text-2xl font-bold text-selcuk-lacivert">%{o.toplam ? puanYaz(Math.round((Number(n) / o.toplam) * 1000) / 10) : 0}</dd>
                <dd className="text-xs text-selcuk-metin-soluk">{n} öğrenci · {h}</dd>
              </div>
            ))}
          </dl>
          {o.ortalamaKatsayi !== null && (
            <p className="mt-4 flex items-center gap-1.5 text-sm text-selcuk-metin-soluk">
              <Award className="h-4 w-4 text-selcuk-altin-koyu" /> Sınıfın ortalama harf katsayısı: <strong className="text-selcuk-metin">{puanYaz(o.ortalamaKatsayi)}</strong>
            </p>
          )}
        </section>
      </div>

      <div className="mt-5 grid gap-5 lg:grid-cols-2">
        <section className="kart p-5">
          <h2 className="font-bold">Harf Notu Dağılımı</h2>
          <div className="mt-4">
            <CubukGrafik baslik="Harf notu dağılımı" veri={a.harfDagilimi.map((h) => ({ etiket: h.harf, deger: h.sayi, renk: DURUM_RENGI[h.durum], aciklama: `${h.sayi} öğrenci · ${h.durum}` }))} />
          </div>
        </section>
        <section className="kart p-5">
          <h2 className="font-bold">Ağırlıklı Puan Dağılımı</h2>
          <div className="mt-4">
            <CubukGrafik baslik="Ağırlıklı puan dağılımı" veri={a.puanDagilimi.map((d) => ({ etiket: d.etiket, deger: d.sayi }))} />
          </div>
        </section>
      </div>

      <section className="kart mt-5 p-5">
        <h2 className="font-bold">Öğrenme Çıktıları Dönem Başarısı</h2>
        {a.ciktilar.length === 0 ? (
          <p className="mt-2 text-sm text-selcuk-metin-soluk">Bu ders için öğrenme çıktısı tanımlanmadı.</p>
        ) : (
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
        )}
      </section>

      <OgrenciListesi ogrenciler={a.ogrenciler} harfler={a.harfDagilimi} />

      <div className="yazdirma-gizle mt-6 flex justify-center">
        <Link to={`/dersler/${dersId}`} className="btn-ikincil"><ArrowLeft className="h-4 w-4" /> Derse Geri Dön</Link>
      </div>
    </Sayfa>
  )
}

/** Tüm öğrenciler; harf notuna göre filtrelenir, numara veya adla aranır. */
function OgrenciListesi({ ogrenciler, harfler }: { ogrenciler: DonemOgrencisi[]; harfler: { harf: string; durum: string; sayi: number }[] }) {
  const [harf, setHarf] = useState<string | null>(null)
  const [arama, setArama] = useState('')
  const liste = useMemo(() => {
    const q = arama.trim().toLocaleLowerCase('tr-TR')
    return ogrenciler.filter((s) => (!harf || s.harf === harf)
      && (!q || s.ogrenciNo.includes(q) || s.adSoyad.toLocaleLowerCase('tr-TR').includes(q)))
  }, [ogrenciler, harf, arama])

  return (
    <section className="kart mt-5">
      <div className="flex flex-wrap items-center justify-between gap-3 border-b border-slate-100 px-5 py-3.5">
        <h2 className="font-bold">Öğrenci Listesi <span className="font-normal text-selcuk-metin-soluk">({liste.length} öğrenci)</span></h2>
        <label className="relative">
          <span className="sr-only">Öğrenci ara</span>
          <Search className="pointer-events-none absolute left-2.5 top-1/2 h-4 w-4 -translate-y-1/2 text-slate-400" />
          <input value={arama} onChange={(e) => setArama(e.target.value)} placeholder="Numara veya ad ara"
            className="w-56 rounded-lg border border-slate-300 py-1.5 pl-8 pr-3 text-sm focus:border-selcuk-mavi focus:outline-none focus:ring-2 focus:ring-selcuk-mavi/20" />
        </label>
      </div>
      <div className="yazdirma-gizle flex flex-wrap gap-1.5 px-5 pt-3" role="group" aria-label="Harf notuna göre filtrele">
        {[{ harf: null as string | null, etiket: 'Tümü', sayi: ogrenciler.length }, ...harfler.map((h) => ({ harf: h.harf, etiket: h.harf, sayi: h.sayi }))].map((f) => (
          <button key={f.etiket} aria-pressed={harf === f.harf} onClick={() => setHarf(f.harf)}
            className={`rounded-full border px-3 py-1 text-xs font-semibold transition-colors ${harf === f.harf
              ? 'border-selcuk-mavi bg-selcuk-mavi text-white' : 'border-slate-300 bg-white text-selcuk-metin hover:bg-slate-50'}`}>
            {f.etiket} <span className={harf === f.harf ? 'text-white/80' : 'text-selcuk-metin-soluk'}>{f.sayi}</span>
          </button>
        ))}
      </div>
      <div className="overflow-x-auto px-5 pb-4 pt-2">
        <table className="w-full min-w-[860px] text-sm">
          <thead className="text-xs text-selcuk-metin-soluk">
            <tr className="border-b border-slate-200 text-left">
              <th className="py-2 pr-3">Öğrenci No</th><th className="py-2 pr-3">Ad Soyad</th>
              <th className="py-2 pr-3 text-center">Vize</th><th className="py-2 pr-3 text-center">Final</th>
              <th className="py-2 pr-3 text-center">Bütünleme</th><th className="py-2 pr-3 text-center">Etkin Sınav</th>
              <th className="py-2 pr-3 text-center">Ağırlıklı Ort.</th><th className="py-2 pr-3 text-center">Harf</th><th className="py-2">Açıklama</th>
            </tr>
          </thead>
          <tbody>
            {liste.map((s) => (
              <tr key={s.ogrenciNo} className="border-b border-slate-100 last:border-0">
                <td className="py-2 pr-3 font-mono text-xs">{s.ogrenciNo}</td>
                <td className="py-2 pr-3">{s.adSoyad}</td>
                <td className="py-2 pr-3 text-center tabular-nums">{puanHucre(s.vize, true)}</td>
                <td className="py-2 pr-3 text-center tabular-nums">{puanHucre(s.final, true)}</td>
                <td className="py-2 pr-3 text-center tabular-nums">{puanHucre(s.butunleme, s.butunlemeListesinde)}</td>
                <td className="py-2 pr-3 text-center text-xs">{s.etkinSinav ?? '—'}</td>
                <td className="py-2 pr-3 text-center font-semibold tabular-nums">{puanYaz(s.agirlikli)}</td>
                <td className="py-2 pr-3 text-center">
                  <span className={`inline-block min-w-[2.5rem] rounded px-1.5 py-0.5 text-xs font-bold ${DURUM_ROZETI[s.durum]}`} title={s.durum}>{s.harf}</span>
                </td>
                <td className="py-2 text-xs text-selcuk-metin-soluk">{s.aciklama}</td>
              </tr>
            ))}
            {liste.length === 0 && <tr><td colSpan={9} className="py-6 text-center text-selcuk-metin-soluk">Filtreye uyan öğrenci yok.</td></tr>}
          </tbody>
        </table>
      </div>
    </section>
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
