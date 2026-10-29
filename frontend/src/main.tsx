import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import { BrowserRouter } from 'react-router-dom'
import { MutationCache, QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { AxiosError } from 'axios'
import './index.css'
import App from './App'
import { AuthProvider } from './auth/AuthContext'
import Bildirimler from './components/Bildirimler'
import { bildir } from './lib/bildirim'

declare module '@tanstack/react-query' {
  interface Register {
    /** İşlem başarılı olunca sağ üstte gösterilecek bildirim */
    mutationMeta: { bildirim?: string }
  }
}

const queryClient = new QueryClient({
  mutationCache: new MutationCache({
    onSuccess: (_veri, _degisken, _baglam, islem) => { if (islem.meta?.bildirim) bildir(islem.meta.bildirim) },
  }),
  defaultOptions: {
    queries: {
      refetchOnWindowFocus: false,
      // 4xx (yetki yok, bulunamadı) hataları tekrar denenmez; ağ/sunucu hataları bir kez denenir
      retry: (deneme, hata) => {
        const durum = hata instanceof AxiosError ? hata.response?.status : undefined
        return deneme < 1 && !(durum && durum >= 400 && durum < 500)
      },
    },
  },
})

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <QueryClientProvider client={queryClient}>
      <BrowserRouter>
        <AuthProvider>
          <App />
          <Bildirimler />
        </AuthProvider>
      </BrowserRouter>
    </QueryClientProvider>
  </StrictMode>,
)
