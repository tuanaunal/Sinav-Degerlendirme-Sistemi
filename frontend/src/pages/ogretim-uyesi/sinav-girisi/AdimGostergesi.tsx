import { Check } from 'lucide-react'

/** Sihirbazın üstündeki 5 adımlı ilerleme göstergesi: tamamlanan yeşil, aktif altın, kalanlar gri. */
export default function AdimGostergesi({
  adimlar,
  aktif,
  tamamlanan,
  sec,
}: {
  adimlar: string[]
  aktif: number
  tamamlanan: number
  sec: (adim: number) => void
}) {
  return (
    <ol className="flex items-start overflow-x-auto pb-2">
      {adimlar.map((ad, i) => {
        const no = i + 1
        const bitti = no <= tamamlanan && no !== aktif
        const gidilebilir = no <= tamamlanan + 1
        return (
          <li key={ad} className="flex min-w-[92px] flex-1 items-start">
            <button
              type="button"
              disabled={!gidilebilir}
              onClick={() => sec(no)}
              aria-current={no === aktif ? 'step' : undefined}
              className="flex w-full flex-col items-center text-center disabled:cursor-not-allowed"
            >
              <span
                className={`flex h-11 w-11 items-center justify-center rounded-full border-2 text-sm font-bold transition-colors ${
                  no === aktif
                    ? 'border-selcuk-altin bg-selcuk-altin text-selcuk-lacivert'
                    : bitti
                      ? 'border-emerald-500 bg-emerald-500 text-white'
                      : 'border-slate-300 bg-white text-slate-400'
                }`}
              >
                {bitti ? <Check className="h-5 w-5" /> : no}
              </span>
              <span className={`mt-2 text-xs font-medium ${no === aktif ? 'text-selcuk-lacivert' : 'text-selcuk-metin-soluk'}`}>{ad}</span>
            </button>
            {i < adimlar.length - 1 && (
              <span aria-hidden className={`mt-[21px] h-0.5 min-w-[16px] flex-1 ${no <= tamamlanan ? 'bg-emerald-500' : 'bg-slate-300'}`} />
            )}
          </li>
        )
      })}
    </ol>
  )
}
