import { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { getCampers, getLocations, suggestItinerary } from './api';
import type { Camper, Location } from './types';
import './Home.css';

export default function Home() {
    const navigate = useNavigate();
    const [campers, setCampers] = useState<Camper[]>([]);
    const [locations, setLocations] = useState<Location[]>([]);
    const [loading, setLoading] = useState(false);
    const [error, setError] = useState('');

    const [selectedCamper, setSelectedCamper] = useState('');
    const [selectedLocation, setSelectedLocation] = useState('');
    const [customDestination, setCustomDestination] = useState('');
    const [useCustomDestination, setUseCustomDestination] = useState(false);
    const [partySize, setPartySize] = useState(2);
    const [dateMode, setDateMode] = useState<'specific' | 'month'>('specific');
    const [startDate, setStartDate] = useState('');
    const [endDate, setEndDate] = useState('');
    const [selectedMonth, setSelectedMonth] = useState('');
    const [monthTripLength, setMonthTripLength] = useState(7);
    const [avoidOvertourism, setAvoidOvertourism] = useState(true);
    const [minDriveHours, setMinDriveHours] = useState(2);
    const [maxDriveHours, setMaxDriveHours] = useState(4);

    useEffect(() => {
        Promise.all([getCampers(), getLocations()])
            .then(([c, l]) => {
                setCampers(c);
                setLocations(l);
            })
            .catch(() => setError('Errore caricamento dati'));
    }, []);

    const handleSubmit = async (e: React.FormEvent) => {
        e.preventDefault();
        setError('');
        setLoading(true);

        try {
            const itinerary = await suggestItinerary({
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
            navigate('/itinerary', { state: { itinerary } });
        } catch (err) {
            setError('Errore nella generazione dell\'itinerario');
        } finally {
            setLoading(false);
        }
    };

    return (
        <div className="home">
            <header className="hero">
                <img src="/broccoli-logo.png" alt="Broccoli Tours Logo" className="hero-logo" />
                <h1>Broccoli Tours</h1>
                <p>Il tuo tour operator specializzato in viaggi in camper</p>
            </header>

            <form onSubmit={handleSubmit} className="booking-form">
                <h2>Progetta il tuo viaggio</h2>

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

                <button type="submit" disabled={loading} className="btn-primary">
                    {loading ? 'Generazione in corso...' : 'Genera Itinerario AI'}
                </button>
            </form>
        </div>
    );
}
