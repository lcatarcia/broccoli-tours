using BroccoliTours.Domain.Models;
using BroccoliTours.Infrastructure.Catalog;
using BroccoliTours.Infrastructure.Itineraries;
using Xunit;

namespace BroccoliTours.Tests;

public class StubItineraryEngineTests
{
    private readonly StubItineraryEngine _engine;
    private readonly InMemoryLocationCatalog _locationCatalog = new();

    public StubItineraryEngineTests()
    {
        _engine = new StubItineraryEngine(_locationCatalog);
    }

    [Fact]
    public async Task SuggestAsync_ShouldReturnItinerary()
    {
        // Arrange
        var location = _locationCatalog.GetAll().First();
        var preferences = new TravelPreferences(
            PeriodType: Domain.Enums.TravelPeriodType.FixedDates,
            StartDate: DateOnly.FromDateTime(DateTime.Today),
            EndDate: DateOnly.FromDateTime(DateTime.Today.AddDays(3)),
            Month: null,
            Year: null,
            SuggestBestPeriod: false,
            LocationId: location.Id,
            LocationQuery: null,
            CamperCategory: null,
            CamperModelName: "Test Camper",
            PartySize: 2,
            TripDurationDays: null,
            WeekendTrip: false,
            OvertourismLevel: 3,
            MinDailyDriveHours: 2,
            MaxDailyDriveHours: 4,
            IsOwnedCamper: true,
            OwnedCamperModel: "Test Camper",
            RentalLocationId: null
        );

        // Act
        var itinerary = await _engine.SuggestAsync(preferences);

        // Assert
        Assert.NotNull(itinerary);
        Assert.NotEmpty(itinerary.Title);
        Assert.NotEmpty(itinerary.Days);
    }

    [Fact]
    public async Task SuggestAsync_ShouldIncludeDriveHoursEstimate()
    {
        // Arrange
        var location = _locationCatalog.GetAll().First();
        var preferences = new TravelPreferences(
            PeriodType: Domain.Enums.TravelPeriodType.FixedDates,
            StartDate: DateOnly.FromDateTime(DateTime.Today),
            EndDate: DateOnly.FromDateTime(DateTime.Today.AddDays(2)),
            Month: null,
            Year: null,
            SuggestBestPeriod: false,
            LocationId: location.Id,
            LocationQuery: null,
            CamperCategory: null,
            CamperModelName: "Test Camper",
            PartySize: 2,
            TripDurationDays: null,
            WeekendTrip: false,
            OvertourismLevel: 3,
            MinDailyDriveHours: 2,
            MaxDailyDriveHours: 4,
            IsOwnedCamper: true,
            OwnedCamperModel: "Test Camper",
            RentalLocationId: null
        );

        // Act
        var itinerary = await _engine.SuggestAsync(preferences);

        // Assert
        Assert.All(itinerary.Days, day =>
        {
            Assert.NotNull(day.DriveHoursEstimate);
            Assert.True(day.DriveHoursEstimate >= 0);
        });
    }

    [Fact]
    public async Task SuggestAsync_ShouldIncludeOvernightRecommendationsExceptLastDay()
    {
        // Arrange
        var location = _locationCatalog.GetAll().First();
        var preferences = new TravelPreferences(
            PeriodType: Domain.Enums.TravelPeriodType.FixedDates,
            StartDate: DateOnly.FromDateTime(DateTime.Today),
            EndDate: DateOnly.FromDateTime(DateTime.Today.AddDays(2)),
            Month: null,
            Year: null,
            SuggestBestPeriod: false,
            LocationId: location.Id,
            LocationQuery: null,
            CamperCategory: null,
            CamperModelName: "Test Camper",
            PartySize: 2,
            TripDurationDays: null,
            WeekendTrip: false,
            OvertourismLevel: 3,
            MinDailyDriveHours: 2,
            MaxDailyDriveHours: 4,
            IsOwnedCamper: true,
            OwnedCamperModel: "Test Camper",
            RentalLocationId: null
        );

        // Act
        var itinerary = await _engine.SuggestAsync(preferences);

        // Assert
        var daysExceptLast = itinerary.Days.Take(itinerary.Days.Count - 1);
        Assert.All(daysExceptLast, day => Assert.NotNull(day.OvernightStopRecommendation));

        var lastDay = itinerary.Days.Last();
        Assert.Null(lastDay.OvernightStopRecommendation);
    }

    [Fact]
    public async Task SuggestAsync_DaysShouldHaveStops()
    {
        // Arrange
        var location = _locationCatalog.GetAll().First();
        var preferences = new TravelPreferences(
            PeriodType: Domain.Enums.TravelPeriodType.FixedDates,
            StartDate: DateOnly.FromDateTime(DateTime.Today),
            EndDate: DateOnly.FromDateTime(DateTime.Today.AddDays(2)),
            Month: null,
            Year: null,
            SuggestBestPeriod: false,
            LocationId: location.Id,
            LocationQuery: null,
            CamperCategory: null,
            CamperModelName: "Test Camper",
            PartySize: 2,
            TripDurationDays: null,
            WeekendTrip: false,
            OvertourismLevel: 3,
            MinDailyDriveHours: 2,
            MaxDailyDriveHours: 4,
            IsOwnedCamper: true,
            OwnedCamperModel: "Test Camper",
            RentalLocationId: null
        );

        // Act
        var itinerary = await _engine.SuggestAsync(preferences);

        // Assert
        Assert.All(itinerary.Days, day =>
        {
            Assert.NotEmpty(day.Stops);
            Assert.All(day.Stops, stop =>
            {
                Assert.NotEmpty(stop.Name);
                Assert.NotEqual(0, stop.Latitude);
                Assert.NotEqual(0, stop.Longitude);
            });
        });
    }
}
