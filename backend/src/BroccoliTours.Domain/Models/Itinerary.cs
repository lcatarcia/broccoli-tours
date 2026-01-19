namespace BroccoliTours.Domain.Models;

public sealed record Itinerary(
    string Id,
    string Title,
    string Summary,
    TravelPeriod Period,
    IReadOnlyList<ItineraryDay> Days,
    IReadOnlyList<string> Tips,
    DateTimeOffset GeneratedAtUtc
);
