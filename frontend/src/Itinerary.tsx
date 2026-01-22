import { useState } from 'react';
import { useLocation, useNavigate } from 'react-router-dom';
import { MapContainer, TileLayer, Marker, Popup, Polyline } from 'react-leaflet';
import L from 'leaflet';
import type { Itinerary as ItineraryType, Camper, Location } from './types';
import { getPdfUrl, suggestItinerary, getCampers, getLocations } from './api';
import 'leaflet/dist/leaflet.css';
import './Itinerary.css';
import './Home.css';
import './Modal.css';

// Fix Leaflet default icon paths for production
delete (L.Icon.Default.prototype as any)._getIconUrl;
L.Icon.Default.mergeOptions({
    iconRetinaUrl: 'https://unpkg.com/leaflet@1.9.4/dist/images/marker-icon-2x.png',
    iconUrl: 'https://unpkg.com/leaflet@1.9.4/dist/images/marker-icon.png',
    shadowUrl: 'https://unpkg.com/leaflet@1.9.4/dist/images/marker-shadow.png',
});

function ErrorModal({ message, onClose }: { message: string; onClose: () => void }) {
    return (
        <div className="modal-overlay" onClick={onClose}>
            <div className="modal-content" onClick={(e) => e.stopPropagation()}>
                <h3>⚠️ Errore nella generazione</h3>
                <p>{message}</p>
                <p className="modal-hint">
                    💡 <strong>Suggerimento:</strong> Prova a semplificare la richiesta:
                </p>
                <ul className="modal-tips">
                    <li>Riduci il numero di giorni del viaggio</li>
                    <li>Scegli una destinazione più specifica</li>
                    <li>Evita periodi troppo lunghi o generici</li>
                </ul>
                <button onClick={onClose} className="btn-modal-close">Chiudi e riprova</button>
            </div>
        </div>
    );
}

export default function Itinerary() {
    const location = useLocation();
    const navigate = useNavigate();
    const [currentItinerary, setCurrentItinerary] = useState<ItineraryType>(location.state?.itinerary);
    const savedPrefs = location.state?.preferences;

    const [campers, setCampers] = useState<Camper[]>([]);
    const [locations, setLocations] = useState<Location[]>([]);
    const [loading, setLoading] = useState(false);
    const [error, setError] = useState('');
    const [showErrorModal, setShowErrorModal] = useState(false);
    const [showForm, setShowForm] = useState(false);

    const [selectedCamper, setSelectedCamper] = useState(savedPrefs?.selectedCamper || '');
    const [selectedLocation, setSelectedLocation] = useState(savedPrefs?.selectedLocation || '');
    const [customDestination, setCustomDestination] = useState(savedPrefs?.customDestination || '');
    const [useCustomDestination, setUseCustomDestination] = useState(savedPrefs?.useCustomDestination || false);
    const [partySize, setPartySize] = useState(savedPrefs?.partySize || 2);
    const [dateMode, setDateMode] = useState<'specific' | 'month'>(savedPrefs?.dateMode || 'specific');
    const [startDate, setStartDate] = useState(savedPrefs?.startDate || '');
    const [endDate, setEndDate] = useState(savedPrefs?.endDate || '');
    const [selectedMonth, setSelectedMonth] = useState(savedPrefs?.selectedMonth || '');
    const [monthTripLength, setMonthTripLength] = useState(savedPrefs?.monthTripLength || 7);
    const [avoidOvertourism, setAvoidOvertourism] = useState(savedPrefs?.avoidOvertourism ?? true);
    const [minDriveHours, setMinDriveHours] = useState(savedPrefs?.minDriveHours || 2);
    const [maxDriveHours, setMaxDriveHours] = useState(savedPrefs?.maxDriveHours || 4);

    const handleRegenerate = async (e: React.FormEvent) => {
        e.preventDefault();
        setError('');
        setLoading(true);

        try {
            const newItinerary = await suggestItinerary({
                locationId: useCustomDestination ? undefined : selectedLocation,
                locationQuery: useCustomDestination ? customDestination : undefined,
                camperModelName: selectedCamper,
                partySize,
                startDate: dateMode === 'specific' ? (startDate || undefined) : undefined,
                endDate: dateMode === 'specific' ? (endDate || undefined) : undefined,
                suggestedMonth: dateMode === 'month' ? selectedMonth : undefined,
                tripDurationDays: dateMode === 'month' ? monthTripLength : undefined,
                avoidOvertourism,
                minDailyDriveHours: minDriveHours,
                maxDailyDriveHours: maxDriveHours,
            });
            setCurrentItinerary(newItinerary);
            setShowForm(false);
            window.scrollTo(0, 0);
        } catch (err) {
            console.error('Itinerary generation error:', err);
            const errorMessage = err instanceof Error && err.message === 'FALLBACK_USED'
                ? 'La richiesta è troppo complessa per l\'intelligenza artificiale. L\'API Gemini non è riuscita a elaborarla correttamente.'
                : 'Errore nella generazione dell\'itinerario. Riprova tra qualche istante.';
            setError(errorMessage);
            setShowErrorModal(true);
        } finally {
            setLoading(false);
        }
    };

    const loadFormData = async () => {
        if (campers.length === 0) {
            const [c, l] = await Promise.all([getCampers(), getLocations()]);
            setCampers(c);
            setLocations(l);
        }
        setShowForm(!showForm);
    };

    const itinerary = currentItinerary;

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
            {showErrorModal && (
                <ErrorModal
                    message={error}
                    onClose={() => {
                        setShowErrorModal(false);
                        setError('');
                    }}
                />
            )}
            <header className="itinerary-header">
                <button onClick={() => navigate('/')} className="btn-back">← Torna alla ricerca</button>
                <img src="/broccoli-logo.png" alt="Broccoli Tours" className="itinerary-logo" />
                <h1>{itinerary.title}</h1>
                <p className="summary">{itinerary.summary}</p>
                <div className="actions">
                    <button onClick={loadFormData} className="btn-pdf">✏️ Modifica Richiesta</button>
                    <button onClick={() => window.open(getPdfUrl(itinerary.id, 'detailed'), '_blank')} className="btn-pdf">📄 PDF Dettagliato</button>
                    <button onClick={() => window.open(getPdfUrl(itinerary.id, 'brochure'), '_blank')} className="btn-pdf">📋 Brochure</button>
                </div>
            </header>

            {showForm && (
                <form onSubmit={handleRegenerate} className="booking-form" style={{marginBottom: '2rem'}}>
                    <h2>Modifica la tua richiesta</h2>

                    <div className="form-group">
                        <label>Destinazione</label>
                        <div className="destination-toggle">
                            <button type="button" className={!useCustomDestination ? 'active' : ''} onClick={() => setUseCustomDestination(false)}>Seleziona</button>
                            <button type="button" className={useCustomDestination ? 'active' : ''} onClick={() => setUseCustomDestination(true)}>Input Libero</button>
                        </div>
                        {!useCustomDestination ? (
                            <select value={selectedLocation} onChange={e => setSelectedLocation(e.target.value)} required>
                                <option value="">Seleziona una destinazione</option>
                                {locations.map(loc => (
                                    <option key={loc.id} value={loc.id}>
                                        {loc.name} ({loc.country})
                                    </option>
                                ))}
                            </select>
                        ) : (
                            <input
                                type="text"
                                value={customDestination}
                                onChange={e => setCustomDestination(e.target.value)}
                                placeholder="Es: Costa Azzurra, Dolomiti, Alpi Svizzere..."
                                required
                            />
                        )}
                    </div>

                    <div className="form-group">
                        <label>Camper</label>
                        <select value={selectedCamper} onChange={e => setSelectedCamper(e.target.value)} required>
                            <option value="">Seleziona un camper</option>
                            {campers.map(c => (
                                <option key={c.id} value={c.name}>
                                    {c.name} - {c.description}
                                </option>
                            ))}
                        </select>
                    </div>

                    <div className="form-group">
                        <label>Numero persone</label>
                        <input type="number" min={1} max={6} value={partySize} onChange={e => setPartySize(+e.target.value)} required />
                    </div>

                    <div className="form-group">
                        <label>Periodo di viaggio</label>
                        <div className="date-mode-toggle">
                            <button type="button" className={dateMode === 'specific' ? 'active' : ''} onClick={() => setDateMode('specific')}>Date Specifiche</button>
                            <button type="button" className={dateMode === 'month' ? 'active' : ''} onClick={() => setDateMode('month')}>Mese Intero</button>
                        </div>
                        {dateMode === 'specific' ? (
                            <div className="form-row">
                                <div className="form-group">
                                    <label>Data partenza (opzionale)</label>
                                    <input type="date" value={startDate} onChange={e => setStartDate(e.target.value)} />
                                </div>
                                <div className="form-group">
                                    <label>Data rientro (opzionale)</label>
                                    <input type="date" value={endDate} onChange={e => setEndDate(e.target.value)} />
                                </div>
                            </div>
                        ) : (
                            <div className="form-row">
                                <div className="form-group">
                                    <label>Mese preferito (opzionale)</label>
                                    <select value={selectedMonth} onChange={e => setSelectedMonth(e.target.value)}>
                                        <option value="">Qualsiasi mese</option>
                                        <option value="gennaio">Gennaio</option>
                                        <option value="febbraio">Febbraio</option>
                                        <option value="marzo">Marzo</option>
                                        <option value="aprile">Aprile</option>
                                        <option value="maggio">Maggio</option>
                                        <option value="giugno">Giugno</option>
                                        <option value="luglio">Luglio</option>
                                        <option value="agosto">Agosto</option>
                                        <option value="settembre">Settembre</option>
                                        <option value="ottobre">Ottobre</option>
                                        <option value="novembre">Novembre</option>
                                        <option value="dicembre">Dicembre</option>
                                    </select>
                                </div>
                                <div className="form-group">
                                    <label>Durata viaggio (giorni)</label>
                                    <input
                                        type="number"
                                        min={2}
                                        max={30}
                                        value={monthTripLength}
                                        onChange={e => {
                                            const numeric = Number(e.target.value);
                                            if (Number.isNaN(numeric)) {
                                                setMonthTripLength(2);
                                                return;
                                            }
                                            const clamped = Math.min(30, Math.max(2, numeric));
                                            setMonthTripLength(clamped);
                                        }}
                                    />
                                </div>
                            </div>
                        )}
                    </div>

                    <div className="form-group">
                        <label>Ore di guida giornaliere</label>
                        <div className="drive-hours-selector">
                            <div className="form-group">
                                <label>Minimo: {minDriveHours} ore</label>
                                <input
                                    type="range"
                                    min="1"
                                    max="8"
                                    step="0.5"
                                    value={minDriveHours}
                                    onChange={e => {
                                        const val = parseFloat(e.target.value);
                                        setMinDriveHours(val);
                                        if (val > maxDriveHours) setMaxDriveHours(val);
                                    }}
                                />
                            </div>
                            <div className="form-group">
                                <label>Massimo: {maxDriveHours} ore</label>
                                <input
                                    type="range"
                                    min="1"
                                    max="8"
                                    step="0.5"
                                    value={maxDriveHours}
                                    onChange={e => {
                                        const val = parseFloat(e.target.value);
                                        setMaxDriveHours(val);
                                        if (val < minDriveHours) setMinDriveHours(val);
                                    }}
                                />
                            </div>
                        </div>
                    </div>

                    <div className="form-group checkbox">
                        <label>
                            <input type="checkbox" checked={avoidOvertourism} onChange={e => setAvoidOvertourism(e.target.checked)} />
                            Evita mete sovraffollate (anti-overtourism)
                        </label>
                    </div>

                    {error && <div className="error">{error}</div>}

                    <div style={{display: 'flex', gap: '1rem'}}>
                        <button type="submit" disabled={loading} className="btn-primary" style={{flex: 1}}>
                            {loading ? 'Rigenerazione in corso...' : 'Rigenera Itinerario'}
                        </button>
                        <button type="button" onClick={() => setShowForm(false)} className="btn-primary" style={{flex: 0, background: '#5a4a3a'}}>
                            Annulla
                        </button>
                    </div>
                </form>
            )}

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
                                        <span className="stop-type">
                                            {stop.type === 'attraction' && '🎭'}
                                            {stop.type === 'village' && '🏘️'}
                                            {stop.type === 'camper_area' && '🏕️'}
                                            {stop.type === 'viewpoint' && '👁️'}
                                            {stop.type === 'food' && '🍽️'}
                                            {stop.type === 'restaurant' && '🍴'}
                                            {stop.type === 'cafe' && '☕'}
                                            {stop.type === 'museum' && '🏛️'}
                                            {stop.type === 'park' && '🌳'}
                                            {stop.type === 'beach' && '🏖️'}
                                            {stop.type === 'mountain' && '⛰️'}
                                            {stop.type === 'lake' && '🏞️'}
                                            {stop.type === 'shopping' && '🛍️'}
                                            {stop.type === 'nightlife' && '🌃'}
                                            {!['attraction', 'village', 'camper_area', 'viewpoint', 'food', 'restaurant', 'cafe', 'museum', 'park', 'beach', 'mountain', 'lake', 'shopping', 'nightlife'].includes(stop.type) && stop.type}
                                        </span>
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
