import { useId, useState } from 'react'
import { AlertCircle, CheckCircle2, Eye, EyeOff, Info } from 'lucide-react'

type InputOzellikleri = React.InputHTMLAttributes<HTMLInputElement>

const inputSinifi =
  'w-full rounded-lg border border-slate-300 bg-white px-3 py-2.5 text-sm text-selcuk-metin placeholder:text-slate-400 ' +
  'focus:border-selcuk-mavi focus:outline-none focus:ring-2 focus:ring-selcuk-mavi/20 disabled:bg-slate-50'

/** Etiketli metin kutusu. */
export function Alan({ etiket, ipucu, ...props }: InputOzellikleri & { etiket: string; ipucu?: string }) {
  const id = useId()
  return (
    <div>
      <label htmlFor={id} className="mb-1.5 block text-sm font-semibold text-selcuk-metin">
        {etiket}
      </label>
      <input id={id} className={inputSinifi} {...props} />
      {ipucu && <p className="mt-1 text-xs text-selcuk-metin-soluk">{ipucu}</p>}
    </div>
  )
}

/** Göster/gizle butonlu şifre kutusu. */
export function SifreAlani({ etiket, ipucu, ...props }: InputOzellikleri & { etiket: string; ipucu?: string }) {
  const id = useId()
  const [goster, setGoster] = useState(false)
  return (
    <div>
      <label htmlFor={id} className="mb-1.5 block text-sm font-semibold text-selcuk-metin">
        {etiket}
      </label>
      <div className="relative">
        <input id={id} type={goster ? 'text' : 'password'} className={`${inputSinifi} pr-11`} {...props} />
        <button
          type="button"
          onClick={() => setGoster((g) => !g)}
          className="absolute inset-y-0 right-0 flex w-11 items-center justify-center text-slate-500 hover:text-selcuk-mavi"
          aria-label={goster ? 'Şifreyi gizle' : 'Şifreyi göster'}
        >
          {goster ? <EyeOff className="h-4 w-4" /> : <Eye className="h-4 w-4" />}
        </button>
      </div>
      {ipucu && <p className="mt-1 text-xs text-selcuk-metin-soluk">{ipucu}</p>}
    </div>
  )
}

const uyariStilleri = {
  hata: { sinif: 'border-red-200 bg-red-50 text-red-700', Ikon: AlertCircle },
  basari: { sinif: 'border-emerald-200 bg-emerald-50 text-emerald-700', Ikon: CheckCircle2 },
  bilgi: { sinif: 'border-blue-200 bg-blue-50 text-selcuk-lacivert', Ikon: Info },
  uyari: { sinif: 'border-amber-200 bg-amber-50 text-amber-800', Ikon: AlertCircle },
}

/** Form üstünde gösterilen hata / bilgi kutusu. */
export function Uyari({ tur, children }: { tur: keyof typeof uyariStilleri; children: React.ReactNode }) {
  const { sinif, Ikon } = uyariStilleri[tur]
  return (
    <div role={tur === 'hata' ? 'alert' : 'status'} className={`flex gap-2 rounded-lg border px-3 py-2.5 text-sm ${sinif}`}>
      <Ikon className="mt-0.5 h-4 w-4 shrink-0" />
      <div>{children}</div>
    </div>
  )
}
