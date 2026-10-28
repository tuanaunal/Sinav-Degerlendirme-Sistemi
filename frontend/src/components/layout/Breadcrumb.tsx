import { Link } from 'react-router-dom'
import { ChevronRight } from 'lucide-react'

export interface BreadcrumbOge {
  etiket: string
  yol?: string
}

/** Üst barın altındaki konum çubuğu: "Sınav Değerlendirme Sistemi > Ders > Vize" */
export default function Breadcrumb({ ogeler }: { ogeler: BreadcrumbOge[] }) {
  return (
    <nav aria-label="Konum" className="border-b border-slate-200 bg-white">
      <ol className="mx-auto flex max-w-7xl flex-wrap items-center gap-1 px-4 py-3 text-sm sm:px-6">
        {ogeler.map((oge, i) => {
          const son = i === ogeler.length - 1
          return (
            <li key={i} className="flex items-center gap-1">
              {oge.yol && !son ? (
                <Link to={oge.yol} className="text-selcuk-metin-soluk hover:text-selcuk-mavi">
                  {oge.etiket}
                </Link>
              ) : (
                <span className={son ? 'font-semibold text-selcuk-lacivert' : 'text-selcuk-metin-soluk'}>
                  {oge.etiket}
                </span>
              )}
              {!son && <ChevronRight className="h-4 w-4 text-slate-400" />}
            </li>
          )
        })}
      </ol>
    </nav>
  )
}
