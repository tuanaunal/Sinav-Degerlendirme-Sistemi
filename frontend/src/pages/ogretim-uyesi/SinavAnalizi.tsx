import { Link, useParams } from 'react-router-dom'
import { useQuery } from '@tanstack/react-query'
import { ChartColumn, CheckCircle2 } from 'lucide-react'
import Sayfa from '../../components/layout/Sayfa'
import { dersSorgulari, sinavTuruAdi, yoldanSinavTuru } from '../../lib/dersler'

/** Sınav Başarı Analiz Raporu. Rapor içeriği Aşama 11'de eklenecek. */
export default function SinavAnalizi() {
  const { dersId, tur } = useParams()
  const sinavTuru = yoldanSinavTuru(tur)
  const ders = useQuery(dersSorgulari.detay(Number(dersId)))
  return (
    <Sayfa
      breadcrumb={[
        { etiket: 'Sınav Değerlendirme Sistemi', yol: '/panom' },
        { etiket: ders.data?.ad ?? 'Ders', yol: `/dersler/${dersId}` },
        { etiket: `Analiz Raporu - ${sinavTuru ? sinavTuruAdi[sinavTuru] : ''}` },
      ]}
    >
      <h1 className="text-2xl font-bold text-selcuk-lacivert">Sınav Başarı Analiz Raporu</h1>
      <div className="kart mt-6 flex flex-col items-center px-6 py-14 text-center">
        <CheckCircle2 className="h-10 w-10 text-emerald-500" />
        <p className="mt-3 font-semibold">Sınav girişi tamamlandı.</p>
        <p className="mt-1 max-w-md text-sm text-selcuk-metin-soluk">
          <ChartColumn className="mr-1 inline h-4 w-4" />Analiz raporu (istatistikler, grafikler, soru ve şık analizi) Aşama 11'de eklenecek.
        </p>
        <Link to={`/dersler/${dersId}`} className="btn-ikincil mt-5">Derse Geri Dön</Link>
      </div>
    </Sayfa>
  )
}
