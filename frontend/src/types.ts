export interface Camper {
    id: string;
    name: string;
    category: 'Van' | 'CompactVan' | 'Campervan' | 'Motorhome' | 'IntegratedMotorhome';
    description: string;
    capacity: number;
    length: number;
    isManualTransmission: boolean;
}

export interface Location {
    id: string;
    name: string;
    region?: string;
    country: string;
    description: string;
    latitude: number;
    longitude: number;
}

export interface RentalLocation {
    id: string;
    name: string;
    city: string;
    country: string;
    latitude: number;
    longitude: number;
    address: string;
}

export interface ItineraryStop {
    name: string;
    description: string;
    latitude: number;
    longitude: number;
    type: string;
}

export interface ItineraryDay {
    dayNumber: number;
    date?: string;
    title: string;
    stops: ItineraryStop[];
    activities: string[];
    driveHoursEstimate?: number;
    overnightStopRecommendation?: string;
}

export interface Itinerary {
    id: string;
    title: string;
    summary: string;
    period: string;
    days: ItineraryDay[];
    tips: string[];
    generatedAt: string;
}

export interface TravelPreferences {
    locationId?: string;
    locationQuery?: string;
    camperModelName: string;
    partySize: number;
    startDate?: string;
    endDate?: string;
    suggestedMonth?: string;
    tripDurationDays?: number;
    avoidOvertourism: boolean;
    minDailyDriveHours?: number;
    maxDailyDriveHours?: number;
    isOwnedCamper: boolean;
    ownedCamperModel?: string;
    rentalLocationId?: string;
}
