import { useParams, useSearchParams } from 'react-router-dom'
import { useQuery } from '@tanstack/react-query'
import { Construction } from 'lucide-react'
import Sayfa from '../../components/layout/Sayfa'
import { dersSorgulari, sinavTuruAdi, yoldanSinavTuru } from '../../lib/dersler'

const adimlar = {
  'coktan-secmeli': ['Dosya Yükleme', 'Cevap Anahtarı', 'Veri Eşleştirme', 'Not Düzenleme', 'Öğrenme Çıktıları'],
  'acik-uclu': ['Sınıf Listesi', 'Soru Sayısı', 'Not Yükleme', 'Not Düzenleme', 'Öğrenme Çıktıları'],
}

/** 5 adımlı sınav girişi sihirbazı. Adımların içi Aşama 7-10'da doldurulacak. */
export default function SinavGirisi() {
  const { dersId, tur } = useParams()
  const [arama] = useSearchParams()
  const format = arama.get('format') === 'acik-uclu' ? 'acik-uclu' : 'coktan-secmeli'
  const sinavTuru = yoldanSinavTuru(tur)
  const ders = useQuery(dersSorgulari.detay(Number(dersId)))

  return (
    <Sayfa
      breadcrumb={[
        { etiket: 'Sınav Değerlendirme Sistemi', yol: '/panom' },
        { etiket: ders.data?.ad ?? 'Ders', yol: `/dersler/${dersId}` },
        { etiket: sinavTuru ? sinavTuruAdi[sinavTuru] : 'Sınav' },
        { etiket: format === 'acik-uclu' ? 'Yeni Açık Uçlu Sınav Girişi' : 'Yeni Sınav Girişi' },
      ]}
    >
      <ol className="flex items-start justify-between gap-2 overflow-x-auto pb-2">
        {adimlar[format].map((a, i) => (
          <li key={a} className="flex min-w-[96px] flex-1 flex-col items-center text-center">
            <span className={`flex h-11 w-11 items-center justify-center rounded-full border-2 text-sm font-bold ${i === 0 ? 'border-selcuk-altin bg-selcuk-altin text-selcuk-lacivert' : 'border-slate-300 bg-white text-slate-400'}`}>
              {i + 1}
            </span>
            <span className="mt-2 text-xs font-medium text-selcuk-metin-soluk">{a}</span>
          </li>
        ))}
      </ol>
      <div className="kart mt-6 flex flex-col items-center px-6 py-14 text-center">
        <Construction className="h-10 w-10 text-selcuk-altin-koyu" />
        <p className="mt-3 font-semibold">Sınav giriş sihirbazı</p>
        <p className="mt-1 max-w-md text-sm text-selcuk-metin-soluk">
          Adımların içeriği sonraki aşamalarda (Aşama 7–10) eklenecek.
        </p>
      </div>
    </Sayfa>
  )
}
