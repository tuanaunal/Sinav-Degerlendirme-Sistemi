import { BookOpen } from 'lucide-react'
import Sayfa from '../../components/layout/Sayfa'
import { useAuth } from '../../auth/AuthContext'

/** Öğretim üyesinin ders panosu. Ders kartları Aşama 4'te eklenecek. */
export default function DersPanosu() {
  const { kullanici } = useAuth()
  return (
    <Sayfa breadcrumb={[{ etiket: 'Sınav Değerlendirme Sistemi' }]}>
      <h1 className="text-2xl font-bold text-selcuk-lacivert">Derslerim</h1>
      <p className="mt-1 text-sm text-selcuk-metin-soluk">Hoş geldiniz, {kullanici?.tamAd}.</p>
      <div className="kart mt-6 flex flex-col items-center px-6 py-14 text-center">
        <span className="flex h-14 w-14 items-center justify-center rounded-full bg-selcuk-mavi-acik text-selcuk-mavi">
          <BookOpen className="h-7 w-7" />
        </span>
        <p className="mt-4 font-semibold">Ders panosu</p>
        <p className="mt-1 max-w-sm text-sm text-selcuk-metin-soluk">
          Akademik yıla göre ders kartları ve "Yeni Ders Ekle" bu sayfada yer alacak.
        </p>
      </div>
    </Sayfa>
  )
}
