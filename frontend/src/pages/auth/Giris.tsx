import { useState } from 'react'
import { Link, Navigate, useLocation, useNavigate } from 'react-router-dom'
import { useMutation } from '@tanstack/react-query'
import { Loader2, LogIn } from 'lucide-react'
import GirisDuzeni from '../../components/layout/GirisDuzeni'
import { Alan, SifreAlani, Uyari } from '../../components/form'
import { api, hataMesaji } from '../../lib/api'
import { anaSayfaYolu, useAuth, type OturumYaniti } from '../../auth/AuthContext'

export default function Giris() {
  const { kullanici, oturumAc } = useAuth()
  const navigate = useNavigate()
  const konum = useLocation()
  const geri = (konum.state as { geri?: string } | null)?.geri

  const [eposta, setEposta] = useState('')
  const [sifre, setSifre] = useState('')

  const giris = useMutation({
    mutationFn: async () => (await api.post<OturumYaniti>('/auth/giris', { eposta, sifre })).data,
    onSuccess: (oturum) => {
      oturumAc(oturum)
      // İlk girişte veya şifre sıfırlandıktan sonra önce şifre değiştirilir
      if (oturum.kullanici.sifreDegistirmeli) navigate('/sifre-degistir', { replace: true })
      else navigate(geri ?? anaSayfaYolu(oturum.kullanici), { replace: true })
    },
  })

  if (kullanici && !giris.isPending) {
    return <Navigate to={kullanici.sifreDegistirmeli ? '/sifre-degistir' : anaSayfaYolu(kullanici)} replace />
  }

  return (
    <GirisDuzeni baslik="Giriş Yap" aciklama="Kurumsal e-posta adresiniz ve şifrenizle giriş yapın.">
      <form
        className="space-y-4"
        onSubmit={(e) => {
          e.preventDefault()
          giris.mutate()
        }}
      >
        {giris.isError && <Uyari tur="hata">{hataMesaji(giris.error)}</Uyari>}

        <Alan
          etiket="E-posta"
          type="email"
          autoComplete="username"
          placeholder="ad.soyad@selcuk.edu.tr"
          value={eposta}
          onChange={(e) => setEposta(e.target.value)}
          required
          autoFocus
        />
        <SifreAlani
          etiket="Şifre"
          autoComplete="current-password"
          value={sifre}
          onChange={(e) => setSifre(e.target.value)}
          required
        />

        <div className="flex justify-end">
          <Link to="/sifremi-unuttum" className="text-sm font-semibold text-selcuk-mavi hover:underline">
            Şifremi unuttum
          </Link>
        </div>

        <button type="submit" className="btn-birincil w-full py-2.5" disabled={giris.isPending}>
          {giris.isPending ? <Loader2 className="h-4 w-4 animate-spin" /> : <LogIn className="h-4 w-4" />}
          Giriş Yap
        </button>
      </form>

      <p className="mt-6 border-t border-slate-100 pt-4 text-xs leading-relaxed text-selcuk-metin-soluk">
        Hesabınız sistem yöneticisi tarafından tanımlanır. İlk girişte varsayılan şifrenizi değiştirmeniz istenecektir.
      </p>
    </GirisDuzeni>
  )
}
