using BroccoliTours.Domain.Enums;

namespace BroccoliTours.Domain.Models;

public sealed record TravelPreferences(
    TravelPeriodType PeriodType,
    DateOnly? StartDate,
    DateOnly? EndDate,
    int? Month,
    int? Year,
    bool SuggestBestPeriod,
    string? LocationId,
    string? LocationQuery,
    CamperCategory? CamperCategory,
    string? CamperModelName,
    int PartySize,
    int? TripDurationDays,
    bool WeekendTrip,
    bool AvoidOvertourism,
    double? MinDailyDriveHours,
    double? MaxDailyDriveHours
);
