import { Navigate, Outlet, useLocation } from 'react-router-dom'
import { Loader2 } from 'lucide-react'
import { anaSayfaYolu, useAuth, type Rol } from './AuthContext'

function YukleniyorEkrani() {
  return (
    <div className="flex min-h-screen items-center justify-center bg-selcuk-zemin">
      <Loader2 className="h-8 w-8 animate-spin text-selcuk-mavi" aria-label="Yükleniyor" />
    </div>
  )
}

/**
 * Giriş gerektiren sayfaları korur.
 * - Giriş yapılmamışsa giriş ekranına gönderir.
 * - Şifresini değiştirmesi gereken kullanıcıyı şifre değiştirme ekranına gönderir.
 * - `roller` / `yonetici` verilmişse yetkisi olmayanı kendi ana sayfasına gönderir.
 */
export default function KorumaliRota({ roller, yonetici }: { roller?: Rol[]; yonetici?: boolean }) {
  const { kullanici, yukleniyor, yoneticiMi } = useAuth()
  const konum = useLocation()

  if (yukleniyor) return <YukleniyorEkrani />
  if (!kullanici) return <Navigate to="/giris" replace state={{ geri: konum.pathname }} />
  if (kullanici.sifreDegistirmeli) return <Navigate to="/sifre-degistir" replace />
  if (roller && !roller.includes(kullanici.rol)) return <Navigate to={anaSayfaYolu(kullanici)} replace />
  if (yonetici && !yoneticiMi) return <Navigate to={anaSayfaYolu(kullanici)} replace />

  return <Outlet />
}

export { YukleniyorEkrani }
