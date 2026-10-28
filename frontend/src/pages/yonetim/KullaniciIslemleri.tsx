import { useState } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { Loader2, Pencil, Search, ShieldCheck, UserPlus, UsersRound } from 'lucide-react'
import Modal from '../../components/Modal'
import { Alan, Uyari } from '../../components/form'
import { api, hataMesaji } from '../../lib/api'
import { useAuth } from '../../auth/AuthContext'
import { donemAdi, ogretimTuruKisa } from '../../lib/bicim'

interface DersOzeti {
  id: number
  kod: string
  ad: string
  akademikYil: string
  donem: string
}

interface KullaniciSatiri {
  id: number
  unvan: string
  ad: string
  soyad: string
  tamAd: string
  eposta: string
  personelNo: string
  rol: 'Admin' | 'OgretimUyesi'
  yoneticiYetkisi: boolean
  aktif: boolean
  sifreDegistirmeli: boolean
  dersler: DersOzeti[]
}

interface DersSatiri {
  id: number
  kod: string
  ad: string
  akademikYil: string
  donem: 'Guz' | 'Bahar'
  ogretimTuru: 'NormalOgretim' | 'IkinciOgretim'
  sinif: number
  egitmenler: { id: number; tamAd: string }[]
}

const unvanlar = ['Prof. Dr.', 'Doç. Dr.', 'Dr. Öğr. Üyesi', 'Öğr. Gör. Dr.', 'Öğr. Gör.', 'Arş. Gör. Dr.', 'Arş. Gör.']

export default function KullaniciIslemleri() {
  const [sekme, setSekme] = useState<'kullanicilar' | 'dersler'>('kullanicilar')

  return (
    <div className="kart p-5 sm:p-6">
      <h2 className="text-lg font-bold text-selcuk-lacivert">Kullanıcı İşlemleri</h2>
      <div className="mt-4 flex flex-wrap gap-2" role="tablist">
        <button role="tab" aria-selected={sekme === 'kullanicilar'} onClick={() => setSekme('kullanicilar')}
          className={sekme === 'kullanicilar' ? 'btn-birincil' : 'btn-ikincil'}>
          <UsersRound className="h-4 w-4" /> Öğretim Üyeleri
        </button>
        <button role="tab" aria-selected={sekme === 'dersler'} onClick={() => setSekme('dersler')}
          className={sekme === 'dersler' ? 'btn-birincil' : 'btn-ikincil'}>
          <ShieldCheck className="h-4 w-4" /> Derslere Eğitmen Atama
        </button>
      </div>
      <div className="mt-5">{sekme === 'kullanicilar' ? <KullaniciListesi /> : <EgitmenAtama />}</div>
    </div>
  )
}

function KullaniciListesi() {
  const { kullanici: ben } = useAuth()
  const [arama, setArama] = useState('')
  const [duzenlenen, setDuzenlenen] = useState<KullaniciSatiri | 'yeni' | null>(null)
  const [mesaj, setMesaj] = useState<string | null>(null)

  const kullanicilar = useQuery({
    queryKey: ['yonetim', 'kullanicilar'],
    queryFn: async () => (await api.get<KullaniciSatiri[]>('/yonetim/kullanicilar')).data,
  })

  const aranan = arama.trim().toLocaleLowerCase('tr-TR')
  const liste = (kullanicilar.data ?? []).filter(
    (k) => !aranan || `${k.tamAd} ${k.eposta} ${k.personelNo}`.toLocaleLowerCase('tr-TR').includes(aranan),
  )

  return (
    <>
      <div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
        <div className="relative sm:w-80">
          <Search className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-slate-400" />
          <input
            className="w-full rounded-lg border border-slate-300 py-2 pl-9 pr-3 text-sm focus:border-selcuk-mavi focus:outline-none focus:ring-2 focus:ring-selcuk-mavi/20"
            placeholder="Ad, e-posta veya personel no ara…"
            value={arama}
            onChange={(e) => setArama(e.target.value)}
          />
        </div>
        <button className="btn-birincil" onClick={() => setDuzenlenen('yeni')}>
          <UserPlus className="h-4 w-4" /> Yeni Öğretim Üyesi
        </button>
      </div>

      {mesaj && (
        <div className="mt-4">
          <Uyari tur="basari">{mesaj}</Uyari>
        </div>
      )}

      <div className="mt-4 overflow-x-auto">
        <table className="w-full min-w-[900px] text-left text-sm">
          <thead>
            <tr className="border-b border-slate-200 text-selcuk-metin-soluk">
              <th className="py-3 pr-4 font-semibold">Ad Soyad</th>
              <th className="py-3 pr-4 font-semibold">E-posta</th>
              <th className="py-3 pr-4 font-semibold">Personel No</th>
              <th className="py-3 pr-4 font-semibold">Yetki</th>
              <th className="py-3 pr-4 font-semibold">Dersleri</th>
              <th className="py-3 pr-4 font-semibold">Durum</th>
              <th className="py-3" />
            </tr>
          </thead>
          <tbody>
            {kullanicilar.isLoading && (
              <tr>
                <td colSpan={7} className="py-8 text-center">
                  <Loader2 className="mx-auto h-5 w-5 animate-spin text-selcuk-mavi" />
                </td>
              </tr>
            )}
            {liste.map((k) => (
              <tr key={k.id} className="border-b border-slate-100 align-top last:border-0">
                <td className="py-3 pr-4">
                  <div className="whitespace-nowrap font-semibold">{k.tamAd}</div>
                  {k.id === ben?.id && <div className="text-xs text-selcuk-metin-soluk">(siz)</div>}
                </td>
                <td className="py-3 pr-4 whitespace-nowrap">{k.eposta}</td>
                <td className="py-3 pr-4">{k.personelNo}</td>
                <td className="py-3 pr-4">
                  {k.rol === 'Admin' ? (
                    <Rozet renk="lacivert">Admin</Rozet>
                  ) : k.yoneticiYetkisi ? (
                    <Rozet renk="altin">Öğretim Üyesi + Yönetici</Rozet>
                  ) : (
                    <Rozet renk="gri">Öğretim Üyesi</Rozet>
                  )}
                </td>
                <td className="py-3 pr-4">
                  {k.rol === 'Admin' ? (
                    <span className="text-selcuk-metin-soluk">Kendi dersi yoktur</span>
                  ) : k.dersler.length === 0 ? (
                    <span className="text-selcuk-metin-soluk">—</span>
                  ) : (
                    <ul className="space-y-0.5">
                      {k.dersler.map((d) => (
                        <li key={d.id}>
                          <span className="font-mono text-xs text-selcuk-metin-soluk">{d.kod}</span> {d.ad}
                        </li>
                      ))}
                    </ul>
                  )}
                </td>
                <td className="py-3 pr-4">
                  {!k.aktif ? (
                    <Rozet renk="kirmizi">Pasif</Rozet>
                  ) : k.sifreDegistirmeli ? (
                    <Rozet renk="sari">İlk giriş bekleniyor</Rozet>
                  ) : (
                    <Rozet renk="yesil">Aktif</Rozet>
                  )}
                </td>
                <td className="py-3 text-right">
                  {k.rol !== 'Admin' && (
                    <button className="btn-ikincil px-3 py-1.5" onClick={() => setDuzenlenen(k)}>
                      <Pencil className="h-4 w-4" /> Düzenle
                    </button>
                  )}
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      {duzenlenen && (
        <KullaniciFormu
          kayit={duzenlenen === 'yeni' ? null : duzenlenen}
          kapat={() => setDuzenlenen(null)}
          tamamlandi={(m) => {
            setMesaj(m)
            setDuzenlenen(null)
          }}
        />
      )}
    </>
  )
}

function KullaniciFormu({
  kayit,
  kapat,
  tamamlandi,
}: {
  kayit: KullaniciSatiri | null
  kapat: () => void
  tamamlandi: (mesaj: string) => void
}) {
  const { kullanici: ben } = useAuth()
  const queryClient = useQueryClient()
  const adminMi = ben?.rol === 'Admin'
  const [form, setForm] = useState({
    unvan: kayit?.unvan ?? 'Dr. Öğr. Üyesi',
    ad: kayit?.ad ?? '',
    soyad: kayit?.soyad ?? '',
    eposta: kayit?.eposta ?? '',
    personelNo: kayit?.personelNo ?? '',
    yoneticiYetkisi: kayit?.yoneticiYetkisi ?? false,
    aktif: kayit?.aktif ?? true,
  })
  const degistir = (alan: keyof typeof form) => (e: React.ChangeEvent<HTMLInputElement | HTMLSelectElement>) =>
    setForm((f) => ({ ...f, [alan]: e.target.type === 'checkbox' ? (e.target as HTMLInputElement).checked : e.target.value }))

  const kaydet = useMutation({
    mutationFn: async () =>
      kayit
        ? (await api.put<KullaniciSatiri>(`/yonetim/kullanicilar/${kayit.id}`, form)).data
        : (await api.post<KullaniciSatiri>('/yonetim/kullanicilar', form)).data,
    onSuccess: (k) => {
      queryClient.invalidateQueries({ queryKey: ['yonetim'] })
      tamamlandi(kayit ? `${k.tamAd} güncellendi.` : `${k.tamAd} eklendi. İlk girişte varsayılan şifresini değiştirmesi istenecek.`)
    },
  })

  const onizlemeSifre =
    form.eposta.includes('@') && form.personelNo ? `${form.eposta.split('@')[0].trim().toLowerCase()}${form.personelNo.trim()}*` : null

  return (
    <Modal
      baslik={kayit ? 'Öğretim Üyesini Düzenle' : 'Yeni Öğretim Üyesi'}
      aciklama={kayit ? kayit.eposta : 'Hesap oluşturulduğunda varsayılan şifre otomatik üretilir.'}
      kapat={kapat}
    >
      <form
        className="space-y-4"
        onSubmit={(e) => {
          e.preventDefault()
          kaydet.mutate()
        }}
      >
        {kaydet.isError && <Uyari tur="hata">{hataMesaji(kaydet.error)}</Uyari>}

        <div>
          <label className="mb-1.5 block text-sm font-semibold" htmlFor="unvan">Unvan</label>
          <select id="unvan" value={form.unvan} onChange={degistir('unvan')}
            className="w-full rounded-lg border border-slate-300 bg-white px-3 py-2.5 text-sm focus:border-selcuk-mavi focus:outline-none focus:ring-2 focus:ring-selcuk-mavi/20">
            {unvanlar.map((u) => <option key={u}>{u}</option>)}
          </select>
        </div>
        <div className="grid gap-4 sm:grid-cols-2">
          <Alan etiket="Ad" value={form.ad} onChange={degistir('ad')} required />
          <Alan etiket="Soyad" value={form.soyad} onChange={degistir('soyad')} required />
        </div>
        {!kayit && (
          <Alan etiket="E-posta" type="email" placeholder="ad.soyad@selcuk.edu.tr" value={form.eposta} onChange={degistir('eposta')} required />
        )}
        <Alan etiket="Personel No" inputMode="numeric" value={form.personelNo} onChange={degistir('personelNo')} required />

        {!kayit && onizlemeSifre && (
          <Uyari tur="bilgi">
            Varsayılan şifre: <b className="font-mono">{onizlemeSifre}</b>
          </Uyari>
        )}

        <label className={`flex items-start gap-3 rounded-lg border border-slate-200 p-3 ${!adminMi ? 'opacity-60' : ''}`}>
          <input type="checkbox" className="mt-0.5 h-4 w-4 accent-selcuk-mavi" checked={form.yoneticiYetkisi}
            onChange={degistir('yoneticiYetkisi')} disabled={!adminMi} />
          <span>
            <span className="block text-sm font-semibold">Yönetici yetkisi</span>
            <span className="block text-xs text-selcuk-metin-soluk">
              Kendi derslerine ek olarak şifre sıfırlama, kullanıcı işlemleri, notlandırma ve optik form ayarlarını yönetir.
              {!adminMi && ' Bu yetkiyi yalnızca sistem yöneticisi verebilir.'}
            </span>
          </span>
        </label>

        {kayit && kayit.id !== ben?.id && (
          <label className="flex items-center gap-3 rounded-lg border border-slate-200 p-3">
            <input type="checkbox" className="h-4 w-4 accent-selcuk-mavi" checked={form.aktif} onChange={degistir('aktif')} />
            <span className="text-sm font-semibold">Hesap aktif</span>
          </label>
        )}

        <div className="flex justify-end gap-2 pt-2">
          <button type="button" className="btn-ikincil" onClick={kapat}>Vazgeç</button>
          <button type="submit" className="btn-birincil" disabled={kaydet.isPending}>
            {kaydet.isPending && <Loader2 className="h-4 w-4 animate-spin" />} Kaydet
          </button>
        </div>
      </form>
    </Modal>
  )
}

function EgitmenAtama() {
  const [secilen, setSecilen] = useState<DersSatiri | null>(null)
  const [mesaj, setMesaj] = useState<string | null>(null)

  const dersler = useQuery({
    queryKey: ['yonetim', 'dersler'],
    queryFn: async () => (await api.get<DersSatiri[]>('/yonetim/dersler')).data,
  })

  return (
    <>
      <p className="text-sm text-selcuk-metin-soluk">
        Bir öğretim üyesi yalnızca eğitmen olarak atandığı derslerde sınav girişi yapabilir ve rapor görebilir.
      </p>
      {mesaj && (
        <div className="mt-4">
          <Uyari tur="basari">{mesaj}</Uyari>
        </div>
      )}
      <div className="mt-4 overflow-x-auto">
        <table className="w-full min-w-[720px] text-left text-sm">
          <thead>
            <tr className="border-b border-slate-200 text-selcuk-metin-soluk">
              <th className="py-3 pr-4 font-semibold">Kod</th>
              <th className="py-3 pr-4 font-semibold">Ders</th>
              <th className="py-3 pr-4 font-semibold">Dönem</th>
              <th className="py-3 pr-4 font-semibold">Eğitmen(ler)</th>
              <th className="py-3" />
            </tr>
          </thead>
          <tbody>
            {dersler.isLoading && (
              <tr>
                <td colSpan={5} className="py-8 text-center">
                  <Loader2 className="mx-auto h-5 w-5 animate-spin text-selcuk-mavi" />
                </td>
              </tr>
            )}
            {dersler.data?.map((d) => (
              <tr key={d.id} className="border-b border-slate-100 align-top last:border-0">
                <td className="py-3 pr-4 font-mono text-xs">{d.kod}</td>
                <td className="py-3 pr-4">
                  <div className="font-semibold">{d.ad}</div>
                  <div className="text-xs text-selcuk-metin-soluk">
                    {d.sinif}. sınıf · {ogretimTuruKisa(d.ogretimTuru)}
                  </div>
                </td>
                <td className="py-3 pr-4 whitespace-nowrap">{d.akademikYil} {donemAdi(d.donem)}</td>
                <td className="py-3 pr-4">
                  {d.egitmenler.length === 0 ? (
                    <span className="text-red-600">Atanmadı</span>
                  ) : (
                    d.egitmenler.map((e) => <div key={e.id}>{e.tamAd}</div>)
                  )}
                </td>
                <td className="py-3 text-right">
                  <button className="btn-ikincil px-3 py-1.5" onClick={() => setSecilen(d)}>
                    <UsersRound className="h-4 w-4" /> Eğitmen Ata
                  </button>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
      {secilen && (
        <EgitmenSecimi
          ders={secilen}
          kapat={() => setSecilen(null)}
          tamamlandi={() => {
            setMesaj(`${secilen.ad} dersinin eğitmen ataması kaydedildi.`)
            setSecilen(null)
          }}
        />
      )}
    </>
  )
}

function EgitmenSecimi({ ders, kapat, tamamlandi }: { ders: DersSatiri; kapat: () => void; tamamlandi: () => void }) {
  const queryClient = useQueryClient()
  const [secili, setSecili] = useState<number[]>(ders.egitmenler.map((e) => e.id))

  const kullanicilar = useQuery({
    queryKey: ['yonetim', 'kullanicilar'],
    queryFn: async () => (await api.get<KullaniciSatiri[]>('/yonetim/kullanicilar')).data,
  })
  const ogretimUyeleri = (kullanicilar.data ?? []).filter((k) => k.rol === 'OgretimUyesi' && k.aktif)

  const kaydet = useMutation({
    mutationFn: async () => api.put(`/yonetim/dersler/${ders.id}/egitmenler`, { kullaniciIdleri: secili }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['yonetim'] })
      tamamlandi()
    },
  })

  return (
    <Modal baslik="Eğitmen Ata" aciklama={`${ders.kod} · ${ders.ad}`} kapat={kapat}>
      {kaydet.isError && (
        <div className="mb-4">
          <Uyari tur="hata">{hataMesaji(kaydet.error)}</Uyari>
        </div>
      )}
      <ul className="max-h-80 space-y-2 overflow-y-auto">
        {ogretimUyeleri.map((k) => (
          <li key={k.id}>
            <label className="flex items-center gap-3 rounded-lg border border-slate-200 p-3 hover:bg-selcuk-zemin">
              <input
                type="checkbox"
                className="h-4 w-4 accent-selcuk-mavi"
                checked={secili.includes(k.id)}
                onChange={(e) => setSecili((s) => (e.target.checked ? [...s, k.id] : s.filter((x) => x !== k.id)))}
              />
              <span>
                <span className="block text-sm font-semibold">{k.tamAd}</span>
                <span className="block text-xs text-selcuk-metin-soluk">{k.eposta}</span>
              </span>
            </label>
          </li>
        ))}
      </ul>
      <div className="mt-5 flex justify-end gap-2">
        <button className="btn-ikincil" onClick={kapat}>Vazgeç</button>
        <button className="btn-birincil" disabled={kaydet.isPending || secili.length === 0} onClick={() => kaydet.mutate()}>
          {kaydet.isPending && <Loader2 className="h-4 w-4 animate-spin" />} Kaydet
        </button>
      </div>
    </Modal>
  )
}

function Rozet({ renk, children }: { renk: 'lacivert' | 'altin' | 'gri' | 'yesil' | 'sari' | 'kirmizi'; children: React.ReactNode }) {
  const stil = {
    lacivert: 'bg-selcuk-lacivert text-white',
    altin: 'bg-selcuk-altin/30 text-selcuk-lacivert',
    gri: 'bg-slate-100 text-slate-700',
    yesil: 'bg-emerald-100 text-emerald-800',
    sari: 'bg-amber-100 text-amber-800',
    kirmizi: 'bg-red-100 text-red-700',
  }[renk]
  return <span className={`inline-block whitespace-nowrap rounded-full px-2.5 py-1 text-xs font-semibold ${stil}`}>{children}</span>
}
