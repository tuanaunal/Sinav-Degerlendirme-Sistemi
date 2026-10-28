import { useQuery } from '@tanstack/react-query'
import { CheckCircle2, XCircle, Loader2, Server, Palette, Database } from 'lucide-react'
import SayfaIskeleti from '../components/layout/SayfaIskeleti'
import { api } from '../lib/api'

interface SaglikYaniti {
  durum: string
  sistem: string
  kurum: string
  zaman: string
}

/** Aşama 0 karşılama ekranı: kurumsal temayı ve backend bağlantısını doğrular. */
export default function Karsilama() {
  const saglik = useQuery({
    queryKey: ['saglik'],
    queryFn: async () => (await api.get<SaglikYaniti>('/saglik')).data,
    retry: false,
  })

  return (
    <SayfaIskeleti breadcrumb={[{ etiket: 'Sınav Değerlendirme Sistemi' }]}>
      <h1 className="text-2xl font-bold text-selcuk-lacivert">Sınav Değerlendirme Sistemi</h1>
      <p className="mt-1 max-w-3xl text-sm text-selcuk-metin-soluk">
        Ölçme ve değerlendirme işlemlerinin yönetilmesini ve takibini kolaylaştıran platform.
      </p>

      <div className="mt-6 grid gap-4 sm:grid-cols-3">
        <div className="kart p-5">
          <div className="flex items-center gap-3">
            <span className="flex h-10 w-10 items-center justify-center rounded-lg bg-selcuk-mavi-acik text-selcuk-mavi">
              <Server className="h-5 w-5" />
            </span>
            <div className="font-semibold">Sunucu (API)</div>
          </div>
          <div className="mt-4 flex items-center gap-2 text-sm">
            {saglik.isLoading && (
              <>
                <Loader2 className="h-4 w-4 animate-spin text-selcuk-mavi" /> Kontrol ediliyor…
              </>
            )}
            {saglik.isSuccess && (
              <>
                <CheckCircle2 className="h-4 w-4 text-emerald-600" /> Bağlantı başarılı
              </>
            )}
            {saglik.isError && (
              <>
                <XCircle className="h-4 w-4 text-red-600" /> Sunucuya ulaşılamadı
              </>
            )}
          </div>
        </div>

        <div className="kart p-5">
          <div className="flex items-center gap-3">
            <span className="flex h-10 w-10 items-center justify-center rounded-lg bg-amber-50 text-selcuk-altin-koyu">
              <Palette className="h-5 w-5" />
            </span>
            <div className="font-semibold">Kurumsal tema</div>
          </div>
          <div className="mt-4 flex gap-2">
            {['bg-selcuk-lacivert', 'bg-selcuk-mavi', 'bg-selcuk-altin', 'bg-selcuk-zemin'].map((r) => (
              <span key={r} className={`h-7 w-7 rounded-full border border-slate-200 ${r}`} />
            ))}
          </div>
        </div>

        <div className="kart p-5">
          <div className="flex items-center gap-3">
            <span className="flex h-10 w-10 items-center justify-center rounded-lg bg-slate-100 text-slate-600">
              <Database className="h-5 w-5" />
            </span>
            <div className="font-semibold">Veritabanı</div>
          </div>
          <p className="mt-4 text-sm text-selcuk-metin-soluk">PostgreSQL · Aşama 1'de bağlanacak</p>
        </div>
      </div>

      <div className="mt-6 flex flex-wrap gap-3">
        <button className="btn-birincil">Birincil buton</button>
        <button className="btn-mavi">Mavi buton</button>
        <button className="btn-ikincil">İkincil buton</button>
      </div>
    </SayfaIskeleti>
  )
}
