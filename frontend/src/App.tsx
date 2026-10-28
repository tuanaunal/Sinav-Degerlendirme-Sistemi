import { Navigate, Route, Routes } from 'react-router-dom'
import { anaSayfaYolu, useAuth } from './auth/AuthContext'
import KorumaliRota, { YukleniyorEkrani } from './auth/KorumaliRota'
import Giris from './pages/auth/Giris'
import SifremiUnuttum from './pages/auth/SifremiUnuttum'
import SifreDegistir from './pages/auth/SifreDegistir'
import DersPanosu from './pages/ogretim-uyesi/DersPanosu'
import DersDetay from './pages/ogretim-uyesi/DersDetay'
import SinavGirisi from './pages/ogretim-uyesi/sinav-girisi/SinavGirisi'
import SinavAnalizi from './pages/ogretim-uyesi/SinavAnalizi'
import YonetimDuzeni from './pages/yonetim/YonetimDuzeni'
import SifreSifirlama from './pages/yonetim/SifreSifirlama'
import KullaniciIslemleri from './pages/yonetim/KullaniciIslemleri'
import NotlandirmaYonetmeligi from './pages/yonetim/NotlandirmaYonetmeligi'
import OptikFormAyarlari from './pages/yonetim/OptikFormAyarlari'
import Karsilama from './pages/Karsilama'

/** Kök adres: giriş yapılmışsa rolüne göre ana sayfaya, yapılmamışsa giriş ekranına. */
function KokYonlendirme() {
  const { kullanici, yukleniyor } = useAuth()
  if (yukleniyor) return <YukleniyorEkrani />
  return <Navigate to={kullanici ? anaSayfaYolu(kullanici) : '/giris'} replace />
}

export default function App() {
  return (
    <Routes>
      <Route path="/" element={<KokYonlendirme />} />
      <Route path="/giris" element={<Giris />} />
      <Route path="/sifremi-unuttum" element={<SifremiUnuttum />} />
      <Route path="/sifre-degistir" element={<SifreDegistir />} />
      <Route path="/sistem-durumu" element={<Karsilama />} />

      <Route element={<KorumaliRota roller={['OgretimUyesi']} />}>
        <Route path="/panom" element={<DersPanosu />} />
        <Route path="/dersler/:dersId" element={<DersDetay />} />
        <Route path="/dersler/:dersId/sinav/:tur/yeni" element={<SinavGirisi />} />
        <Route path="/dersler/:dersId/sinav/:tur" element={<SinavGirisi />} />
        <Route path="/dersler/:dersId/sinav/:tur/duzenle" element={<SinavGirisi />} />
        <Route path="/dersler/:dersId/sinav/:tur/analiz" element={<SinavAnalizi />} />
      </Route>
      <Route element={<KorumaliRota yonetici />}>
        <Route path="/yonetim" element={<YonetimDuzeni />}>
          <Route index element={<Navigate to="sifre-sifirlama" replace />} />
          <Route path="sifre-sifirlama" element={<SifreSifirlama />} />
          <Route path="kullanicilar" element={<KullaniciIslemleri />} />
          <Route path="notlandirma" element={<NotlandirmaYonetmeligi />} />
          <Route path="optik-form" element={<OptikFormAyarlari />} />
        </Route>
      </Route>

      <Route path="*" element={<Navigate to="/" replace />} />
    </Routes>
  )
}
