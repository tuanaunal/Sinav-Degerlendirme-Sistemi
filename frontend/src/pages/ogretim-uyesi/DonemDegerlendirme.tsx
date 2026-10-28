import { Link, useParams } from 'react-router-dom'
import { useQuery } from '@tanstack/react-query'
import { BarChart3 } from 'lucide-react'
import Sayfa from '../../components/layout/Sayfa'
import { dersSorgulari } from '../../lib/dersler'

/** Dönem Analiz Raporu. Harf notları ve dönem raporu Aşama 13'te eklenecek. */
export default function DonemDegerlendirme() {
  const { dersId } = useParams()
  const ders = useQuery(dersSorgulari.detay(Number(dersId)))
  return (
    <Sayfa
      breadcrumb={[
        { etiket: 'Sınav Değerlendirme Sistemi', yol: '/panom' },
        { etiket: ders.data?.ad ?? 'Ders', yol: `/dersler/${dersId}` },
        { etiket: 'Dönem Analiz Raporu' },
      ]}
    >
      <h1 className="text-2xl font-bold text-selcuk-lacivert">Dönem Analiz Raporu</h1>
      <div className="kart mt-6 flex flex-col items-center px-6 py-14 text-center">
        <BarChart3 className="h-10 w-10 text-purple-600" />
        <p className="mt-3 max-w-md text-sm text-selcuk-metin-soluk">
          Ağırlıklı ortalama, harf notları ve dönem raporu Aşama 13'te eklenecek.
        </p>
        <Link to={`/dersler/${dersId}`} className="btn-ikincil mt-5">Derse Geri Dön</Link>
      </div>
    </Sayfa>
  )
}
