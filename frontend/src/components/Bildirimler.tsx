import { useEffect, useState } from 'react'
import { CheckCircle2 } from 'lucide-react'
import { bildirimleriDinle, type Bildirim } from '../lib/bildirim'

/** Uygulamada bir kez yerleştirilir; bildir() ile gönderilen mesajları sağ üstte 3 saniye gösterir. */
export default function Bildirimler() {
  const [liste, setListe] = useState<Bildirim[]>([])
  useEffect(() => bildirimleriDinle((b) => {
    setListe((l) => [...l.slice(-2), b])
    setTimeout(() => setListe((l) => l.filter((x) => x.id !== b.id)), 3000)
  }), [])

  return (
    <div className="yazdirma-gizle pointer-events-none fixed right-4 top-20 z-[60] flex flex-col gap-2" role="status" aria-live="polite">
      {liste.map((b) => (
        <div key={b.id} className="flex items-center gap-2 rounded-lg border border-slate-200 bg-white px-4 py-2.5 text-sm font-medium text-selcuk-metin shadow-lg">
          <CheckCircle2 className="h-4 w-4 shrink-0 text-emerald-600" /> {b.mesaj}
        </div>
      ))}
    </div>
  )
}
