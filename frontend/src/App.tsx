import { BrowserRouter, Routes, Route } from 'react-router-dom'
import Home from './Home'
import Itinerary from './Itinerary'
import './App.css'

function App() {
  return (
    <BrowserRouter>
      <Routes>
        <Route path="/" element={<Home />} />
        <Route path="/itinerary" element={<Itinerary />} />
      </Routes>
    </BrowserRouter>
  )
}

export default App
