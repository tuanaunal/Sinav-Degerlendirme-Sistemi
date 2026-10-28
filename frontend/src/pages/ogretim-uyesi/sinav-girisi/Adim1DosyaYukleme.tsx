import { useRef, useState } from 'react'
import { useMutation, useQueryClient } from '@tanstack/react-query'
import { ChevronDown, ChevronRight, FileSpreadsheet, FileText, Info, Loader2, Plus, Upload, X } from 'lucide-react'
import { Uyari } from '../../../components/form'
import { api, hataMesaji } from '../../../lib/api'
import { sinavAnahtari, type OptikOnizleme, type SinavBilgisi, type SinifListesiOnizleme } from '../../../lib/sinav'
import type { SinavFormati, SinavTuru } from '../../../lib/dersler'

const EN_FAZLA_LISTE = 2

interface SeciliOptik {
  dosya: File
  onizleme?: OptikOnizleme
  hata?: string
}
interface SeciliListe {
  dosya: File
  onizleme?: SinifListesiOnizleme
  hata?: string
}

const boyut = (b: number) => (b < 1024 * 1024 ? `${(b / 1024).toFixed(1)} KB` : `${(b / 1024 / 1024).toFixed(1)} MB`)

/**
 * 1. adım: optik okuyucu dosyası (.txt) ve sınıf listesi (.xlsx) yüklenir; seçilir seçilmez okunup özetlenir.
 * Açık uçlu sınavda yalnızca sınıf listesi yüklenir.
 */
export default function Adim1DosyaYukleme({
  dersId,
  tur,
  format,
  sinav,
  ileri,
}: {
  dersId: number
  tur: SinavTuru
  format: SinavFormati
  sinav: SinavBilgisi | null
  ileri: () => void
}) {
  const acikUclu = format === 'AcikUclu'
  const queryClient = useQueryClient()
  const [optik, setOptik] = useState<SeciliOptik | null>(null)
  const [listeler, setListeler] = useState<SeciliListe[]>([])
  // Daha önce yüklenmiş dosyalar (sayfaya geri dönüldüğünde)
  const [oncekiKullan, setOncekiKullan] = useState(!!sinav?.sinifListesiDosyaAdi)
  const [formatAcik, setFormatAcik] = useState(false)
  const optikInput = useRef<HTMLInputElement>(null)
  const listeInput = useRef<HTMLInputElement>(null)

  const onizle = async <T,>(yol: string, dosya: File) => {
    const form = new FormData()
    form.append('dosya', dosya)
    return (await api.post<T>(yol, form)).data
  }

  const optikSec = async (dosya: File) => {
    setOncekiKullan(false)
    setOptik({ dosya })
    try {
      setOptik({ dosya, onizleme: await onizle<OptikOnizleme>('/dosyalar/optik/onizle', dosya) })
    } catch (e) {
      setOptik({ dosya, hata: hataMesaji(e) })
    }
  }

  const listeEkle = async (dosya: File) => {
    setOncekiKullan(false)
    setListeler((l) => [...l, { dosya }])
    let sonuc: SeciliListe
    try {
      sonuc = { dosya, onizleme: await onizle<SinifListesiOnizleme>('/dosyalar/sinif-listesi/onizle', dosya) }
    } catch (e) {
      sonuc = { dosya, hata: hataMesaji(e) }
    }
    setListeler((l) => l.map((x) => (x.dosya === dosya ? sonuc : x)))
  }

  // Önizlemede zaten gösterilen uyarılar kayıttan sonra tekrar gösterilmez
  const gosterilenUyarilar = new Set([...(optik?.onizleme?.uyarilar ?? []), ...listeler.flatMap((l) => l.onizleme?.uyarilar ?? [])])
  const yeniUyarilar = (s: SinavBilgisi) => s.uyarilar.filter((u) => !gosterilenUyarilar.has(u))

  const yukle = useMutation({
    mutationFn: async () => {
      const form = new FormData()
      form.append('format', format)
      if (!acikUclu) form.append('optik', optik!.dosya)
      listeler.forEach((l) => form.append('sinifListeleri', l.dosya))
      return (await api.post<SinavBilgisi>(`/dersler/${dersId}/sinavlar/${tur.toLowerCase()}/dosyalar`, form)).data
    },
    onSuccess: (s) => {
      queryClient.setQueryData(sinavAnahtari(dersId, tur), s)
      queryClient.invalidateQueries({ queryKey: ['dersler'] })
      queryClient.removeQueries({ queryKey: ['cevap-anahtari', s.id] })
      if (yeniUyarilar(s).length === 0) ileri()
    },
  })

  const yeniDosyalarHazir =
    (acikUclu || (!!optik?.onizleme && !optik.hata)) && listeler.length > 0 && listeler.every((l) => l.onizleme)
  const devamEdilebilir = oncekiKullan || yeniDosyalarHazir
  const toplamOgrenci = listeler.reduce((t, l) => t + (l.onizleme?.ogrenciSayisi ?? 0), 0)

  return (
    <div className="kart p-5 sm:p-6">
      <h2 className="text-lg font-bold text-selcuk-lacivert">{acikUclu ? 'Açık Uçlu Sınıf Listesi' : 'Dosya Yükleme'}</h2>
      {acikUclu && (
        <div className="mt-4">
          <Uyari tur="uyari">
            Lütfen <b>OBİS</b> sisteminden temin ettiğiniz orijinal sınıf listesi dosyasını yükleyin. Öğrencilerin soru bazlı puanlarını
            3. adımda (Not Yükleme) bu listeden üretilen şablonla gireceksiniz.
          </Uyari>
        </div>
      )}

      {oncekiKullan && sinav && (
        <div className="mt-4">
          <Uyari tur="basari">
            Dosyalar daha önce yüklendi:{' '}
            {sinav.optikDosyaAdi && <><b>{sinav.optikDosyaAdi}</b> ({sinav.optikKagitSayisi} kağıt) ve{' '}</>}
            <b>{sinav.sinifListesiDosyaAdi}</b> ({sinav.sinifMevcudu} öğrenci). Değiştirmek için yeni dosya seçin.
          </Uyari>
        </div>
      )}

      <div className={`mt-5 grid gap-5 ${acikUclu ? 'max-w-xl' : 'lg:grid-cols-2'}`}>
        {/* Optik dosyası */}
        <div className={`space-y-3 ${acikUclu ? 'hidden' : ''}`}>
          <div className={`rounded-xl border-2 border-dashed p-4 ${optik?.onizleme ? 'border-emerald-300 bg-emerald-50/60' : 'border-emerald-300/70 bg-emerald-50/30'}`}>
            <div className="flex items-center gap-2 font-semibold">
              <Upload className="h-4 w-4" /> Optik Okuyucu Dosyası (.txt)
            </div>
            <input ref={optikInput} type="file" accept=".txt,.dat" hidden
              onChange={(e) => e.target.files?.[0] && optikSec(e.target.files[0])} />
            {optik ? (
              <>
                <button className="btn mt-4 w-full bg-red-100 py-2.5 text-red-700 hover:bg-red-200"
                  onClick={() => { setOptik(null); if (optikInput.current) optikInput.current.value = '' }}>
                  <X className="h-4 w-4" /> Kaldır
                </button>
                <DosyaKarti ad={optik.dosya.name} boyut={boyut(optik.dosya.size)} Ikon={FileText} yukleniyor={!optik.onizleme && !optik.hata} />
              </>
            ) : (
              <button className="btn mt-4 w-full bg-emerald-600 py-2.5 text-white hover:bg-emerald-700" onClick={() => optikInput.current?.click()}>
                <FileText className="h-4 w-4" /> Dosya Seç
              </button>
            )}
          </div>

          {optik?.hata && <Uyari tur="hata">{optik.hata}</Uyari>}
          {optik?.onizleme && (
            <div className="rounded-lg border border-slate-200 bg-white p-3 text-sm">
              <div className="grid grid-cols-2 gap-2 sm:grid-cols-4">
                <Ozet etiket="Öğrenci kağıdı" deger={optik.onizleme.ogrenciSayisi} />
                <Ozet etiket="Cevap anahtarı" deger={optik.onizleme.cevapAnahtariSayisi} />
                <Ozet etiket="Soru" deger={optik.onizleme.tahminiSoruSayisi} />
                <Ozet etiket="Kitapçık" deger={Object.entries(optik.onizleme.kitapcikDagilimi).map(([k, v]) => `${k}:${v}`).join(' ')} />
              </div>
              <p className="mt-2 text-xs text-selcuk-metin-soluk">Karakter kodlaması: {optik.onizleme.kodlama}</p>
            </div>
          )}
          {optik?.onizleme?.uyarilar.map((u) => <Uyari key={u} tur="uyari">{u}</Uyari>)}

          <div className="flex gap-2 rounded-lg border border-amber-200 bg-amber-50 px-3 py-2.5 text-sm text-amber-800">
            <Info className="mt-0.5 h-4 w-4 shrink-0" />
            Sistem yöneticisi tarafından girilmiş güncel optik formatı kullanılmaktadır. Mevcut optik dosyanızı yükleyebilirsiniz.
          </div>
        </div>

        {/* Sınıf listesi */}
        <div className="space-y-3">
          <div className="rounded-xl border-2 border-dashed border-amber-300 bg-amber-50/40 p-4">
            <div className="flex items-center justify-between gap-2">
              <span className="flex items-center gap-2 font-semibold">
                <Upload className="h-4 w-4" /> Sınıf Listesi (.xlsx)
              </span>
              <span className="text-sm text-selcuk-metin-soluk">{listeler.length}/{EN_FAZLA_LISTE}</span>
            </div>
            <input ref={listeInput} type="file" accept=".xlsx" hidden
              onChange={(e) => {
                if (e.target.files?.[0]) listeEkle(e.target.files[0])
                e.target.value = ''
              }} />
            <div className="mt-4 space-y-2">
              {listeler.map((l) => (
                <DosyaKarti key={l.dosya.name + l.dosya.size} ad={l.dosya.name} boyut={boyut(l.dosya.size)} Ikon={FileSpreadsheet}
                  yukleniyor={!l.onizleme && !l.hata} ek={l.onizleme ? `${l.onizleme.ogrenciSayisi} öğrenci` : undefined}
                  kaldir={() => setListeler((x) => x.filter((y) => y !== l))} />
              ))}
            </div>
            {listeler.length < EN_FAZLA_LISTE && (
              <button className="btn-birincil mt-3 w-full py-2.5" onClick={() => listeInput.current?.click()}>
                <Plus className="h-4 w-4" /> Dosya Ekle
              </button>
            )}
          </div>

          {listeler.filter((l) => l.hata).map((l) => <Uyari key={l.dosya.name} tur="hata">{l.dosya.name}: {l.hata}</Uyari>)}
          {listeler.flatMap((l) => l.onizleme?.uyarilar ?? []).map((u) => <Uyari key={u} tur="uyari">{u}</Uyari>)}
          {listeler.length > 1 && toplamOgrenci > 0 && (
            <p className="text-sm text-selcuk-metin-soluk">İki şubenin listesi birleştirilecek: toplam {toplamOgrenci} öğrenci.</p>
          )}

          <div className="rounded-lg border border-slate-200 bg-white">
            <button type="button" onClick={() => setFormatAcik((a) => !a)}
              className="flex w-full items-center justify-between px-3 py-2.5 text-sm font-semibold">
              <span className="flex items-center gap-2"><Info className="h-4 w-4 text-emerald-600" /> Sınıf Listesi Formatı Nasıl Olmalı?</span>
              {formatAcik ? <ChevronDown className="h-4 w-4" /> : <ChevronRight className="h-4 w-4" />}
            </button>
            {formatAcik && (
              <div className="border-t border-slate-100 px-3 py-3 text-sm text-selcuk-metin-soluk">
                <p>OBİS'ten indirdiğiniz orijinal sınıf listesini doğrudan yükleyebilirsiniz. Sistem başlık satırını kendisi bulur.</p>
                <ul className="mt-2 list-disc space-y-1 pl-5">
                  <li><b>Öğrenci No</b> (veya OGRENCINO / Numara) sütunu zorunludur.</li>
                  <li><b>Adı</b> + <b>Soyadı</b> ya da tek bir <b>Ad Soyad</b> sütunu bulunmalıdır.</li>
                  <li>İki şube için iki ayrı liste yükleyebilirsiniz; listeler birleştirilir.</li>
                </ul>
              </div>
            )}
          </div>
        </div>
      </div>

      {!oncekiKullan && sinav && sinav.tamamlananAdim > 1 && (optik || listeler.length > 0) && (
        <div className="mt-4">
          <Uyari tur="uyari">
            Yeni dosya yüklerseniz bu sınavın eşleştirmesi, puanları ve eklemeleri sıfırlanır{sinav.durum === 'Tamamlandi' ? '; sınav yeniden tamamlanana kadar analizde görünmez' : ''}.
            Girdiğiniz {acikUclu ? 'soru ayarları' : 'cevap anahtarı'} korunur.
          </Uyari>
        </div>
      )}
      {yukle.isError && <div className="mt-4"><Uyari tur="hata">{hataMesaji(yukle.error)}</Uyari></div>}
      {yukle.data && yeniUyarilar(yukle.data).length > 0 && (
        <div className="mt-4 space-y-2">
          <Uyari tur="basari">Dosyalar kaydedildi. Lütfen aşağıdaki uyarıları kontrol edin.</Uyari>
          {yeniUyarilar(yukle.data).map((u) => <Uyari key={u} tur="uyari">{u}</Uyari>)}
        </div>
      )}

      <div className="mt-6 flex justify-end">
        {yukle.data && yeniUyarilar(yukle.data).length > 0 ? (
          <button className="btn-birincil" onClick={ileri}>Uyarıları gördüm, devam et <ChevronRight className="h-4 w-4" /></button>
        ) : (
          <button
            className="btn-birincil"
            disabled={!devamEdilebilir || yukle.isPending}
            onClick={() => (oncekiKullan ? ileri() : yukle.mutate())}
          >
            {yukle.isPending ? <><Loader2 className="h-4 w-4 animate-spin" /> Yükleniyor…</> : <>İleri <ChevronRight className="h-4 w-4" /></>}
          </button>
        )}
      </div>
    </div>
  )
}

function DosyaKarti({ ad, boyut, Ikon, yukleniyor, ek, kaldir }: {
  ad: string; boyut: string; Ikon: typeof FileText; yukleniyor?: boolean; ek?: string; kaldir?: () => void
}) {
  return (
    <div className="mt-3 flex items-center gap-3 rounded-lg border border-emerald-200 bg-white px-3 py-2.5">
      <Ikon className="h-5 w-5 shrink-0 text-emerald-600" />
      <div className="min-w-0 flex-1">
        <div className="truncate text-sm font-semibold text-emerald-800">{ad}</div>
        <div className="text-xs text-selcuk-metin-soluk">{boyut}{ek && ` · ${ek}`}</div>
      </div>
      {yukleniyor && <Loader2 className="h-4 w-4 animate-spin text-selcuk-mavi" aria-label="Okunuyor" />}
      {kaldir && (
        <button onClick={kaldir} className="rounded p-1 text-red-600 hover:bg-red-50" aria-label={`${ad} dosyasını kaldır`}>
          <X className="h-4 w-4" />
        </button>
      )}
    </div>
  )
}

function Ozet({ etiket, deger }: { etiket: string; deger: string | number }) {
  return (
    <div>
      <div className="text-xs text-selcuk-metin-soluk">{etiket}</div>
      <div className="font-bold text-selcuk-lacivert">{deger}</div>
    </div>
  )
}
