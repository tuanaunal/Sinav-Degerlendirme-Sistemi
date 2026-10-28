import { useState } from 'react'
import { Navigate, useNavigate } from 'react-router-dom'
import { useMutation } from '@tanstack/react-query'
import { Check, KeyRound, Loader2, X } from 'lucide-react'
import GirisDuzeni from '../../components/layout/GirisDuzeni'
import SayfaIskeleti from '../../components/layout/SayfaIskeleti'
import KullaniciMenusu from '../../components/layout/KullaniciMenusu'
import { SifreAlani, Uyari } from '../../components/form'
import { api, hataMesaji } from '../../lib/api'
import { anaSayfaYolu, useAuth, type OturumYaniti } from '../../auth/AuthContext'
import { YukleniyorEkrani } from '../../auth/KorumaliRota'

/** Backend'deki SifreKurali ile aynı kurallar; kullanıcı yazarken anlık gösterilir. */
const kurallar = [
  { metin: 'En az 8 karakter', kontrol: (s: string) => s.length >= 8 },
  { metin: 'En az bir harf', kontrol: (s: string) => /\p{L}/u.test(s) },
  { metin: 'En az bir rakam', kontrol: (s: string) => /\d/.test(s) },
]

function SifreFormu({ zorunlu }: { zorunlu: boolean }) {
  const { kullanici, oturumAc, cikisYap } = useAuth()
  const navigate = useNavigate()
  const [mevcut, setMevcut] = useState('')
  const [yeni, setYeni] = useState('')
  const [tekrar, setTekrar] = useState('')

  const degistir = useMutation({
    mutationFn: async () =>
      (
        await api.post<OturumYaniti>('/auth/sifre-degistir', {
          mevcutSifre: mevcut,
          yeniSifre: yeni,
          yeniSifreTekrar: tekrar,
        })
      ).data,
    onSuccess: (oturum) => {
      oturumAc(oturum)
      if (zorunlu) navigate(anaSayfaYolu(oturum.kullanici), { replace: true })
      else {
        setMevcut('')
        setYeni('')
        setTekrar('')
      }
    },
  })

  const kurallarTamam = kurallar.every((k) => k.kontrol(yeni))
  const eslesiyor = yeni.length > 0 && yeni === tekrar

  return (
    <form
      className="space-y-4"
      onSubmit={(e) => {
        e.preventDefault()
        degistir.mutate()
      }}
    >
      {zorunlu && (
        <Uyari tur="uyari">
          Hoş geldiniz, <b>{kullanici?.tamAd}</b>. Sistemi kullanmaya başlamadan önce varsayılan şifrenizi
          değiştirmeniz gerekmektedir.
        </Uyari>
      )}
      {degistir.isError && <Uyari tur="hata">{hataMesaji(degistir.error)}</Uyari>}
      {degistir.isSuccess && !zorunlu && <Uyari tur="basari">Şifreniz başarıyla değiştirildi.</Uyari>}

      <SifreAlani
        etiket={zorunlu ? 'Mevcut (varsayılan) şifre' : 'Mevcut şifre'}
        autoComplete="current-password"
        value={mevcut}
        onChange={(e) => setMevcut(e.target.value)}
        required
      />
      <SifreAlani
        etiket="Yeni şifre"
        autoComplete="new-password"
        value={yeni}
        onChange={(e) => setYeni(e.target.value)}
        required
      />
      <ul className="grid gap-1 text-xs sm:grid-cols-3">
        {kurallar.map((k) => {
          const tamam = k.kontrol(yeni)
          return (
            <li key={k.metin} className={`flex items-center gap-1 ${tamam ? 'text-emerald-600' : 'text-slate-500'}`}>
              {tamam ? <Check className="h-3.5 w-3.5" /> : <X className="h-3.5 w-3.5" />}
              {k.metin}
            </li>
          )
        })}
      </ul>
      <SifreAlani
        etiket="Yeni şifre (tekrar)"
        autoComplete="new-password"
        value={tekrar}
        onChange={(e) => setTekrar(e.target.value)}
        required
      />
      {tekrar.length > 0 && !eslesiyor && <p className="text-xs text-red-600">Şifreler birbiriyle uyuşmuyor.</p>}

      <button
        type="submit"
        className="btn-birincil w-full py-2.5"
        disabled={degistir.isPending || !kurallarTamam || !eslesiyor || !mevcut}
      >
        {degistir.isPending ? <Loader2 className="h-4 w-4 animate-spin" /> : <KeyRound className="h-4 w-4" />}
        Şifreyi Değiştir
      </button>

      {zorunlu && (
        <button type="button" onClick={cikisYap} className="w-full text-center text-sm font-semibold text-selcuk-mavi hover:underline">
          Çıkış yap
        </button>
      )}
    </form>
  )
}

/**
 * İlk girişte veya şifre sıfırlandıktan sonra zorunlu şifre değiştirme ekranı;
 * normal kullanıcılar için kullanıcı menüsünden açılan şifre değiştirme sayfası.
 */
export default function SifreDegistir() {
  const { kullanici, yukleniyor } = useAuth()

  if (yukleniyor) return <YukleniyorEkrani />
  if (!kullanici) return <Navigate to="/giris" replace />

  if (kullanici.sifreDegistirmeli) {
    return (
      <GirisDuzeni baslik="Yeni Şifre Belirleyin" aciklama="Güvenliğiniz için kendinize ait bir şifre belirleyin.">
        <SifreFormu zorunlu />
      </GirisDuzeni>
    )
  }

  return (
    <SayfaIskeleti
      ustBarSag={<KullaniciMenusu />}
      breadcrumb={[{ etiket: 'Sınav Değerlendirme Sistemi', yol: anaSayfaYolu(kullanici) }, { etiket: 'Şifre Değiştir' }]}
    >
      <div className="mx-auto max-w-md">
        <h1 className="text-2xl font-bold text-selcuk-lacivert">Şifre Değiştir</h1>
        <div className="kart mt-4 p-6">
          <SifreFormu zorunlu={false} />
        </div>
      </div>
    </SayfaIskeleti>
  )
}
