using BroccoliTours.Domain.Enums;

namespace BroccoliTours.Domain.Models;

public sealed record TravelPeriod(
    TravelPeriodType Type,
    DateOnly? StartDate,
    DateOnly? EndDate,
    int? Month,
    int? Year
);
