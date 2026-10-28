/** @type {import('tailwindcss').Config} */
// Renkler Selçuk Üniversitesi kurumsal kimliğinden (selcuk.edu.tr) alınmıştır.
export default {
  content: ['./index.html', './src/**/*.{ts,tsx}'],
  theme: {
    extend: {
      colors: {
        selcuk: {
          lacivert: '#0D2D5E', // üst bar, başlıklar
          mavi: '#1C458B', // ana kurumsal mavi
          'mavi-acik': '#E8EEF8',
          altin: '#FFC90D', // amblemdeki altın, birincil butonlar
          'altin-koyu': '#E6B400',
          zemin: '#F6F8FC',
          metin: '#1E293B',
          'metin-soluk': '#64748B',
          cizgi: '#CBD5E1',
        },
      },
      fontFamily: {
        sans: ['Montserrat', 'system-ui', 'sans-serif'],
      },
      boxShadow: {
        kart: '0 1px 3px rgba(13, 45, 94, 0.08), 0 1px 2px rgba(13, 45, 94, 0.04)',
      },
    },
  },
  plugins: [],
}
