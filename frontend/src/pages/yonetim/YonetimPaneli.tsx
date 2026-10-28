import { FileText, KeyRound, ScanLine, Users } from 'lucide-react'
import Sayfa from '../../components/layout/Sayfa'
import { anaSayfaYolu, useAuth } from '../../auth/AuthContext'

const moduller = [
  { Ikon: KeyRound, ad: 'Şifre Sıfırlama', aciklama: 'Şifresini unutan öğretim üyelerinin taleplerini onaylayın veya reddedin.' },
  { Ikon: Users, ad: 'Kullanıcı İşlemleri', aciklama: 'Öğretim üyesi ekleyin, yönetici yetkisi verin, derslere eğitmen atayın.' },
  { Ikon: FileText, ad: 'Notlandırma Yönetmeliği', aciklama: 'Vize, final ve bütünleme ağırlıklarını ve harf notu aralıklarını belirleyin.' },
  { Ikon: ScanLine, ad: 'Optik Form Ayarları', aciklama: 'Optik dosyadaki alanların başlangıç ve bitiş sütunlarını tanımlayın.' },
]

/** Sistem yönetimi. Modüllerin içi Aşama 3'te doldurulacak. */
export default function YonetimPaneli() {
  const { kullanici } = useAuth()
  const breadcrumb =
    kullanici?.rol === 'Admin'
      ? [{ etiket: 'Sınav Değerlendirme Sistemi' }, { etiket: 'Yönetim Paneli' }]
      : [{ etiket: 'Sınav Değerlendirme Sistemi', yol: kullanici ? anaSayfaYolu(kullanici) : '/' }, { etiket: 'Yönetim Paneli' }]

  return (
    <Sayfa breadcrumb={breadcrumb}>
      <h1 className="text-2xl font-bold text-selcuk-lacivert">Sistem Yönetimi</h1>
      <p className="mt-1 text-sm text-selcuk-metin-soluk">Teknik ekibe ihtiyaç duymadan sistem ayarlarını yönetin.</p>
      <div className="mt-6 grid gap-4 sm:grid-cols-2">
        {moduller.map(({ Ikon, ad, aciklama }) => (
          <div key={ad} className="kart flex gap-4 p-5">
            <span className="flex h-11 w-11 shrink-0 items-center justify-center rounded-lg bg-selcuk-mavi-acik text-selcuk-mavi">
              <Ikon className="h-5 w-5" />
            </span>
            <div>
              <div className="font-semibold">{ad}</div>
              <p className="mt-1 text-sm text-selcuk-metin-soluk">{aciklama}</p>
            </div>
          </div>
        ))}
      </div>
    </Sayfa>
  )
}
