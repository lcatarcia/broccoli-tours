using BroccoliTours.Domain.Models;

namespace BroccoliTours.Infrastructure.Itineraries;

public interface IItineraryEngine
{
    Task<Itinerary> SuggestAsync(TravelPreferences preferences, CancellationToken cancellationToken = default);
}
