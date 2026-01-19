using BroccoliTours.Domain.Models;

namespace BroccoliTours.Infrastructure.Itineraries;

public interface IItineraryStore
{
    void Save(Itinerary itinerary);
    Itinerary? Get(string id);
}
