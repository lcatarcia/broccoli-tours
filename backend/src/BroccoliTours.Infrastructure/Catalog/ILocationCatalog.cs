using BroccoliTours.Domain.Models;

namespace BroccoliTours.Infrastructure.Catalog;

public interface ILocationCatalog
{
    IReadOnlyList<Location> GetAll();
    Location? FindById(string id);
}
