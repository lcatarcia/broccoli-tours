namespace BroccoliTours.Domain.Models;

public sealed record ItineraryDay(
    int DayNumber,
    DateOnly? Date,
    string Title,
    IReadOnlyList<ItineraryStop> Stops,
    IReadOnlyList<string> Activities,
    double? DriveHoursEstimate,
    string? OvernightStopRecommendation
);
