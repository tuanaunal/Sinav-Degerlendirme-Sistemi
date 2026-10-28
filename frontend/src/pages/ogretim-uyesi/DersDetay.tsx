import { useEffect, useRef, useState } from 'react'
import { Link, useNavigate, useParams } from 'react-router-dom'
import { useQuery } from '@tanstack/react-query'
import {
  BarChart3, ChartColumn, ChevronDown, ClipboardCheck, FilePenLine, GraduationCap, ListChecks, Loader2,
  Lock, PencilLine, Play, Plus, RotateCcw,
} from 'lucide-react'
import Sayfa from '../../components/layout/Sayfa'
import Modal from '../../components/Modal'
import { Uyari } from '../../components/form'
import { hataMesaji } from '../../lib/api'
import { donemAdi, ogretimTuruKisa } from '../../lib/bicim'
import {
  dersSorgulari, sinavFormatiAdi, sinavTuruAdi, sinavTuruYolu,
  type DersDetayi, type SinavFormati, type SinavOzeti, type SinavTuru,
} from '../../lib/dersler'

const kartGorunumu: Record<SinavTuru, { Ikon: typeof ClipboardCheck; zemin: string; renk: string }> = {
  Vize: { Ikon: ClipboardCheck, zemin: 'bg-blue-50', renk: 'text-selcuk-mavi' },
  Final: { Ikon: GraduationCap, zemin: 'bg-emerald-50', renk: 'text-emerald-700' },
  Butunleme: { Ikon: RotateCcw, zemin: 'bg-orange-50', renk: 'text-orange-600' },
}

/** Bir dersin içi: vize, final, bütünleme ve dönem değerlendirme kartları. */
export default function DersDetay() {
  const { dersId } = useParams()
  const id = Number(dersId)
  const ders = useQuery({ ...dersSorgulari.detay(id), enabled: Number.isFinite(id) })
  const [formatSecimi, setFormatSecimi] = useState<SinavTuru | null>(null)

  const breadcrumb = [
    { etiket: 'Sınav Değerlendirme Sistemi', yol: '/panom' },
    { etiket: ders.data?.ad ?? 'Ders' },
  ]

  if (ders.isLoading) {
    return (
      <Sayfa breadcrumb={breadcrumb}>
        <div className="flex justify-center p-10"><Loader2 className="h-6 w-6 animate-spin text-selcuk-mavi" /></div>
      </Sayfa>
    )
  }
  if (ders.isError || !ders.data) {
    return (
      <Sayfa breadcrumb={breadcrumb}>
        <Uyari tur="hata">{hataMesaji(ders.error, 'Ders bulunamadı.')}</Uyari>
        <Link to="/panom" className="btn-ikincil mt-4">Derslerime dön</Link>
      </Sayfa>
    )
  }

  const d = ders.data
  return (
    <Sayfa breadcrumb={breadcrumb}>
      <h1 className="text-2xl font-bold text-selcuk-lacivert">{d.ad}</h1>
      <p className="mt-1 text-sm text-selcuk-metin-soluk">
        <span className="font-mono">{d.kod}</span> · {d.akademikYil} {donemAdi(d.donem)} · {d.sinif}. sınıf ·{' '}
        {ogretimTuruKisa(d.ogretimTuru)}
        {d.egitmenler.length > 0 && <> · {d.egitmenler.join(', ')}</>}
      </p>

      <div className="mt-6 grid gap-4 sm:grid-cols-2 xl:grid-cols-4">
        {(['Vize', 'Final', 'Butunleme'] as SinavTuru[]).map((t) => (
          <SinavKarti key={t} ders={d} tur={t} sinav={d.sinavlar.find((s) => s.tur === t)} yeniGiris={() => setFormatSecimi(t)} />
        ))}
        <DonemKarti ders={d} />
      </div>

      {formatSecimi && <FormatSecimi ders={d} tur={formatSecimi} kapat={() => setFormatSecimi(null)} />}
    </Sayfa>
  )
}

function SinavKarti({ ders, tur, sinav, yeniGiris }: { ders: DersDetayi; tur: SinavTuru; sinav?: SinavOzeti; yeniGiris: () => void }) {
  const navigate = useNavigate()
  const [acik, setAcik] = useState(false)
  const kutu = useRef<HTMLDivElement>(null)
  const { Ikon, zemin, renk } = kartGorunumu[tur]
  const taban = `/dersler/${ders.id}/sinav/${sinavTuruYolu(tur)}`

  useEffect(() => {
    if (!acik) return
    const tikla = (e: MouseEvent) => !kutu.current?.contains(e.target as Node) && setAcik(false)
    const tus = (e: KeyboardEvent) => e.key === 'Escape' && setAcik(false)
    document.addEventListener('mousedown', tikla)
    document.addEventListener('keydown', tus)
    return () => {
      document.removeEventListener('mousedown', tikla)
      document.removeEventListener('keydown', tus)
    }
  }, [acik])

  const tamamlandi = sinav?.durum === 'Tamamlandi'
  const taslak = sinav?.durum === 'Taslak'

  const menu = [
    { ad: 'Yeni Sınav Girişi', Ikon: Plus, aktif: !sinav, git: () => yeniGiris() },
    ...(taslak
      ? [{ ad: `Girişe Devam Et (${sinav.tamamlananAdim}/5)`, Ikon: Play, aktif: true, git: () => navigate(taban) }]
      : [{ ad: 'Sınavı Düzenle', Ikon: PencilLine, aktif: tamamlandi, git: () => navigate(`${taban}/duzenle`) }]),
    { ad: 'Analiz', Ikon: ChartColumn, aktif: tamamlandi, git: () => navigate(`${taban}/analiz`) },
  ]

  return (
    <div ref={kutu} className="relative">
      <button
        type="button"
        onClick={() => setAcik((a) => !a)}
        aria-haspopup="menu"
        aria-expanded={acik}
        className={`kart flex w-full flex-col items-center px-5 pb-5 pt-7 text-center transition-shadow hover:shadow-md ${acik ? 'ring-2 ring-selcuk-mavi/40' : ''}`}
      >
        <span className={`flex h-14 w-14 items-center justify-center rounded-xl ${zemin} ${renk}`}>
          <Ikon className="h-7 w-7" />
        </span>
        <span className="mt-4 flex items-center gap-1 font-semibold">
          {sinavTuruAdi[tur]} <ChevronDown className={`h-4 w-4 transition-transform ${acik ? 'rotate-180' : ''}`} />
        </span>
        <span className="mt-2 text-xs">
          {tamamlandi ? (
            <span className="rounded-full bg-emerald-100 px-2 py-0.5 font-semibold text-emerald-800">
              Girildi · {sinavFormatiAdi[sinav.format]}
            </span>
          ) : taslak ? (
            <span className="rounded-full bg-amber-100 px-2 py-0.5 font-semibold text-amber-800">
              Devam ediyor · {sinav.tamamlananAdim}/5 adım
            </span>
          ) : (
            <span className="text-selcuk-metin-soluk">Henüz girilmedi</span>
          )}
        </span>
      </button>

      {acik && (
        <div role="menu" className="absolute inset-x-0 z-20 mt-2 overflow-hidden rounded-xl border border-slate-200 bg-white py-1 shadow-lg">
          {menu.map((m) => (
            <button
              key={m.ad}
              role="menuitem"
              type="button"
              disabled={!m.aktif}
              onClick={() => {
                setAcik(false)
                m.git()
              }}
              className="flex w-full items-center gap-2 px-4 py-2.5 text-left text-sm hover:bg-selcuk-zemin disabled:cursor-not-allowed disabled:text-slate-400 disabled:hover:bg-transparent"
            >
              <m.Ikon className="h-4 w-4" /> {m.ad}
            </button>
          ))}
        </div>
      )}
    </div>
  )
}

function DonemKarti({ ders }: { ders: DersDetayi }) {
  const icerik = (
    <>
      <span className={`flex h-14 w-14 items-center justify-center rounded-xl ${ders.donemDegerlendirmeAcik ? 'bg-purple-50 text-purple-700' : 'bg-slate-100 text-slate-400'}`}>
        <BarChart3 className="h-7 w-7" />
      </span>
      <span className={`mt-4 font-semibold ${ders.donemDegerlendirmeAcik ? '' : 'text-slate-400'}`}>Dönem Değerlendirme</span>
      <span className="mt-2 flex items-center gap-1 text-xs text-selcuk-metin-soluk">
        {ders.donemDegerlendirmeAcik ? (
          'Harf notları ve dönem raporu'
        ) : (
          <>
            <Lock className="h-3.5 w-3.5" /> Vize ve final (veya bütünleme) girilince açılır
          </>
        )}
      </span>
    </>
  )

  return ders.donemDegerlendirmeAcik ? (
    <Link to={`/dersler/${ders.id}/donem`} className="kart flex flex-col items-center px-5 pb-5 pt-7 text-center transition-shadow hover:shadow-md">
      {icerik}
    </Link>
  ) : (
    <div aria-disabled className="flex cursor-not-allowed flex-col items-center rounded-xl border border-slate-200 bg-slate-50 px-5 pb-5 pt-7 text-center">
      {icerik}
    </div>
  )
}

/** "Vize için sınav giriş türü seçin": çoktan seçmeli veya açık uçlu. */
function FormatSecimi({ ders, tur, kapat }: { ders: DersDetayi; tur: SinavTuru; kapat: () => void }) {
  const navigate = useNavigate()
  const secenekler: { format: SinavFormati; Ikon: typeof ListChecks; aciklama: string }[] = [
    { format: 'CoktanSecmeli', Ikon: ListChecks, aciklama: 'Optik ve cevap anahtarı ile otomatik puanlama yapın.' },
    { format: 'AcikUclu', Ikon: FilePenLine, aciklama: 'Açık uçlu değerlendirme için soru bazlı puanları girin.' },
  ]
  return (
    <Modal
      baslik={`${sinavTuruAdi[tur]} için sınav giriş türü seçin`}
      aciklama="Devam etmeden önce hangi sınav türü ile ilerleyeceğinizi belirleyin."
      kapat={kapat}
    >
      <div className="space-y-3">
        {secenekler.map(({ format, Ikon, aciklama }) => (
          <button
            key={format}
            type="button"
            onClick={() => navigate(`/dersler/${ders.id}/sinav/${sinavTuruYolu(tur)}/yeni?format=${format === 'CoktanSecmeli' ? 'coktan-secmeli' : 'acik-uclu'}`)}
            className="flex w-full items-start gap-4 rounded-xl border border-slate-200 p-4 text-left transition-colors hover:border-selcuk-altin hover:bg-amber-50/50 focus:outline-none focus:ring-2 focus:ring-selcuk-altin"
          >
            <span className="flex h-10 w-10 shrink-0 items-center justify-center rounded-lg bg-selcuk-mavi-acik text-selcuk-mavi">
              <Ikon className="h-5 w-5" />
            </span>
            <span>
              <span className="block font-semibold">{sinavFormatiAdi[format]}</span>
              <span className="block text-sm text-selcuk-metin-soluk">{aciklama}</span>
            </span>
          </button>
        ))}
      </div>
    </Modal>
  )
}
