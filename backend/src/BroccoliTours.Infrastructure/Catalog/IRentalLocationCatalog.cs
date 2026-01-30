using BroccoliTours.Domain.Models;

namespace BroccoliTours.Infrastructure.Catalog;

public interface IRentalLocationCatalog
{
    IReadOnlyList<RentalLocation> GetAll();
    RentalLocation? FindById(string id);
}
