import { api } from './api'
import type { SinavFormati, SinavTuru } from './dersler'

export interface SinavBilgisi {
  id: number
  dersId: number
  tur: SinavTuru
  format: SinavFormati
  durum: 'Taslak' | 'Tamamlandi'
  tamamlananAdim: number
  soruSayisi: number
  maksimumPuan: number
  kitapcikSayisi: number
  puanlariDinamikDagit: boolean
  birdenFazlaDogruKabul: boolean
  optikDosyaAdi: string | null
  sinifListesiDosyaAdi: string | null
  sinifMevcudu: number
  optikKagitSayisi: number
  uyarilar: string[]
}

export interface OptikOnizleme {
  dosyaAdi: string
  kodlama: string
  ogrenciSayisi: number
  cevapAnahtariSayisi: number
  tahminiSoruSayisi: number
  kitapcikDagilimi: Record<string, number>
  uyarilar: string[]
}

export interface SinifListesiOnizleme {
  dosyaAdi: string
  ogrenciSayisi: number
  uyarilar: string[]
}

export interface OptikSecenegi {
  id: number
  etiket: string
  kitapcik: string | null
  cevaplar: string
  cevapAnahtariMi: boolean
}

export interface CevapAnahtari {
  soruSayisi: number
  maksimumPuan: number
  kitapcikSayisi: number
  puanlariDinamikDagit: boolean
  birdenFazlaDogruKabul: boolean
  sorular: { soruNo: number; puan: number }[]
  kitapciklar: { kitapcik: string; cevaplar: string[] }[]
  optikSecenekleri: OptikSecenegi[]
}

export const sinavAnahtari = (dersId: number, tur: string) => ['sinav', dersId, tur.toLowerCase()] as const

export const sinavSorgusu = (dersId: number, tur: string) => ({
  queryKey: sinavAnahtari(dersId, tur),
  queryFn: async (): Promise<SinavBilgisi | null> => {
    try {
      return (await api.get<SinavBilgisi>(`/dersler/${dersId}/sinavlar/${tur.toLowerCase()}`)).data
    } catch (e) {
      // Henüz giriş yapılmamış sınav
      if ((e as { response?: { status?: number } }).response?.status === 404) return null
      throw e
    }
  },
})

/** Backend'deki PuanDagitimi.EsitDagit ile aynı: kalan kuruşlar son sorulara eklenir. */
export function esitDagit(maksimum: number, soruSayisi: number): number[] {
  if (soruSayisi <= 0) return []
  const taban = Math.floor((maksimum / soruSayisi) * 100) / 100
  const puanlar = Array<number>(soruSayisi).fill(taban)
  const kalan = Math.round((maksimum - taban * soruSayisi) * 100)
  for (let i = 0; i < kalan; i++) puanlar[soruSayisi - 1 - i] = Math.round((puanlar[soruSayisi - 1 - i] + 0.01) * 100) / 100
  return puanlar
}

export const puanYaz = (p: number) => p.toLocaleString('tr-TR', { maximumFractionDigits: 2 })
