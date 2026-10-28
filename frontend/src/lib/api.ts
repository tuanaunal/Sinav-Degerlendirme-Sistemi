import axios, { AxiosError } from 'axios'

const TOKEN_ANAHTARI = 'sds.token'

/** Token tarayıcıda saklanır; gizli pencere gibi depolamanın kapalı olduğu durumlarda sessizce atlanır. */
export const tokenDeposu = {
  oku(): string | null {
    try {
      return localStorage.getItem(TOKEN_ANAHTARI)
    } catch {
      return null
    }
  },
  yaz(token: string | null) {
    try {
      if (token) localStorage.setItem(TOKEN_ANAHTARI, token)
      else localStorage.removeItem(TOKEN_ANAHTARI)
    } catch {
      /* depolama kullanılamıyor */
    }
  },
}

/** Tüm backend çağrıları bu istemci üzerinden yapılır (Vite proxy ile /api → :5080). */
export const api = axios.create({
  baseURL: '/api',
})

api.interceptors.request.use((config) => {
  const token = tokenDeposu.oku()
  if (token) config.headers.Authorization = `Bearer ${token}`
  return config
})

/** Oturum süresi dolunca (401) çağrılır; AuthProvider tarafından ayarlanır. */
let oturumDustu: (() => void) | null = null
export function oturumDusunceCagir(fn: (() => void) | null) {
  oturumDustu = fn
}

api.interceptors.response.use(
  (r) => r,
  (hata: AxiosError) => {
    const girisIstegi = hata.config?.url?.includes('/auth/giris')
    if (hata.response?.status === 401 && !girisIstegi) oturumDustu?.()
    return Promise.reject(hata)
  },
)

/** Sunucunun ürettiği dosyayı (Excel vb.) indirir; dosya adı Content-Disposition başlığından alınır. */
export async function dosyaIndir(yol: string, varsayilanAd: string) {
  const yanit = await api.get<Blob>(yol, { responseType: 'blob' })
  const baslik = yanit.headers['content-disposition'] as string | undefined
  const ad = baslik?.match(/filename\*=UTF-8''([^;]+)/)?.[1] ?? baslik?.match(/filename="?([^";]+)"?/)?.[1]
  const url = URL.createObjectURL(yanit.data)
  const a = document.createElement('a')
  a.href = url
  a.download = ad ? decodeURIComponent(ad) : varsayilanAd
  document.body.appendChild(a)
  a.click()
  a.remove()
  setTimeout(() => URL.revokeObjectURL(url), 1000)
}

/** Backend'in { mesaj } biçimindeki hata mesajını kullanıcıya gösterilecek metne çevirir. */
export function hataMesaji(hata: unknown, varsayilan = 'Beklenmeyen bir hata oluştu. Lütfen tekrar deneyin.'): string {
  if (hata instanceof AxiosError) {
    const veri = hata.response?.data as { mesaj?: string; title?: string } | undefined
    if (veri?.mesaj) return veri.mesaj
    if (!hata.response) return 'Sunucuya ulaşılamadı. İnternet bağlantınızı kontrol edin.'
  }
  return varsayilan
}
