import { useState } from 'react'

export interface Cubuk {
  etiket: string
  deger: number
  /** Üzerine gelince gösterilen açıklama */
  aciklama?: string
  /** Varsayılan kurumsal mavi yerine (ör. harf notunda durum rengi) */
  renk?: string
}

/**
 * Tek serili dikey çubuk grafik (puan dağılımı, harf dağılımı). Izgara çizgileri soluk, çubuk uçları yuvarlak;
 * her çubuğun üstünde değeri yazar, üzerine gelince açıklama balonu gösterir.
 */
export function CubukGrafik({ veri, yukseklik = 220, birim = 'öğrenci', baslik }: {
  veri: Cubuk[]
  yukseklik?: number
  birim?: string
  baslik: string
}) {
  const [secili, setSecili] = useState<number | null>(null)
  const maks = Math.max(1, ...veri.map((v) => v.deger))
  const adim = maks <= 5 ? 1 : Math.ceil(maks / 4 / 5) * 5
  const ust = Math.ceil(maks / adim) * adim
  const izgara = Array.from({ length: ust / adim + 1 }, (_, i) => i * adim)

  return (
    <figure aria-label={baslik} className="pt-3">
      <div className="flex gap-2">
        <div className="relative w-7 shrink-0 text-right text-[11px] text-selcuk-metin-soluk" style={{ height: yukseklik }}>
          {izgara.map((g) => (
            <span key={g} className="absolute right-0 -translate-y-1/2" style={{ bottom: `${(g / ust) * 100}%` }}>{g}</span>
          ))}
        </div>
        <div className="relative flex-1" style={{ height: yukseklik }} onMouseLeave={() => setSecili(null)}>
          {izgara.map((g) => (
            <div key={g} className="absolute inset-x-0 border-t border-dashed border-slate-200" style={{ bottom: `${(g / ust) * 100}%` }} />
          ))}
          <div className="absolute inset-0 flex items-end gap-[2px] px-1">
            {veri.map((v, i) => (
              <div key={v.etiket} className="relative flex h-full flex-1 flex-col justify-end" onMouseEnter={() => setSecili(i)}
                onFocus={() => setSecili(i)} tabIndex={0} aria-label={`${v.etiket}: ${v.deger} ${birim}`}>
                {v.deger > 0 && (
                  <span className="mb-0.5 text-center text-[11px] font-semibold text-selcuk-metin">{v.deger}</span>
                )}
                <div className={`mx-auto w-full max-w-[56px] rounded-t transition-opacity ${secili !== null && secili !== i ? 'opacity-50' : ''}`}
                  style={{ height: `${(v.deger / ust) * 100}%`, minHeight: v.deger > 0 ? 3 : 0, background: v.renk ?? '#1C458B' }} />
                {secili === i && (
                  <div role="tooltip" className="pointer-events-none absolute bottom-full left-1/2 z-10 mb-1 -translate-x-1/2 whitespace-nowrap rounded-lg bg-selcuk-lacivert px-2.5 py-1.5 text-xs text-white shadow-lg">
                    <div className="font-semibold">{v.etiket}</div>
                    <div>{v.aciklama ?? `${v.deger} ${birim}`}</div>
                  </div>
                )}
              </div>
            ))}
          </div>
        </div>
      </div>
      <div className="ml-9 mt-1 flex gap-[2px] px-1">
        {veri.map((v) => <span key={v.etiket} className="flex-1 text-center text-[11px] text-selcuk-metin-soluk">{v.etiket}</span>)}
      </div>
    </figure>
  )
}

/** Yüzde halkası: %70 ve üzeri yeşil, %40-70 sarı, altı kırmızı. Değer her zaman ortada yazılıdır. */
export function YuzdeHalkasi({ yuzde, boyut = 64 }: { yuzde: number; boyut?: number }) {
  const r = (boyut - 8) / 2
  const cevre = 2 * Math.PI * r
  const renk = yuzde >= 70 ? '#059669' : yuzde >= 40 ? '#D97706' : '#DC2626'
  return (
    <svg width={boyut} height={boyut} viewBox={`0 0 ${boyut} ${boyut}`} role="img" aria-label={`%${yuzde.toLocaleString('tr-TR', { maximumFractionDigits: 1 })}`}>
      <circle cx={boyut / 2} cy={boyut / 2} r={r} fill="none" stroke="#E2E8F0" strokeWidth={6} />
      <circle cx={boyut / 2} cy={boyut / 2} r={r} fill="none" stroke={renk} strokeWidth={6} strokeLinecap="round"
        strokeDasharray={`${(Math.min(100, Math.max(0, yuzde)) / 100) * cevre} ${cevre}`} transform={`rotate(-90 ${boyut / 2} ${boyut / 2})`} />
      <text x="50%" y="50%" dominantBaseline="central" textAnchor="middle" className="fill-selcuk-metin text-[13px] font-bold">
        %{yuzde.toLocaleString('tr-TR', { maximumFractionDigits: 0 })}
      </text>
    </svg>
  )
}

/** Yatay oran çubuğu (öğrenme çıktısı başarısı). */
export function OranCubugu({ yuzde }: { yuzde: number }) {
  const renk = yuzde >= 70 ? 'bg-emerald-500' : yuzde >= 40 ? 'bg-amber-500' : 'bg-red-500'
  return (
    <div className="flex items-center gap-2">
      <div className="h-2 flex-1 overflow-hidden rounded-full bg-slate-200">
        <div className={`h-full rounded-full ${renk}`} style={{ width: `${Math.min(100, yuzde)}%` }} />
      </div>
      <span className="w-12 text-right text-sm font-semibold">%{yuzde.toLocaleString('tr-TR', { maximumFractionDigits: 1 })}</span>
    </div>
  )
}
