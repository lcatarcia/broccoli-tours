namespace BroccoliTours.Domain.Models;

public sealed record Camper(
    string Id,
    string ModelName,
    BroccoliTours.Domain.Enums.CamperCategory Category,
    int Sleeps,
    decimal LengthMeters,
    string? Notes
);
