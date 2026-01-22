import type { Camper, Location, Itinerary, TravelPreferences } from './types';

const defaultApiBase = import.meta.env.DEV
    ? 'http://localhost:5080/api'
    : 'https://wa-bt-hgged7edc8gph9gz.italynorth-01.azurewebsites.net/api';

const API_BASE = (import.meta.env.VITE_API_BASE ?? defaultApiBase).replace(/\/$/, '');

export async function getCampers(): Promise<Camper[]> {
    const response = await fetch(`${API_BASE}/campers`);
    if (!response.ok) throw new Error('Failed to fetch campers');
    const data = await response.json() as Array<{
        id: string;
        modelName: string;
        category: Camper['category'];
        notes: string;
        sleeps: number;
        lengthMeters: number;
    }>;
    return data.map((c) => ({
        id: c.id,
        name: c.modelName,
        category: c.category,
        description: c.notes,
        capacity: c.sleeps,
        length: c.lengthMeters,
        isManualTransmission: false
    }));
}

export async function getLocations(): Promise<Location[]> {
    const response = await fetch(`${API_BASE}/locations`);
    if (!response.ok) throw new Error('Failed to fetch locations');
    return response.json();
}

export async function suggestItinerary(preferences: TravelPreferences): Promise<Itinerary> {
    const response = await fetch(`${API_BASE}/itineraries/suggest`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(preferences),
    });
    if (!response.ok) throw new Error('Failed to suggest itinerary');
    return response.json();
}

export function getPdfUrl(itineraryId: string, mode: 'detailed' | 'brochure' = 'detailed'): string {
    return `${API_BASE}/itineraries/${itineraryId}/pdf?mode=${mode}`;
}
