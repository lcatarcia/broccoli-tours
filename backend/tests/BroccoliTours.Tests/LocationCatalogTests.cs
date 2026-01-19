using BroccoliTours.Infrastructure.Catalog;
using Xunit;

namespace BroccoliTours.Tests;

public class LocationCatalogTests
{
    private readonly InMemoryLocationCatalog _catalog = new();

    [Fact]
    public void GetAll_ShouldReturnMultipleLocations()
    {
        // Act
        var locations = _catalog.GetAll();

        // Assert
        Assert.NotEmpty(locations);
        Assert.True(locations.Count >= 4);
    }

    [Fact]
    public void GetAll_ShouldContainLocationWithId()
    {
        // Arrange
        var allLocations = _catalog.GetAll();
        var firstLocation = allLocations.First();

        // Act & Assert
        Assert.NotNull(firstLocation);
        Assert.NotEmpty(firstLocation.Id);
        Assert.NotEmpty(firstLocation.Name);
    }

    [Fact]
    public void GetAll_ShouldReturnItalianAndFrenchLocations()
    {
        // Act
        var locations = _catalog.GetAll();
        var countries = locations.Select(l => l.CountryCode).Distinct().ToList();

        // Assert
        Assert.Contains("IT", countries);
    }

    [Fact]
    public void GetAll_AllLocationsShouldHaveValidCoordinates()
    {
        // Act
        var locations = _catalog.GetAll();

        // Assert
        Assert.All(locations, location =>
        {
            Assert.InRange(location.Latitude, -90, 90);
            Assert.InRange(location.Longitude, -180, 180);
            Assert.NotEqual(0, location.Latitude);
            Assert.NotEqual(0, location.Longitude);
        });
    }
}
