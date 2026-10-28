import { useEffect, useState } from 'react'
import { Link, useParams, useSearchParams } from 'react-router-dom'
import { useQuery } from '@tanstack/react-query'
import { ChevronLeft, Construction, Loader2 } from 'lucide-react'
import Sayfa from '../../../components/layout/Sayfa'
import { Uyari } from '../../../components/form'
import { hataMesaji } from '../../../lib/api'
import { dersSorgulari, sinavTuruAdi, yoldanSinavTuru } from '../../../lib/dersler'
import { sinavSorgusu } from '../../../lib/sinav'
import AdimGostergesi from './AdimGostergesi'
import Adim1DosyaYukleme from './Adim1DosyaYukleme'
import Adim2CevapAnahtari from './Adim2CevapAnahtari'
import Adim3VeriEslestirme from './Adim3VeriEslestirme'
import Adim4NotDuzenleme from './Adim4NotDuzenleme'
import Adim5OgrenmeCiktilari from './Adim5OgrenmeCiktilari'

const ADIMLAR = {
  CoktanSecmeli: ['Dosya Yükleme', 'Cevap Anahtarı', 'Veri Eşleştirme', 'Not Düzenleme', 'Öğrenme Çıktıları'],
  AcikUclu: ['Sınıf Listesi', 'Soru Sayısı', 'Not Yükleme', 'Not Düzenleme', 'Öğrenme Çıktıları'],
}

/**
 * 5 adımlı sınav girişi sihirbazı. Bir adım tamamlanmadan sonrakine geçilemez;
 * tamamlanmış adımlara üstteki göstergeden geri dönülebilir.
 */
export default function SinavGirisi() {
  const { dersId: dersParam, tur: turParam } = useParams()
  const [arama] = useSearchParams()
  const dersId = Number(dersParam)
  const tur = yoldanSinavTuru(turParam)

  const ders = useQuery(dersSorgulari.detay(dersId))
  const sinav = useQuery({ ...sinavSorgusu(dersId, tur ?? 'vize'), enabled: !!tur })
  const [adim, setAdim] = useState<number | null>(null)

  const format = sinav.data?.format ?? (arama.get('format') === 'acik-uclu' ? 'AcikUclu' : 'CoktanSecmeli')
  const tamamlanan = sinav.data?.tamamlananAdim ?? 0

  // İlk açılışta kalınan yerden devam et
  useEffect(() => {
    if (adim === null && sinav.isFetched) setAdim(Math.min(tamamlanan + 1, 5))
  }, [adim, sinav.isFetched, tamamlanan])

  const breadcrumb = [
    { etiket: 'Sınav Değerlendirme Sistemi', yol: '/panom' },
    { etiket: ders.data?.ad ?? 'Ders', yol: `/dersler/${dersId}` },
    { etiket: tur ? sinavTuruAdi[tur] : 'Sınav' },
    { etiket: sinav.data ? 'Sınav Girişi' : format === 'AcikUclu' ? 'Yeni Açık Uçlu Sınav Girişi' : 'Yeni Sınav Girişi' },
  ]

  if (!tur) return <Sayfa breadcrumb={breadcrumb}><Uyari tur="hata">Geçersiz sınav türü.</Uyari></Sayfa>
  if (ders.isError || sinav.isError) {
    return (
      <Sayfa breadcrumb={breadcrumb}>
        <Uyari tur="hata">{hataMesaji(ders.error ?? sinav.error)}</Uyari>
        <Link to="/panom" className="btn-ikincil mt-4">Derslerime dön</Link>
      </Sayfa>
    )
  }
  if (adim === null || ders.isLoading) {
    return <Sayfa breadcrumb={breadcrumb}><div className="flex justify-center p-10"><Loader2 className="h-6 w-6 animate-spin text-selcuk-mavi" /></div></Sayfa>
  }

  return (
    <Sayfa breadcrumb={breadcrumb}>
      <AdimGostergesi adimlar={ADIMLAR[format]} aktif={adim} tamamlanan={tamamlanan} sec={setAdim} />

      <div className="mt-6">
        {format === 'AcikUclu' ? (
          <YapimAsamasinda metin="Açık uçlu sınav girişi Aşama 10'da eklenecek." />
        ) : adim === 1 ? (
          <Adim1DosyaYukleme dersId={dersId} tur={tur} sinav={sinav.data ?? null} ileri={() => setAdim(2)} />
        ) : adim === 2 && sinav.data ? (
          <Adim2CevapAnahtari sinav={sinav.data} geri={() => setAdim(1)} ileri={() => setAdim(3)} />
        ) : adim === 3 && sinav.data ? (
          <Adim3VeriEslestirme sinav={sinav.data} geri={() => setAdim(2)} ileri={() => setAdim(4)} />
        ) : adim === 4 && sinav.data ? (
          <Adim4NotDuzenleme sinav={sinav.data} geri={() => setAdim(3)} ileri={() => setAdim(5)} />
        ) : adim === 5 && sinav.data ? (
          <Adim5OgrenmeCiktilari sinav={sinav.data} geri={() => setAdim(4)} />
        ) : (
          <YapimAsamasinda
            metin="Önce önceki adımları tamamlayın."
            geri={() => setAdim(adim - 1)}
          />
        )}
      </div>
    </Sayfa>
  )
}

function YapimAsamasinda({ metin, geri }: { metin: string; geri?: () => void }) {
  return (
    <div className="kart flex flex-col items-center px-6 py-14 text-center">
      <Construction className="h-10 w-10 text-selcuk-altin-koyu" />
      <p className="mt-3 max-w-md text-sm text-selcuk-metin-soluk">{metin}</p>
      {geri && <button className="btn-ikincil mt-5" onClick={geri}><ChevronLeft className="h-4 w-4" /> Geri</button>}
    </div>
  )
}
