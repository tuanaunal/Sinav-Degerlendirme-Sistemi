import { Link } from 'react-router-dom'
import SelcukLogo from '../SelcukLogo'

/** Tüm sayfalarda görünen lacivert kurumsal üst bar. */
export default function UstBar({ sag }: { sag?: React.ReactNode }) {
  return (
    <header className="bg-selcuk-lacivert text-white">
      <div className="mx-auto flex h-[72px] max-w-7xl items-center justify-between gap-4 px-4 sm:px-6">
        <Link to="/" className="flex min-w-0 items-center gap-3">
          <SelcukLogo boyut={46} />
          <div className="min-w-0 leading-tight">
            <div className="truncate text-base font-bold sm:text-lg">Sınav Değerlendirme Sistemi</div>
            <div className="truncate text-xs font-medium text-white/70 sm:text-sm">Selçuk Üniversitesi</div>
          </div>
        </Link>
        {sag}
      </div>
    </header>
  )
}
