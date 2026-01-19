using System.Collections.Concurrent;
using BroccoliTours.Domain.Models;

namespace BroccoliTours.Infrastructure.Itineraries;

public sealed class InMemoryItineraryStore : IItineraryStore
{
    private readonly ConcurrentDictionary<string, Itinerary> _store = new(StringComparer.OrdinalIgnoreCase);

    public void Save(Itinerary itinerary) => _store[itinerary.Id] = itinerary;

    public Itinerary? Get(string id) => _store.TryGetValue(id, out var itinerary) ? itinerary : null;
}
