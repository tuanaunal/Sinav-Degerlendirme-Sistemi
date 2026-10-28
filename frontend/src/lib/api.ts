import axios from 'axios'

/** Tüm backend çağrıları bu istemci üzerinden yapılır (Vite proxy ile /api → :5080). */
export const api = axios.create({
  baseURL: '/api',
})
