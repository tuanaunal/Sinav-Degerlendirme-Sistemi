import { useEffect, useRef, useState } from 'react'
import { Link } from 'react-router-dom'
import { BookOpen, ChevronDown, KeyRound, LogOut, Settings } from 'lucide-react'
import { useAuth } from '../../auth/AuthContext'

function basHarfler(ad: string, soyad: string) {
  return `${ad.charAt(0)}${soyad.charAt(0)}`.toLocaleUpperCase('tr-TR')
}

/** Üst barın sağındaki kullanıcı adı ve açılır menü. */
export default function KullaniciMenusu() {
  const { kullanici, yoneticiMi, cikisYap } = useAuth()
  const [acik, setAcik] = useState(false)
  const kutu = useRef<HTMLDivElement>(null)

  // Menünün dışına tıklanınca veya Esc'ye basılınca kapat
  useEffect(() => {
    if (!acik) return
    const tikla = (e: MouseEvent) => !kutu.current?.contains(e.target as Node) && setAcik(false)
    const tus = (e: KeyboardEvent) => e.key === 'Escape' && setAcik(false)
    document.addEventListener('mousedown', tikla)
    document.addEventListener('keydown', tus)
    return () => {
      document.removeEventListener('mousedown', tikla)
      document.removeEventListener('keydown', tus)
    }
  }, [acik])

  if (!kullanici) return null

  const adminMi = kullanici.rol === 'Admin'
  const rolEtiketi = adminMi ? 'Sistem Yöneticisi' : yoneticiMi ? 'Öğretim Üyesi · Yönetici' : 'Öğretim Üyesi'
  const menuOgesi = 'flex w-full items-center gap-2 px-4 py-2.5 text-left text-sm hover:bg-selcuk-zemin'

  return (
    <div ref={kutu} className="relative">
      <button
        type="button"
        onClick={() => setAcik((a) => !a)}
        className="flex items-center gap-2 rounded-lg px-2 py-1.5 hover:bg-white/10"
        aria-haspopup="menu"
        aria-expanded={acik}
      >
        <span className="flex h-9 w-9 shrink-0 items-center justify-center rounded-full bg-selcuk-altin text-sm font-bold text-selcuk-lacivert">
          {adminMi ? <Settings className="h-4 w-4" /> : basHarfler(kullanici.ad, kullanici.soyad)}
        </span>
        <span className="hidden text-left leading-tight md:block">
          <span className="block text-sm font-semibold">{adminMi ? 'Admin Yönetimi' : kullanici.tamAd}</span>
          <span className="block text-xs text-white/70">{rolEtiketi}</span>
        </span>
        <ChevronDown className="h-4 w-4 text-white/80" />
      </button>

      {acik && (
        <div
          role="menu"
          className="absolute right-0 z-30 mt-2 w-64 overflow-hidden rounded-xl border border-slate-200 bg-white py-1 text-selcuk-metin shadow-lg"
        >
          <div className="border-b border-slate-100 px-4 py-3">
            <div className="truncate text-sm font-semibold">{kullanici.tamAd}</div>
            <div className="truncate text-xs text-selcuk-metin-soluk">{kullanici.eposta}</div>
          </div>
          {!adminMi && (
            <Link role="menuitem" to="/panom" className={menuOgesi} onClick={() => setAcik(false)}>
              <BookOpen className="h-4 w-4 text-selcuk-mavi" /> Derslerim
            </Link>
          )}
          {yoneticiMi && (
            <Link role="menuitem" to="/yonetim" className={menuOgesi} onClick={() => setAcik(false)}>
              <Settings className="h-4 w-4 text-selcuk-mavi" /> Yönetim Paneli
            </Link>
          )}
          <Link role="menuitem" to="/sifre-degistir" className={menuOgesi} onClick={() => setAcik(false)}>
            <KeyRound className="h-4 w-4 text-selcuk-mavi" /> Şifre Değiştir
          </Link>
          <button role="menuitem" type="button" onClick={cikisYap} className={`${menuOgesi} border-t border-slate-100 text-red-600`}>
            <LogOut className="h-4 w-4" /> Çıkış Yap
          </button>
        </div>
      )}
    </div>
  )
}
