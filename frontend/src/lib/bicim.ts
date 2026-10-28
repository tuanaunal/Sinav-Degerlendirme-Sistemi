/** Ekranda gösterim için küçük biçimlendirme yardımcıları. */

export const donemAdi = (d: string) => (d === 'Guz' ? 'Güz' : 'Bahar')

export const ogretimTuruKisa = (t: string) => (t === 'IkinciOgretim' ? 'İÖ' : 'NÖ')

export const tarihSaat = (s: string) =>
  new Date(s).toLocaleString('tr-TR', { day: '2-digit', month: '2-digit', year: 'numeric', hour: '2-digit', minute: '2-digit' })
