export interface Bildirim { id: number; mesaj: string }

let sayac = 0
const dinleyiciler = new Set<(b: Bildirim) => void>()

/** Sağ üstte birkaç saniye görünen başarı bildirimi ("Sınav kaydedildi" gibi). */
export function bildir(mesaj: string) {
  const b = { id: ++sayac, mesaj }
  dinleyiciler.forEach((d) => d(b))
}

/** Bildirimleri dinler; dinlemeyi bırakan fonksiyonu döner. */
export function bildirimleriDinle(d: (b: Bildirim) => void) {
  dinleyiciler.add(d)
  return () => { dinleyiciler.delete(d) }
}
