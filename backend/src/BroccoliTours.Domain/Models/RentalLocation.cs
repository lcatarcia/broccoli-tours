namespace BroccoliTours.Domain.Models;

public sealed record RentalLocation(
    string Id,
    string Name,
    string City,
    string Country,
    double Latitude,
    double Longitude,
    string Address
);
