import { useState } from 'react'
import { Link } from 'react-router-dom'
import { useMutation } from '@tanstack/react-query'
import { ArrowLeft, Loader2, Send } from 'lucide-react'
import GirisDuzeni from '../../components/layout/GirisDuzeni'
import { Alan, Uyari } from '../../components/form'
import { api, hataMesaji } from '../../lib/api'

export default function SifremiUnuttum() {
  const [eposta, setEposta] = useState('')

  const talep = useMutation({
    mutationFn: async () => (await api.post<{ mesaj: string }>('/auth/sifremi-unuttum', { eposta })).data,
  })

  return (
    <GirisDuzeni
      baslik="Şifremi Unuttum"
      aciklama="E-posta adresinizi girin; şifre sıfırlama talebiniz sistem yöneticisine iletilsin."
    >
      {talep.isSuccess ? (
        <div className="space-y-4">
          <Uyari tur="basari">{talep.data.mesaj}</Uyari>
          <Uyari tur="bilgi">
            Onaydan sonra şifreniz <b>e-postanızın @ öncesi + personel numaranız + *</b> biçiminde olacaktır.
            Örnek: ali@selcuk.edu.tr, personel no 12345 → <b>ali12345*</b>
          </Uyari>
        </div>
      ) : (
        <form
          className="space-y-4"
          onSubmit={(e) => {
            e.preventDefault()
            talep.mutate()
          }}
        >
          {talep.isError && <Uyari tur="hata">{hataMesaji(talep.error)}</Uyari>}
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
          <button type="submit" className="btn-birincil w-full py-2.5" disabled={talep.isPending}>
            {talep.isPending ? <Loader2 className="h-4 w-4 animate-spin" /> : <Send className="h-4 w-4" />}
            Talep Gönder
          </button>
        </form>
      )}

      <Link
        to="/giris"
        className="mt-6 inline-flex items-center gap-1 text-sm font-semibold text-selcuk-mavi hover:underline"
      >
        <ArrowLeft className="h-4 w-4" /> Giriş ekranına dön
      </Link>
    </GirisDuzeni>
  )
}
