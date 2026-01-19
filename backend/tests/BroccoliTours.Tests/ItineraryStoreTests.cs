using BroccoliTours.Domain.Models;
using BroccoliTours.Infrastructure.Itineraries;
using Xunit;

namespace BroccoliTours.Tests;

public class ItineraryStoreTests
{
    [Fact]
    public void Save_ShouldStoreItinerary()
    {
        // Arrange
        var store = new InMemoryItineraryStore();
        var itinerary = CreateTestItinerary();

        // Act
        store.Save(itinerary);

        // Assert
        Assert.NotNull(itinerary);
        Assert.NotEmpty(itinerary.Id);
        Assert.NotEmpty(itinerary.Title);
    }

    [Fact]
    public void Save_ShouldPersistMultipleItineraries()
    {
        // Arrange
        var store = new InMemoryItineraryStore();
        var itinerary1 = CreateTestItinerary();
        var itinerary2 = itinerary1 with { Id = "test-id-2", Title = "Another Itinerary" };

        // Act
        store.Save(itinerary1);
        store.Save(itinerary2);

        // Assert
        Assert.NotEqual(itinerary1.Id, itinerary2.Id);
    }

    [Fact]
    public void CreateTestItinerary_ShouldCreateValidItinerary()
    {
        // Act
        var itinerary = CreateTestItinerary();

        // Assert
        Assert.NotNull(itinerary);
        Assert.NotEmpty(itinerary.Days);
        Assert.All(itinerary.Days, day => Assert.NotEmpty(day.Stops));
    }

    private static Itinerary CreateTestItinerary()
    {
        var stop = new ItineraryStop("Test Stop", "Test Description", 45.0, 9.0, "poi");
        var day = new ItineraryDay(1, DateOnly.FromDateTime(DateTime.Today), "Day 1",
            new List<ItineraryStop> { stop }, new List<string> { "Activity" }, 2.0, "Test Area");

        var period = new TravelPeriod(
            Type: Domain.Enums.TravelPeriodType.FixedDates,
            StartDate: DateOnly.FromDateTime(DateTime.Today),
            EndDate: DateOnly.FromDateTime(DateTime.Today.AddDays(3)),
            Month: null,
            Year: null
        );

        return new Itinerary(
            Id: "test-id",
            Title: "Test Itinerary",
            Summary: "Test Summary",
            Period: period,
            Days: new List<ItineraryDay> { day },
            Tips: new List<string> { "Tip 1" },
            GeneratedAtUtc: DateTimeOffset.UtcNow
        );
    }
}