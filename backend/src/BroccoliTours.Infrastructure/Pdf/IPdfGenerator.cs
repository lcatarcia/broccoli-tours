using BroccoliTours.Domain.Models;

namespace BroccoliTours.Infrastructure.Pdf;

public interface IPdfGenerator
{
    Task<byte[]> GenerateAsync(Itinerary itinerary, string mode, CancellationToken cancellationToken = default);
}
