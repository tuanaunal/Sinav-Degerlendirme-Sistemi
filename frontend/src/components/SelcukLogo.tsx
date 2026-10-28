import { useState } from 'react'

/** Selçuk Üniversitesi resmi amblemi. Dosya bulunamazsa altın daire içinde "SÜ" gösterilir. */
export default function SelcukLogo({ boyut = 44 }: { boyut?: number }) {
  const [hata, setHata] = useState(false)

  if (hata) {
    return (
      <div
        className="flex shrink-0 items-center justify-center rounded-full bg-selcuk-altin font-bold text-selcuk-lacivert"
        style={{ width: boyut, height: boyut, fontSize: boyut * 0.36 }}
        aria-label="Selçuk Üniversitesi"
      >
        SÜ
      </div>
    )
  }

  return (
    <img
      src="/selcuk-logo.svg"
      alt="Selçuk Üniversitesi amblemi"
      width={boyut}
      height={boyut}
      className="shrink-0 rounded-full bg-white"
      onError={() => setHata(true)}
    />
  )
}
