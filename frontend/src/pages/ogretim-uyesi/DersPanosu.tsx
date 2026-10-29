import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { BookOpen, CalendarRange, ChevronRight, Loader2, Plus, Trash2, Users } from 'lucide-react'
import Sayfa from '../../components/layout/Sayfa'
import Modal from '../../components/Modal'
import { Alan, Uyari } from '../../components/form'
import { useAuth } from '../../auth/AuthContext'
import { api, hataMesaji } from '../../lib/api'
import { donemAdi, ogretimTuruKisa } from '../../lib/bicim'
import { dersSorgulari, sinavTuruAdi, type DersKarti, type SinavTuru } from '../../lib/dersler'

const YIL_ANAHTARI = 'sds.akademikYil'

function kayitliYil(): string | null {
  try {
    return localStorage.getItem(YIL_ANAHTARI)
  } catch {
    return null
  }
}

/** Öğretim üyesinin seçtiği akademik yıla ait dersleri kart hâlinde gösterir. */
export default function DersPanosu() {
  const { kullanici } = useAuth()
  const [yeniDers, setYeniDers] = useState(false)
  const yillar = useQuery(dersSorgulari.yillar())
  const [yil, setYil] = useState<string | null>(kayitliYil)

  // Kayıtlı yıl listede yoksa (veya hiç seçilmemişse) içinde bulunulan yıla dön
  useEffect(() => {
    if (yillar.data && (!yil || !yillar.data.yillar.includes(yil))) setYil(yillar.data.guncel)
  }, [yillar.data, yil])

  useEffect(() => {
    if (!yil) return
    try {
      localStorage.setItem(YIL_ANAHTARI, yil)
    } catch {
      /* depolama kullanılamıyor */
    }
  }, [yil])

  const dersler = useQuery({ ...dersSorgulari.liste(yil ?? ''), enabled: !!yil })

  return (
    <Sayfa breadcrumb={[{ etiket: 'Sınav Değerlendirme Sistemi' }]}>
      <div className="flex flex-col gap-4 sm:flex-row sm:items-end sm:justify-between">
        <div>
          <h1 className="text-2xl font-bold text-selcuk-lacivert">Derslerim</h1>
          <p className="mt-1 text-sm text-selcuk-metin-soluk">Hoş geldiniz, {kullanici?.tamAd}.</p>
        </div>
        <label className="flex items-center gap-2 text-sm font-semibold">
          <CalendarRange className="h-4 w-4 text-selcuk-mavi" />
          Akademik Yıl
          <select
            className="rounded-lg border border-slate-300 bg-white px-3 py-2 text-sm font-semibold focus:border-selcuk-mavi focus:outline-none focus:ring-2 focus:ring-selcuk-mavi/20"
            value={yil ?? ''}
            onChange={(e) => setYil(e.target.value)}
          >
            {yillar.data?.yillar.map((y) => <option key={y}>{y}</option>)}
          </select>
        </label>
      </div>

      <div className="mt-6 grid gap-4 sm:grid-cols-2 xl:grid-cols-3">
        {dersler.isLoading && (
          <div className="kart col-span-full flex justify-center p-10">
            <Loader2 className="h-6 w-6 animate-spin text-selcuk-mavi" />
          </div>
        )}
        {dersler.data?.map((d) => <DersKartiBileseni key={d.id} ders={d} />)}

        <button
          type="button"
          onClick={() => setYeniDers(true)}
          className="flex min-h-[176px] flex-col items-center justify-center gap-2 rounded-xl border-2 border-dashed border-slate-300 bg-white/60 p-5 text-selcuk-metin-soluk transition-colors hover:border-selcuk-altin hover:bg-white hover:text-selcuk-lacivert"
        >
          <span className="flex h-11 w-11 items-center justify-center rounded-full bg-selcuk-altin text-selcuk-lacivert">
            <Plus className="h-5 w-5" />
          </span>
          <span className="font-semibold">Yeni Ders Ekle</span>
        </button>
      </div>

      {dersler.data?.length === 0 && (
        <p className="mt-4 text-sm text-selcuk-metin-soluk">
          {yil} akademik yılında tanımlı dersiniz yok. "Yeni Ders Ekle" ile ders oluşturabilirsiniz.
        </p>
      )}

      {yeniDers && yil && <YeniDersFormu varsayilanYil={yil} kapat={() => setYeniDers(false)} eklendi={setYil} />}
    </Sayfa>
  )
}

function DersKartiBileseni({ ders }: { ders: DersKarti }) {
  const durumu = (t: SinavTuru) => ders.sinavlar.find((s) => s.tur === t)?.durum
  const [silinecek, setSilinecek] = useState(false)
  return (
    <div className="relative">
    <Link
      to={`/dersler/${ders.id}`}
      className="kart group flex min-h-[176px] flex-col p-5 transition-shadow hover:shadow-md focus:outline-none focus:ring-2 focus:ring-selcuk-mavi/30"
    >
      <div className="flex items-start justify-between gap-3">
        <span className="flex h-11 w-11 shrink-0 items-center justify-center rounded-lg bg-selcuk-mavi-acik text-selcuk-mavi">
          <BookOpen className="h-5 w-5" />
        </span>
        <span className="mr-8 rounded-md bg-selcuk-zemin px-2 py-1 font-mono text-xs font-semibold text-selcuk-metin-soluk">
          {ders.kod}
        </span>
      </div>
      <h2 className="mt-3 font-bold leading-snug text-selcuk-lacivert">{ders.ad}</h2>
      <p className="mt-1 text-xs text-selcuk-metin-soluk">
        {donemAdi(ders.donem)} · {ders.sinif}. sınıf · {ogretimTuruKisa(ders.ogretimTuru)}
      </p>
      {ders.egitmenler.length > 1 && (
        <p className="mt-1 flex items-center gap-1 text-xs text-selcuk-metin-soluk">
          <Users className="h-3.5 w-3.5" /> {ders.egitmenler.length} eğitmen
        </p>
      )}
      <div className="mt-auto flex items-center justify-between pt-4">
        <div className="flex gap-1.5">
          {(['Vize', 'Final', 'Butunleme'] as SinavTuru[]).map((t) => {
            const d = durumu(t)
            const stil =
              d === 'Tamamlandi'
                ? 'bg-emerald-100 text-emerald-800'
                : d === 'Taslak'
                  ? 'bg-amber-100 text-amber-800'
                  : 'bg-slate-100 text-slate-400'
            return (
              <span key={t} className={`rounded-md px-2 py-0.5 text-xs font-semibold ${stil}`} title={`${sinavTuruAdi[t]}: ${d === 'Tamamlandi' ? 'girildi' : d === 'Taslak' ? 'devam ediyor' : 'girilmedi'}`}>
                {sinavTuruAdi[t]}
              </span>
            )
          })}
        </div>
        <ChevronRight className="h-5 w-5 text-slate-400 transition-transform group-hover:translate-x-0.5" />
      </div>
    </Link>
      <button className="absolute right-3 top-4 rounded-md p-1.5 text-slate-400 hover:bg-red-50 hover:text-red-600 focus:outline-none focus:ring-2 focus:ring-red-200"
        onClick={() => setSilinecek(true)} aria-label={`${ders.ad} dersini sil`} title="Dersi sil">
        <Trash2 className="h-4 w-4" />
      </button>
      {silinecek && <DersSilmeOnayi ders={ders} kapat={() => setSilinecek(false)} />}
    </div>
  )
}

function DersSilmeOnayi({ ders, kapat }: { ders: DersKarti; kapat: () => void }) {
  const istemci = useQueryClient()
  const sil = useMutation({
    meta: { bildirim: 'Ders silindi' },
    mutationFn: () => api.delete(`/dersler/${ders.id}`),
    onSuccess: async () => { await istemci.invalidateQueries({ queryKey: ['dersler'] }); kapat() },
  })
  return (
    <Modal baslik="Dersi Sil" aciklama={`${ders.kod} — ${ders.ad} (${ders.akademikYil})`} kapat={kapat}>
      <Uyari tur="uyari">
        Derse ait tüm sınavlar, yüklenen dosyalar, öğrenci puanları ve öğrenme çıktıları kalıcı olarak silinecek. Bu işlem geri alınamaz.
      </Uyari>
      {sil.isError && <div className="mt-3"><Uyari tur="hata">{hataMesaji(sil.error)}</Uyari></div>}
      <div className="mt-5 flex justify-end gap-2">
        <button className="btn-ikincil" onClick={kapat}>İptal</button>
        <button className="btn bg-red-600 text-white hover:bg-red-700" disabled={sil.isPending} onClick={() => sil.mutate()}>
          {sil.isPending ? <Loader2 className="h-4 w-4 animate-spin" /> : <Trash2 className="h-4 w-4" />} Dersi Sil
        </button>
      </div>
    </Modal>
  )
}

function YeniDersFormu({ varsayilanYil, kapat, eklendi }: { varsayilanYil: string; kapat: () => void; eklendi: (yil: string) => void }) {
  const queryClient = useQueryClient()
  const [form, setForm] = useState({
    kod: '',
    ad: '',
    akademikYil: varsayilanYil,
    donem: new Date().getMonth() + 1 >= 2 && new Date().getMonth() + 1 <= 8 ? 'Bahar' : 'Guz',
    ogretimTuru: 'NormalOgretim',
    sinif: 1,
  })
  const set = (alan: keyof typeof form) => (e: React.ChangeEvent<HTMLInputElement | HTMLSelectElement>) =>
    setForm((f) => ({ ...f, [alan]: alan === 'sinif' ? Number(e.target.value) : e.target.value }))

  const yilSecenekleri = (() => {
    const bas = Number(varsayilanYil.slice(0, 4))
    return [bas - 1, bas, bas + 1].map((y) => `${y}-${y + 1}`)
  })()

  const kaydet = useMutation({
    meta: { bildirim: 'Ders eklendi' },
    mutationFn: async () => (await api.post<DersKarti>('/dersler', form)).data,
    onSuccess: (d) => {
      queryClient.invalidateQueries({ queryKey: ['dersler'] })
      eklendi(d.akademikYil)
      kapat()
    },
  })

  const secimSinifi =
    'w-full rounded-lg border border-slate-300 bg-white px-3 py-2.5 text-sm focus:border-selcuk-mavi focus:outline-none focus:ring-2 focus:ring-selcuk-mavi/20'

  return (
    <Modal baslik="Yeni Ders Ekle" aciklama="Dersi oluşturduğunuzda otomatik olarak eğitmeni olursunuz." kapat={kapat}>
      <form
        className="space-y-4"
        onSubmit={(e) => {
          e.preventDefault()
          kaydet.mutate()
        }}
      >
        {kaydet.isError && <Uyari tur="hata">{hataMesaji(kaydet.error)}</Uyari>}
        <div className="grid gap-4 sm:grid-cols-[140px_1fr]">
          <Alan etiket="Ders Kodu" placeholder="3301104" value={form.kod} onChange={set('kod')} required autoFocus />
          <Alan etiket="Ders Adı" placeholder="Ayrık Matematik" value={form.ad} onChange={set('ad')} required />
        </div>
        <div className="grid gap-4 sm:grid-cols-2">
          <label className="text-sm font-semibold">
            Akademik Yıl
            <select className={`${secimSinifi} mt-1.5`} value={form.akademikYil} onChange={set('akademikYil')}>
              {yilSecenekleri.map((y) => <option key={y}>{y}</option>)}
            </select>
          </label>
          <label className="text-sm font-semibold">
            Dönem
            <select className={`${secimSinifi} mt-1.5`} value={form.donem} onChange={set('donem')}>
              <option value="Guz">Güz</option>
              <option value="Bahar">Bahar</option>
            </select>
          </label>
          <label className="text-sm font-semibold">
            Sınıf
            <select className={`${secimSinifi} mt-1.5`} value={form.sinif} onChange={set('sinif')}>
              {[1, 2, 3, 4].map((s) => <option key={s} value={s}>{s}. sınıf</option>)}
            </select>
          </label>
          <label className="text-sm font-semibold">
            Öğretim Türü
            <select className={`${secimSinifi} mt-1.5`} value={form.ogretimTuru} onChange={set('ogretimTuru')}>
              <option value="NormalOgretim">Normal Öğretim (NÖ)</option>
              <option value="IkinciOgretim">İkinci Öğretim (İÖ)</option>
            </select>
          </label>
        </div>
        <div className="flex justify-end gap-2 pt-2">
          <button type="button" className="btn-ikincil" onClick={kapat}>Vazgeç</button>
          <button type="submit" className="btn-birincil" disabled={kaydet.isPending}>
            {kaydet.isPending ? <Loader2 className="h-4 w-4 animate-spin" /> : <Plus className="h-4 w-4" />} Dersi Ekle
          </button>
        </div>
      </form>
    </Modal>
  )
}
