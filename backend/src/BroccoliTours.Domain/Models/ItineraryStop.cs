namespace BroccoliTours.Domain.Models;

public sealed record ItineraryStop(
    string Name,
    string? Description,
    double Latitude,
    double Longitude,
    string Type
);
