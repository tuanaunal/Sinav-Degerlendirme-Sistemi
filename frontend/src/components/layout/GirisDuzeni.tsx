import SelcukLogo from '../SelcukLogo'

/**
 * Giriş, şifremi unuttum ve şifre değiştirme ekranlarının ortak düzeni:
 * solda lacivert kurumsal panel, sağda form kartı. Telefonda alt alta dizilir.
 */
export default function GirisDuzeni({
  baslik,
  aciklama,
  children,
}: {
  baslik: string
  aciklama?: string
  children: React.ReactNode
}) {
  return (
    <div className="flex min-h-screen flex-col bg-selcuk-zemin lg:flex-row">
      <aside className="relative flex flex-col items-center justify-center overflow-hidden bg-selcuk-lacivert px-6 py-10 text-center text-white lg:w-[44%] lg:py-0">
        <div
          aria-hidden
          className="absolute -right-24 -top-24 h-72 w-72 rounded-full border-[40px] border-white/5 lg:h-[28rem] lg:w-[28rem]"
        />
        <div aria-hidden className="absolute -bottom-20 -left-20 h-60 w-60 rounded-full bg-selcuk-mavi/40" />
        <div className="relative flex flex-col items-center">
          <SelcukLogo boyut={112} />
          <div className="mt-6 text-sm font-semibold uppercase tracking-[0.2em] text-selcuk-altin">
            Selçuk Üniversitesi
          </div>
          <h1 className="mt-2 text-2xl font-bold sm:text-3xl">Sınav Değerlendirme Sistemi</h1>
          <p className="mt-3 hidden max-w-sm text-sm leading-relaxed text-white/70 lg:block">
            Ölçme ve değerlendirme işlemlerinin yönetilmesini ve takibini kolaylaştıran platform.
          </p>
          <div className="mt-8 hidden gap-6 text-left text-xs text-white/70 lg:flex">
            <div>
              <div className="text-2xl font-bold text-white">5</div>
              adımda sınav girişi
            </div>
            <div>
              <div className="text-2xl font-bold text-white">3</div>
              sınav türü
            </div>
            <div>
              <div className="text-2xl font-bold text-white">2</div>
              değerlendirme şekli
            </div>
          </div>
        </div>
      </aside>

      <main className="flex flex-1 items-center justify-center px-4 py-10 sm:px-6">
        <div className="w-full max-w-md">
          <div className="kart p-6 sm:p-8">
            <h2 className="text-xl font-bold text-selcuk-lacivert">{baslik}</h2>
            {aciklama && <p className="mt-1 text-sm text-selcuk-metin-soluk">{aciklama}</p>}
            <div className="mt-6">{children}</div>
          </div>
          <p className="mt-6 text-center text-xs text-selcuk-metin-soluk">
            © {new Date().getFullYear()} Selçuk Üniversitesi · Teknoloji Fakültesi
          </p>
        </div>
      </main>
    </div>
  )
}
