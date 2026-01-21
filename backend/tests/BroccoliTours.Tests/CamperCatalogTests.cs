using BroccoliTours.Infrastructure.Catalog;
using Xunit;

namespace BroccoliTours.Tests;

public class CamperCatalogTests
{
    private readonly InMemoryCamperCatalog _catalog = new();

    [Fact]
    public void GetAll_ShouldReturnSeventeenCampers()
    {
        // Act
        var campers = _catalog.GetAll();

        // Assert
        Assert.Equal(17, campers.Count);
    }

    [Fact]
    public void GetAll_ShouldReturnCampersInAllCategories()
    {
        // Act
        var campers = _catalog.GetAll();
        var categories = campers.Select(c => c.Category).Distinct().ToList();

        // Assert
        Assert.Equal(4, categories.Count);
        Assert.Contains(Domain.Enums.CamperCategory.Van, categories);
        Assert.Contains(Domain.Enums.CamperCategory.Campervan, categories);
        Assert.Contains(Domain.Enums.CamperCategory.SemiIntegrated, categories);
        Assert.Contains(Domain.Enums.CamperCategory.Motorhome, categories);
    }

    [Fact]
    public void GetAll_AllCampersShouldHaveValidCapacity()
    {
        // Act
        var campers = _catalog.GetAll();

        // Assert
        Assert.All(campers, camper => Assert.InRange(camper.Sleeps, 2, 6));
    }

    [Fact]
    public void GetAll_AllCampersShouldHaveValidLength()
    {
        // Act
        var campers = _catalog.GetAll();

        // Assert
        Assert.All(campers, camper => Assert.InRange(camper.LengthMeters, 4.5m, 10.0m));
    }

    [Fact]
    public void GetAll_ShouldIncludeVariousModels()
    {
        // Act
        var campers = _catalog.GetAll();

        // Assert
        Assert.Contains(campers, c => c.Id == "beach-hostel");
        Assert.Contains(campers, c => c.Id == "family-freedom");
        Assert.Contains(campers, c => c.Id == "surfer-suite");
    }
}
