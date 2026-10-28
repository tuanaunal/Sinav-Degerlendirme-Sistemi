import { createContext, useCallback, useContext, useEffect, useMemo, useState } from 'react'
import { useQueryClient } from '@tanstack/react-query'
import { api, oturumDusunceCagir, tokenDeposu } from '../lib/api'

export type Rol = 'Admin' | 'OgretimUyesi'

export interface Kullanici {
  id: number
  tamAd: string
  unvan: string
  ad: string
  soyad: string
  eposta: string
  rol: Rol
  yoneticiYetkisi: boolean
  sifreDegistirmeli: boolean
}

export interface OturumYaniti {
  token: string
  gecerlilikSonu: string
  kullanici: Kullanici
}

interface AuthDegeri {
  kullanici: Kullanici | null
  /** İlk açılışta kayıtlı token doğrulanırken true */
  yukleniyor: boolean
  oturumAc: (oturum: OturumYaniti) => void
  cikisYap: () => void
  /** Admin veya yönetici yetkisi verilmiş öğretim üyesi */
  yoneticiMi: boolean
}

const AuthContext = createContext<AuthDegeri | null>(null)

export function AuthProvider({ children }: { children: React.ReactNode }) {
  const queryClient = useQueryClient()
  const [kullanici, setKullanici] = useState<Kullanici | null>(null)
  const [yukleniyor, setYukleniyor] = useState(() => tokenDeposu.oku() !== null)

  const cikisYap = useCallback(() => {
    tokenDeposu.yaz(null)
    setKullanici(null)
    queryClient.clear()
  }, [queryClient])

  const oturumAc = useCallback((oturum: OturumYaniti) => {
    tokenDeposu.yaz(oturum.token)
    setKullanici(oturum.kullanici)
  }, [])

  // Sayfa yenilendiğinde kayıtlı token ile kullanıcıyı geri yükle
  useEffect(() => {
    if (!tokenDeposu.oku()) return
    api
      .get<Kullanici>('/auth/ben')
      .then((r) => setKullanici(r.data))
      .catch(() => tokenDeposu.yaz(null))
      .finally(() => setYukleniyor(false))
  }, [])

  useEffect(() => {
    oturumDusunceCagir(cikisYap)
    return () => oturumDusunceCagir(null)
  }, [cikisYap])

  const deger = useMemo<AuthDegeri>(
    () => ({
      kullanici,
      yukleniyor,
      oturumAc,
      cikisYap,
      yoneticiMi: !!kullanici && (kullanici.rol === 'Admin' || kullanici.yoneticiYetkisi),
    }),
    [kullanici, yukleniyor, oturumAc, cikisYap],
  )

  return <AuthContext.Provider value={deger}>{children}</AuthContext.Provider>
}

export function useAuth() {
  const deger = useContext(AuthContext)
  if (!deger) throw new Error('useAuth, AuthProvider içinde kullanılmalıdır')
  return deger
}

/** Rolüne göre girişten sonra gidilecek sayfa: admin yönetim paneline, öğretim üyesi ders panosuna. */
export function anaSayfaYolu(k: Kullanici): string {
  return k.rol === 'Admin' ? '/yonetim' : '/panom'
}
