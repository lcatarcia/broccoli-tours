using System.Text;
using BroccoliTours.Domain.Models;

namespace BroccoliTours.Infrastructure.Pdf;

public sealed class SimplePdfGenerator : IPdfGenerator
{
    public Task<byte[]> GenerateAsync(Itinerary itinerary, string mode, CancellationToken cancellationToken = default)
    {
        var title = mode.Equals("brochure", StringComparison.OrdinalIgnoreCase)
            ? $"{itinerary.Title} (Brochure)"
            : $"{itinerary.Title} (Detailed)";

        var brochure = mode.Equals("brochure", StringComparison.OrdinalIgnoreCase);

        var header = BuildBroccoliHeader();
        var body = brochure
            ? BuildBrochureBody(itinerary)
            : BuildDetailedBody(itinerary);

        var pdf = MinimalPdf.Build(title, header + "\n\n" + body);
        return Task.FromResult(pdf);
    }


    private static string BuildBroccoliHeader()
    {
        return @"
╔═══════════════════════════════════════════════════════════════════════════════╗
║                                                                               ║
║   🥦  BROCCOLI TOURS  🥦                                                      ║
║       CAMPER ADVENTURES                                                       ║
║                                                                               ║
║   Il tuo tour operator specializzato in viaggi in camper                     ║
║                                                                               ║
╚═══════════════════════════════════════════════════════════════════════════════╝
";
    }
    private static string BuildBrochureBody(Itinerary itinerary)
    {
        var highlights = itinerary.Tips.Take(6).ToList();
        if (highlights.Count == 0) highlights.Add("Itinerario personalizzato con tappe e soste facili.");

        var dayTitles = itinerary.Days
            .OrderBy(d => d.DayNumber)
            .Select(d => $"Giorno {d.DayNumber}: {d.Title}")
            .ToList();

        return string.Join("\n", new[]
        {
            itinerary.Summary,
            "",
            "═══════════════════════════════════════════════════════════════════════════════",
            "DEPLIANT - Panoramica",
            "═══════════════════════════════════════════════════════════════════════════════",
            "",
            "Periodo: " + FormatPeriod(itinerary),
            "Durata: " + itinerary.Days.Count + " giorni",
            "",
            "───────────────────────────────────────────────────────────────────────────────",
            "HIGHLIGHTS",
            "───────────────────────────────────────────────────────────────────────────────",
            "",
            "✓ " + string.Join("\n✓ ", highlights),
            "",
            "───────────────────────────────────────────────────────────────────────────────",
            "PROGRAMMA GIORNALIERO",
            "───────────────────────────────────────────────────────────────────────────────",
            "",
            string.Join("\n", dayTitles),
            "",
            "───────────────────────────────────────────────────────────────────────────────",
            "NOTE IMPORTANTI PER IL CAMPERISTA",
            "───────────────────────────────────────────────────────────────────────────────",
            "",
            "• Arriva in area sosta entro le 16:30 quando possibile",
            "• Verifica sempre accessi ZTL e spazi di manovra (soprattutto con motorhome)",
            "• Porta sempre acqua potabile e scarico grigie a bordo"
        });
    }

    private static string BuildDetailedBody(Itinerary itinerary)
    {
        var parts = new List<string>
        {
            itinerary.Summary,
            "",
            "═══════════════════════════════════════════════════════════════════════════════",
            "ITINERARIO DETTAGLIATO",
            "═══════════════════════════════════════════════════════════════════════════════",
            "",
            "Periodo: " + FormatPeriod(itinerary),
            "Durata: " + itinerary.Days.Count + " giorni",
            ""
        };

        if (itinerary.Tips.Count > 0)
        {
            parts.Add("───────────────────────────────────────────────────────────────────────────────");
            parts.Add("💡 CONSIGLI DAL TOUR OPERATOR");
            parts.Add("───────────────────────────────────────────────────────────────────────────────");
            parts.Add("");
            parts.Add("✓ " + string.Join("\n✓ ", itinerary.Tips));
            parts.Add("");
        }

        foreach (var d in itinerary.Days.OrderBy(x => x.DayNumber))
        {
            parts.Add("═══════════════════════════════════════════════════════════════════════════════");
            parts.Add($"GIORNO {d.DayNumber}: {d.Title.ToUpper()}" + (d.Date.HasValue ? $" - {d.Date:dd/MM/yyyy}" : string.Empty));
            parts.Add("═══════════════════════════════════════════════════════════════════════════════");
            parts.Add("");
            
            if (d.DriveHoursEstimate.HasValue && d.DriveHoursEstimate > 0)
            {
                parts.Add($"🚗 Tempo di guida stimato: {d.DriveHoursEstimate:F1} ore");
                parts.Add("");
            }
            
            if (d.Stops.Count > 0)
            {
                parts.Add("📍 TAPPE:");
                parts.Add("");
                foreach (var stop in d.Stops)
                {
                    parts.Add($"  • {stop.Name}");
                    if (!string.IsNullOrEmpty(stop.Description))
                    {
                        parts.Add($"    {stop.Description}");
                    }
                }
                parts.Add("");
            }
            
            if (d.Activities.Count > 0)
            {
                parts.Add("🎯 ATTIVITÀ CONSIGLIATE:");
                parts.Add("");
                parts.Add("  • " + string.Join("\n  • ", d.Activities));
                parts.Add("");
            }
            
            if (!string.IsNullOrEmpty(d.OvernightStopRecommendation))
            {
                parts.Add("🌙 SOSTA NOTTURNA:");
                parts.Add($"  {d.OvernightStopRecommendation}");
                parts.Add("");
            }
        }

        return string.Join("\n", parts);
    }

    private static string FormatPeriod(Itinerary itinerary)
    {
        return itinerary.Period.Type switch
        {
            BroccoliTours.Domain.Enums.TravelPeriodType.FixedDates => $"{itinerary.Period.StartDate:yyyy-MM-dd} → {itinerary.Period.EndDate:yyyy-MM-dd}",
            BroccoliTours.Domain.Enums.TravelPeriodType.Month => $"{itinerary.Period.Year}-{itinerary.Period.Month:00}",
            _ => "Periodo consigliato (AI)"
        };
    }

    private static class MinimalPdf
    {
        public static byte[] Build(string title, string body)
        {
            // Minimal single-page PDF with Helvetica text.
            // Not feature-complete, but valid enough for most readers.
            var text = (title + "\n\n" + body).Trim();
            var lines = WrapLines(text, 92);

            var objects = new List<string>();
            objects.Add("<< /Type /Catalog /Pages 2 0 R >>");
            objects.Add("<< /Type /Pages /Kids [3 0 R] /Count 1 >>");
            objects.Add("<< /Type /Page /Parent 2 0 R /MediaBox [0 0 595 842] /Resources << /Font << /F1 4 0 R >> >> /Contents 5 0 R >>");
            objects.Add("<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>");

            var contentStream = BuildTextContent(lines);
            var contentBytes = Encoding.ASCII.GetBytes(contentStream);
            objects.Add($"<< /Length {contentBytes.Length} >>\nstream\n{contentStream}endstream");

            using var ms = new MemoryStream();
            void Write(string s) => ms.Write(Encoding.ASCII.GetBytes(s));

            Write("%PDF-1.4\n");

            var offsets = new List<long> { 0 };
            for (var i = 0; i < objects.Count; i++)
            {
                offsets.Add(ms.Position);
                var objNum = i + 1;
                Write($"{objNum} 0 obj\n");
                Write(objects[i]);
                Write("\nendobj\n");
            }

            var xrefStart = ms.Position;
            Write($"xref\n0 {objects.Count + 1}\n");
            Write("0000000000 65535 f \n");
            for (var i = 1; i < offsets.Count; i++)
            {
                Write($"{offsets[i]:D10} 00000 n \n");
            }

            Write("trailer\n");
            Write($"<< /Size {objects.Count + 1} /Root 1 0 R >>\n");
            Write("startxref\n");
            Write($"{xrefStart}\n");
            Write("%%EOF\n");

            return ms.ToArray();
        }

        private static string BuildTextContent(IReadOnlyList<string> lines)
        {
            // Start near top-left; move down each line with proper spacing.
            var sb = new StringBuilder();
            sb.Append("BT\n");
            sb.Append("/F1 10 Tf\n");  // Reduced to 10pt
            sb.Append("14 TL\n");       // Leading (line spacing) = 14pt
            sb.Append("50 780 Td\n");   // Start a bit lower

            foreach (var line in lines)
            {
                var escaped = EscapePdfString(line);
                sb.Append('(').Append(escaped).Append(") '\n");  // ' operator = move to next line and show text
            }

            sb.Append("ET\n");
            return sb.ToString();
        }

        private static IReadOnlyList<string> WrapLines(string text, int maxChars)
        {
            var rawLines = text.Replace("\r", "").Split('\n');
            var result = new List<string>();

            foreach (var raw in rawLines)
            {
                var line = raw.TrimEnd();
                if (line.Length <= maxChars)
                {
                    result.Add(line);
                    continue;
                }

                var words = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                var current = new StringBuilder();
                foreach (var w in words)
                {
                    if (current.Length == 0)
                    {
                        current.Append(w);
                        continue;
                    }

                    if (current.Length + 1 + w.Length > maxChars)
                    {
                        result.Add(current.ToString());
                        current.Clear();
                        current.Append(w);
                    }
                    else
                    {
                        current.Append(' ').Append(w);
                    }
                }

                if (current.Length > 0)
                    result.Add(current.ToString());
            }

            return result;
        }

        private static string EscapePdfString(string input)
        {
            return input
                .Replace("\\", "\\\\")
                .Replace("(", "\\(")
                .Replace(")", "\\)")
                .Replace("\r", "")
                .Replace("\n", " ");
        }
    }
}



