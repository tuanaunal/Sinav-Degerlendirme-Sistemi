import { Navigate, Route, Routes } from 'react-router-dom'
import { anaSayfaYolu, useAuth } from './auth/AuthContext'
import KorumaliRota, { YukleniyorEkrani } from './auth/KorumaliRota'
import Giris from './pages/auth/Giris'
import SifremiUnuttum from './pages/auth/SifremiUnuttum'
import SifreDegistir from './pages/auth/SifreDegistir'
import DersPanosu from './pages/ogretim-uyesi/DersPanosu'
import YonetimPaneli from './pages/yonetim/YonetimPaneli'
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
      </Route>
      <Route element={<KorumaliRota yonetici />}>
        <Route path="/yonetim" element={<YonetimPaneli />} />
      </Route>

      <Route path="*" element={<Navigate to="/" replace />} />
    </Routes>
  )
}
