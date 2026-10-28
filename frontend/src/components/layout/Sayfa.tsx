import SayfaIskeleti from './SayfaIskeleti'
import KullaniciMenusu from './KullaniciMenusu'
import type { BreadcrumbOge } from './Breadcrumb'

/** Giriş yapılmış sayfalar için: kurumsal iskelet + üst barda kullanıcı menüsü. */
export default function Sayfa({ breadcrumb, children }: { breadcrumb: BreadcrumbOge[]; children: React.ReactNode }) {
  return (
    <SayfaIskeleti breadcrumb={breadcrumb} ustBarSag={<KullaniciMenusu />}>
      {children}
    </SayfaIskeleti>
  )
}
