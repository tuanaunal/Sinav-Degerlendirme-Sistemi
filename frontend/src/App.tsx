import { Navigate, Route, Routes } from 'react-router-dom'
import Karsilama from './pages/Karsilama'

export default function App() {
  return (
    <Routes>
      <Route path="/" element={<Karsilama />} />
      <Route path="*" element={<Navigate to="/" replace />} />
    </Routes>
  )
}
