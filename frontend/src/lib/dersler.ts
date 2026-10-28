import { api } from './api'

export type SinavTuru = 'Vize' | 'Final' | 'Butunleme'
export type SinavFormati = 'CoktanSecmeli' | 'AcikUclu'
export type SinavDurumu = 'Taslak' | 'Tamamlandi'

export interface SinavOzeti {
  id: number
  tur: SinavTuru
  format: SinavFormati
  durum: SinavDurumu
  tamamlananAdim: number
  guncellemeTarihi: string
}

export interface DersKarti {
  id: number
  kod: string
  ad: string
  akademikYil: string
  donem: 'Guz' | 'Bahar'
  ogretimTuru: 'NormalOgretim' | 'IkinciOgretim'
  sinif: number
  egitmenler: string[]
  sinavlar: SinavOzeti[]
}

export interface DersDetayi extends DersKarti {
  donemDegerlendirmeAcik: boolean
}

export const sinavTuruAdi: Record<SinavTuru, string> = { Vize: 'Vize', Final: 'Final', Butunleme: 'Bütünleme' }
export const sinavFormatiAdi: Record<SinavFormati, string> = { CoktanSecmeli: 'Çoktan Seçmeli', AcikUclu: 'Açık Uçlu' }

/** Adres çubuğunda kullanılan küçük harfli sınav türü: /dersler/5/sinav/vize */
export const sinavTuruYolu = (t: SinavTuru) => t.toLowerCase() as 'vize' | 'final' | 'butunleme'
export const yoldanSinavTuru = (s?: string): SinavTuru | null =>
  s === 'vize' ? 'Vize' : s === 'final' ? 'Final' : s === 'butunleme' ? 'Butunleme' : null

export const dersSorgulari = {
  yillar: () => ({
    queryKey: ['dersler', 'akademik-yillar'],
    queryFn: async () => (await api.get<{ guncel: string; yillar: string[] }>('/dersler/akademik-yillar')).data,
  }),
  liste: (akademikYil: string) => ({
    queryKey: ['dersler', 'liste', akademikYil],
    queryFn: async () => (await api.get<DersKarti[]>('/dersler', { params: { akademikYil } })).data,
  }),
  detay: (id: number) => ({
    queryKey: ['dersler', 'detay', id],
    queryFn: async () => (await api.get<DersDetayi>(`/dersler/${id}`)).data,
  }),
}
