import { useEffect } from 'react'
import { X } from 'lucide-react'

/** Ekranın ortasında açılan pencere. Esc veya arka plana tıklama ile kapanır. */
export default function Modal({
  baslik,
  aciklama,
  kapat,
  children,
  genis,
}: {
  baslik: string
  aciklama?: string
  kapat: () => void
  children: React.ReactNode
  genis?: boolean
}) {
  useEffect(() => {
    const tus = (e: KeyboardEvent) => e.key === 'Escape' && kapat()
    document.addEventListener('keydown', tus)
    const eski = document.body.style.overflow
    document.body.style.overflow = 'hidden'
    return () => {
      document.removeEventListener('keydown', tus)
      document.body.style.overflow = eski
    }
  }, [kapat])

  return (
    <div
      className="fixed inset-0 z-40 flex items-end justify-center bg-selcuk-lacivert/40 p-0 sm:items-center sm:p-4"
      onMouseDown={(e) => e.target === e.currentTarget && kapat()}
    >
      <div
        role="dialog"
        aria-modal="true"
        aria-label={baslik}
        className={`max-h-[92vh] w-full overflow-y-auto rounded-t-2xl bg-white shadow-xl sm:rounded-2xl ${genis ? 'sm:max-w-2xl' : 'sm:max-w-lg'}`}
      >
        <div className="flex items-start justify-between gap-4 border-b border-slate-100 px-5 py-4">
          <div>
            <h2 className="text-lg font-bold text-selcuk-lacivert">{baslik}</h2>
            {aciklama && <p className="mt-0.5 text-sm text-selcuk-metin-soluk">{aciklama}</p>}
          </div>
          <button type="button" onClick={kapat} className="rounded-lg p-1.5 text-slate-500 hover:bg-slate-100" aria-label="Kapat">
            <X className="h-5 w-5" />
          </button>
        </div>
        <div className="px-5 py-5">{children}</div>
      </div>
    </div>
  )
}
