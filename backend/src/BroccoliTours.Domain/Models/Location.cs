namespace BroccoliTours.Domain.Models;

public sealed record Location(
    string Id,
    string Name,
    string CountryCode,
    string? Region,
    double Latitude,
    double Longitude,
    string? Description
);
