import { useState } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { Check, Loader2, X } from 'lucide-react'
import { Uyari } from '../../components/form'
import { api, hataMesaji } from '../../lib/api'
import { tarihSaat as tarih } from '../../lib/bicim'

type Durum = 'Bekliyor' | 'Onaylandi' | 'Reddedildi'

interface Talep {
  id: number
  adSoyad: string
  eposta: string
  talepTarihi: string
  durum: Durum
  islemTarihi: string | null
  isleyenKullanici: string | null
}

const sekmeler: { durum: Durum; ad: string }[] = [
  { durum: 'Bekliyor', ad: 'Bekleyenler' },
  { durum: 'Onaylandi', ad: 'Onaylananlar' },
  { durum: 'Reddedildi', ad: 'Reddedilenler' },
]


export default function SifreSifirlama() {
  const queryClient = useQueryClient()
  const [sekme, setSekme] = useState<Durum>('Bekliyor')
  const [mesaj, setMesaj] = useState<{ tur: 'basari' | 'hata'; metin: string } | null>(null)

  const talepler = useQuery({
    queryKey: ['sifre-talepleri', sekme],
    queryFn: async () => (await api.get<Talep[]>('/yonetim/sifre-talepleri', { params: { durum: sekme } })).data,
  })

  const islem = useMutation({
    mutationFn: async ({ id, tur }: { id: number; tur: 'onayla' | 'reddet' }) =>
      (await api.post<{ mesaj: string }>(`/yonetim/sifre-talepleri/${id}/${tur}`)).data,
    onSuccess: (r) => {
      setMesaj({ tur: 'basari', metin: r.mesaj })
      queryClient.invalidateQueries({ queryKey: ['sifre-talepleri'] })
    },
    onError: (e) => setMesaj({ tur: 'hata', metin: hataMesaji(e) }),
  })

  return (
    <div className="kart p-5 sm:p-6">
      <h2 className="text-lg font-bold text-selcuk-lacivert">Şifre Sıfırlama Talepleri</h2>

      <div className="mt-4 flex flex-wrap gap-2" role="tablist">
        {sekmeler.map((s) => (
          <button
            key={s.durum}
            role="tab"
            aria-selected={sekme === s.durum}
            onClick={() => {
              setSekme(s.durum)
              setMesaj(null)
            }}
            className={sekme === s.durum ? 'btn-birincil' : 'btn-ikincil'}
          >
            {s.ad}
          </button>
        ))}
      </div>

      {mesaj && (
        <div className="mt-4">
          <Uyari tur={mesaj.tur}>{mesaj.metin}</Uyari>
        </div>
      )}

      <div className="mt-4 overflow-x-auto">
        <table className="w-full min-w-[640px] text-left text-sm">
          <thead>
            <tr className="border-b border-slate-200 text-selcuk-metin-soluk">
              <th className="py-3 pr-4 font-semibold">Ad Soyad</th>
              <th className="py-3 pr-4 font-semibold">E-posta</th>
              <th className="py-3 pr-4 font-semibold">Talep Tarihi</th>
              <th className="py-3 pr-4 font-semibold">Durum</th>
              <th className="py-3 font-semibold">{sekme === 'Bekliyor' ? 'İşlem' : 'İşleyen'}</th>
            </tr>
          </thead>
          <tbody>
            {talepler.isLoading && (
              <tr>
                <td colSpan={5} className="py-8 text-center">
                  <Loader2 className="mx-auto h-5 w-5 animate-spin text-selcuk-mavi" />
                </td>
              </tr>
            )}
            {talepler.data?.length === 0 && (
              <tr>
                <td colSpan={5} className="py-8 text-center text-selcuk-metin-soluk">
                  Bu filtrede talep bulunamadı.
                </td>
              </tr>
            )}
            {talepler.data?.map((t) => (
              <tr key={t.id} className="border-b border-slate-100 last:border-0">
                <td className="py-3 pr-4 font-semibold">{t.adSoyad}</td>
                <td className="py-3 pr-4">{t.eposta}</td>
                <td className="py-3 pr-4 whitespace-nowrap">{tarih(t.talepTarihi)}</td>
                <td className="py-3 pr-4">
                  <DurumRozeti durum={t.durum} />
                </td>
                <td className="py-3">
                  {t.durum === 'Bekliyor' ? (
                    <div className="flex gap-2">
                      <button
                        className="btn bg-emerald-600 px-3 py-1.5 text-white hover:bg-emerald-700"
                        disabled={islem.isPending}
                        onClick={() => islem.mutate({ id: t.id, tur: 'onayla' })}
                      >
                        <Check className="h-4 w-4" /> Onayla
                      </button>
                      <button
                        className="btn border border-red-200 bg-white px-3 py-1.5 text-red-600 hover:bg-red-50"
                        disabled={islem.isPending}
                        onClick={() => islem.mutate({ id: t.id, tur: 'reddet' })}
                      >
                        <X className="h-4 w-4" /> Reddet
                      </button>
                    </div>
                  ) : (
                    <span className="text-selcuk-metin-soluk">
                      {t.isleyenKullanici} · {t.islemTarihi && tarih(t.islemTarihi)}
                    </span>
                  )}
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      <div className="mt-5">
        <Uyari tur="bilgi">
          <b>Bilgi:</b> Onaylanan taleplerde şifre sistem tarafından <b>mail adresinin @ işaretinden önceki kısmı + personel
          no + *</b> formatına döner. Örnek mail: ali@selcuk.edu.tr, personel no: 12345 → oluşturulan şifre:{' '}
          <b>ali12345*</b>. Kullanıcı ilk girişte yeni şifre belirler.
        </Uyari>
      </div>
    </div>
  )
}

function DurumRozeti({ durum }: { durum: Durum }) {
  const stil = {
    Bekliyor: 'bg-amber-100 text-amber-800',
    Onaylandi: 'bg-emerald-100 text-emerald-800',
    Reddedildi: 'bg-red-100 text-red-700',
  }[durum]
  const ad = { Bekliyor: 'Bekliyor', Onaylandi: 'Onaylandı', Reddedildi: 'Reddedildi' }[durum]
  return <span className={`rounded-full px-2.5 py-1 text-xs font-semibold ${stil}`}>{ad}</span>
}
