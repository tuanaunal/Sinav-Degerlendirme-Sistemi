import { Link, NavLink, Outlet } from 'react-router-dom'
import { useQuery } from '@tanstack/react-query'
import { ArrowLeft, FileText, KeyRound, ScanLine, Users } from 'lucide-react'
import Sayfa from '../../components/layout/Sayfa'
import { useAuth } from '../../auth/AuthContext'
import { api } from '../../lib/api'

const menu = [
  { yol: 'sifre-sifirlama', ad: 'Şifre Sıfırlama', Ikon: KeyRound },
  { yol: 'kullanicilar', ad: 'Kullanıcı İşlemleri', Ikon: Users },
  { yol: 'notlandirma', ad: 'Notlandırma Yönetmeliği', Ikon: FileText },
  { yol: 'optik-form', ad: 'Optik Form Ayarları', Ikon: ScanLine },
]

/** Sistem Yönetimi: solda modül menüsü, sağda seçili modülün içeriği. */
export default function YonetimDuzeni() {
  const { kullanici } = useAuth()
  const adminMi = kullanici?.rol === 'Admin'

  const bekleyen = useQuery({
    queryKey: ['sifre-talepleri', 'bekleyen-sayisi'],
    queryFn: async () => (await api.get<number>('/yonetim/sifre-talepleri/bekleyen-sayisi')).data,
  })

  return (
    <Sayfa
      breadcrumb={
        adminMi
          ? [{ etiket: 'Sınav Değerlendirme Sistemi' }, { etiket: 'Yönetim Paneli' }]
          : [{ etiket: 'Sınav Değerlendirme Sistemi', yol: '/panom' }, { etiket: 'Yönetim Paneli' }]
      }
    >
      <div className="flex items-center gap-3">
        {!adminMi && (
          <Link to="/panom" className="btn-ikincil">
            <ArrowLeft className="h-4 w-4" /> Geri Dön
          </Link>
        )}
        <h1 className="text-2xl font-bold text-selcuk-lacivert">Sistem Yönetimi</h1>
      </div>

      <div className="mt-6 grid gap-6 xl:grid-cols-[250px_1fr]">
        <nav aria-label="Yönetim menüsü" className="kart h-fit p-3">
          <ul className="flex gap-1 overflow-x-auto xl:flex-col">
            {menu.map(({ yol, ad, Ikon }) => (
              <li key={yol} className="shrink-0">
                <NavLink
                  to={yol}
                  className={({ isActive }) =>
                    `flex items-center gap-3 rounded-lg px-3 py-2.5 text-sm font-semibold transition-colors ${
                      isActive ? 'bg-selcuk-altin text-selcuk-lacivert' : 'text-selcuk-metin hover:bg-selcuk-zemin'
                    }`
                  }
                >
                  <Ikon className="h-4 w-4" />
                  <span className="whitespace-nowrap">{ad}</span>
                  {yol === 'sifre-sifirlama' && !!bekleyen.data && (
                    <span className="ml-auto rounded-full bg-red-600 px-2 py-0.5 text-xs font-bold text-white">
                      {bekleyen.data}
                    </span>
                  )}
                </NavLink>
              </li>
            ))}
          </ul>
        </nav>

        <section className="min-w-0">
          <Outlet />
        </section>
      </div>
    </Sayfa>
  )
}
