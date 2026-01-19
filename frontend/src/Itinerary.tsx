import { useLocation, useNavigate } from 'react-router-dom';
import { MapContainer, TileLayer, Marker, Popup, Polyline } from 'react-leaflet';
import type { Itinerary as ItineraryType } from './types';
import { getPdfUrl } from './api';
import 'leaflet/dist/leaflet.css';
import './Itinerary.css';

export default function Itinerary() {
    const location = useLocation();
    const navigate = useNavigate();
    const itinerary = location.state?.itinerary as ItineraryType;

    if (!itinerary) {
        return (
            <div className="itinerary-error">
                <p>Nessun itinerario trovato</p>
                <button onClick={() => navigate('/')}>Torna alla home</button>
            </div>
        );
    }

    const allStops = itinerary.days.flatMap(day => day.stops);
    const center = allStops.length > 0
        ? { lat: allStops[0].latitude, lng: allStops[0].longitude }
        : { lat: 45.4642, lng: 9.1900 };

    const pathCoordinates = allStops.map(stop => [stop.latitude, stop.longitude] as [number, number]);

    return (
        <div className="itinerary-page">
            <header className="itinerary-header">
                <button onClick={() => navigate('/')} className="btn-back">← Torna alla ricerca</button>
                <img src="http://localhost:5080/images/ChatGPT%20Image%20Jan%2012,%202026,%2011_45_43%20AM.png" alt="Broccoli Tours" className="itinerary-logo" />
                <h1>{itinerary.title}</h1>
                <p className="summary">{itinerary.summary}</p>
                <div className="actions">
                    <a href={getPdfUrl(itinerary.id, 'detailed')} target="_blank" className="btn-pdf">📄 PDF Dettagliato</a>
                    <a href={getPdfUrl(itinerary.id, 'brochure')} target="_blank" className="btn-pdf">📋 Brochure</a>
                </div>
            </header>

            <div className="itinerary-content">
                <div className="days-list">
                    {itinerary.days.map(day => (
                        <div key={day.dayNumber} className="day-card">
                            <h3>Giorno {day.dayNumber}: {day.title}</h3>
                            {day.date && <p className="date">📅 {new Date(day.date).toLocaleDateString('it-IT')}</p>}
                            {day.driveHoursEstimate !== undefined && day.driveHoursEstimate > 0 && (
                                <p className="drive-hours">🚗 Guida stimata: {day.driveHoursEstimate.toFixed(1)} ore</p>
                            )}

                            <div className="stops">
                                {day.stops.map((stop, i) => (
                                    <div key={i} className="stop">
                                        <h4>{stop.name}</h4>
                                        <span className="stop-type">{stop.type}</span>
                                        <p>{stop.description}</p>
                                    </div>
                                ))}
                            </div>

                            {day.activities.length > 0 && (
                                <div className="activities">
                                    <strong>Attività:</strong>
                                    <ul>
                                        {day.activities.map((act, i) => <li key={i}>{act}</li>)}
                                    </ul>
                                </div>
                            )}

                            {day.overnightStopRecommendation && (
                                <div className="overnight">
                                    <strong>🌙 Sosta notturna consigliata:</strong> {day.overnightStopRecommendation}
                                </div>
                            )}
                        </div>
                    ))}
                </div>

                <div className="map-section">
                    <h3>Mappa del percorso</h3>
                    <MapContainer center={center} zoom={8} style={{ height: '500px', width: '100%' }}>
                        <TileLayer
                            attribution='&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a>'
                            url="https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png"
                        />
                        {allStops.map((stop, i) => (
                            <Marker key={i} position={[stop.latitude, stop.longitude]}>
                                <Popup>{stop.name}</Popup>
                            </Marker>
                        ))}
                        {pathCoordinates.length > 1 && <Polyline positions={pathCoordinates} color="green" />}
                    </MapContainer>
                </div>

                {itinerary.tips.length > 0 && (
                    <div className="tips-section">
                        <h3>💡 Consigli del tour operator</h3>
                        <ul>
                            {itinerary.tips.map((tip, i) => <li key={i}>{tip}</li>)}
                        </ul>
                    </div>
                )}
            </div>
        </div>
    );
}
