using BroccoliTours.Domain.Models;

namespace BroccoliTours.Infrastructure.Catalog;

public interface ICamperCatalog
{
    IReadOnlyList<Camper> GetAll();
}
