import UstBar from './UstBar'
import Breadcrumb, { type BreadcrumbOge } from './Breadcrumb'

/** Üst bar + konum çubuğu + içerik + alt bilgi. Uygulamadaki tüm sayfalar bunu kullanır. */
export default function SayfaIskeleti({
  breadcrumb,
  ustBarSag,
  children,
}: {
  breadcrumb?: BreadcrumbOge[]
  ustBarSag?: React.ReactNode
  children: React.ReactNode
}) {
  return (
    <div className="flex min-h-screen flex-col">
      <UstBar sag={ustBarSag} />
      {breadcrumb && <Breadcrumb ogeler={breadcrumb} />}
      <main className="mx-auto w-full max-w-7xl flex-1 px-4 py-6 sm:px-6 sm:py-8">{children}</main>
      <footer className="border-t border-slate-200 bg-white">
        <div className="mx-auto max-w-7xl px-4 py-4 text-center text-xs text-selcuk-metin-soluk sm:px-6">
          © {new Date().getFullYear()} Selçuk Üniversitesi · Teknoloji Fakültesi · Bilgisayar Mühendisliği
        </div>
      </footer>
    </div>
  )
}
