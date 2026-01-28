using BroccoliTours.Domain.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace BroccoliTours.Infrastructure.Pdf;

public sealed class EnhancedPdfGenerator : IPdfGenerator
{
    // Colori del brand (dal CSS)
    private static readonly string BrandBrown = "#8b6f47";
    private static readonly string BrandOrange = "#f4a460";
    private static readonly string BrandLight = "#e8d7c3";
    private static readonly string BrandDark = "#1a1510";
    private static readonly string BrandAccent = "#c9a87c";

    static EnhancedPdfGenerator()
    {
        // Configurazione della licenza QuestPDF (Community License per uso non commerciale)
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public Task<byte[]> GenerateAsync(Itinerary itinerary, string mode, CancellationToken cancellationToken = default)
    {
        var isBrochure = mode.Equals("brochure", StringComparison.OrdinalIgnoreCase);
        
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(40);
                
                page.Header().Element(c => { ComposeHeader(c, itinerary, isBrochure); });
                page.Content().Element(c => { if (isBrochure) ComposeBrochure(c, itinerary); else ComposeDetailed(c, itinerary); });
                page.Footer().Element(c => { ComposeFooter(c); });
            });
        });

        var pdf = document.GeneratePdf();
        return Task.FromResult(pdf);
    }

    private void ComposeHeader(IContainer container, Itinerary itinerary, bool isBrochure)
    {
        container.Column(column =>
        {
            // Box decorativo superiore
            column.Item().Background(BrandBrown).Height(8);
            
            column.Item().PaddingVertical(15).Column(inner =>
            {
                // Logo/Brand
                inner.Item().AlignCenter().Text("🥦 BROCCOLI TOURS 🥦")
                    .FontSize(24)
                    .Bold()
                    .FontColor(BrandOrange);
                
                inner.Item().AlignCenter().Text("CAMPER ADVENTURES")
                    .FontSize(12)
                    .FontColor(BrandAccent);
                
                inner.Item().PaddingTop(5).AlignCenter().Text("Il tuo tour operator specializzato in viaggi in camper")
                    .FontSize(9)
                    .Italic()
                    .FontColor(BrandAccent);
            });
            
            column.Item().Background(BrandBrown).Height(2);
            
            // Titolo itinerario
            column.Item().PaddingTop(15).Column(inner =>
            {
                inner.Item().Text(itinerary.Title)
                    .FontSize(20)
                    .Bold()
                    .FontColor(BrandOrange);
                
                if (isBrochure)
                {
                    inner.Item().Text("(Brochure)")
                        .FontSize(10)
                        .Italic()
                        .FontColor(BrandAccent);
                }
                else
                {
                    inner.Item().Text("(Itinerario Dettagliato)")
                        .FontSize(10)
                        .Italic()
                        .FontColor(BrandAccent);
                }
                
                inner.Item().PaddingTop(8).Text(itinerary.Summary)
                    .FontSize(11)
                    .FontColor(Colors.Grey.Darken2);
            });
            
            column.Item().PaddingTop(10).Background(BrandAccent).Height(1);
        });
    }

    private void ComposeFooter(IContainer container)
    {
        container.AlignCenter().Text(text =>
        {
            text.Span("Generato da ").FontSize(8).FontColor(Colors.Grey.Medium);
            text.Span("Broccoli Tours").FontSize(8).Bold().FontColor(BrandOrange);
            text.Span(" • ").FontSize(8).FontColor(Colors.Grey.Medium);
            text.CurrentPageNumber().FontSize(8).FontColor(Colors.Grey.Medium);
        });
    }

    private void ComposeBrochure(IContainer container, Itinerary itinerary)
    {
        container.Column(column =>
        {
            // Informazioni generali
            column.Item().PaddingTop(15).Column(inner =>
            {
                inner.Item().Row(row =>
                {
                    row.RelativeItem().Text(text =>
                    {
                        text.Span("📅 Periodo: ").Bold().FontColor(BrandBrown);
                        text.Span(FormatPeriod(itinerary)).FontSize(10);
                    });
                    
                    row.RelativeItem().Text(text =>
                    {
                        text.Span("⏱️ Durata: ").Bold().FontColor(BrandBrown);
                        text.Span($"{itinerary.Days.Count} giorni").FontSize(10);
                    });
                });
            });

            // Separatore
            column.Item().PaddingVertical(10).Background(BrandAccent).Height(1);

            // Highlights
            if (itinerary.Tips.Any())
            {
                column.Item().PaddingTop(10).Column(inner =>
                {
                    inner.Item().Text("✨ HIGHLIGHTS")
                        .FontSize(14)
                        .Bold()
                        .FontColor(BrandOrange);
                    
                    inner.Item().PaddingTop(8).Column(highlights =>
                    {
                        foreach (var tip in itinerary.Tips.Take(6))
                        {
                            highlights.Item().PaddingBottom(4).Row(row =>
                            {
                                row.ConstantItem(15).Text("✓").FontColor(BrandBrown).Bold();
                                row.RelativeItem().Text(tip).FontSize(10);
                            });
                        }
                    });
                });

                column.Item().PaddingVertical(10).Background(BrandAccent).Height(1);
            }

            // Programma giornaliero
            column.Item().PaddingTop(10).Column(inner =>
            {
                inner.Item().Text("📍 PROGRAMMA GIORNALIERO")
                    .FontSize(14)
                    .Bold()
                    .FontColor(BrandOrange);
                
                inner.Item().PaddingTop(8).Column(days =>
                {
                    foreach (var day in itinerary.Days.OrderBy(d => d.DayNumber))
                    {
                        days.Item().PaddingBottom(5).Text(text =>
                        {
                            text.Span($"Giorno {day.DayNumber}: ").Bold().FontColor(BrandBrown);
                            text.Span(day.Title).FontSize(10);
                            if (day.Date.HasValue)
                            {
                                text.Span($" • {day.Date:dd/MM/yyyy}").FontSize(9).FontColor(Colors.Grey.Darken1);
                            }
                        });
                    }
                });
            });

            // Note importanti
            column.Item().PaddingTop(15).Column(inner =>
            {
                inner.Item().Background(BrandAccent).Height(1);
                
                inner.Item().PaddingTop(10).Text("⚠️ NOTE IMPORTANTI PER IL CAMPERISTA")
                    .FontSize(12)
                    .Bold()
                    .FontColor(BrandOrange);
                
                inner.Item().PaddingTop(8).Column(notes =>
                {
                    var importantNotes = new[]
                    {
                        "Arriva in area sosta entro le 16:30 quando possibile",
                        "Verifica sempre accessi ZTL e spazi di manovra (soprattutto con motorhome)",
                        "Porta sempre acqua potabile e scarico grigie a bordo"
                    };
                    
                    foreach (var note in importantNotes)
                    {
                        notes.Item().PaddingBottom(4).Row(row =>
                        {
                            row.ConstantItem(15).Text("•").FontColor(BrandBrown);
                            row.RelativeItem().Text(note).FontSize(9);
                        });
                    }
                });
            });
        });
    }

    private void ComposeDetailed(IContainer container, Itinerary itinerary)
    {
        container.Column(column =>
        {
            // Informazioni generali
            column.Item().PaddingTop(15).Column(inner =>
            {
                inner.Item().Row(row =>
                {
                    row.RelativeItem().Text(text =>
                    {
                        text.Span("📅 Periodo: ").Bold().FontColor(BrandBrown);
                        text.Span(FormatPeriod(itinerary)).FontSize(10);
                    });
                    
                    row.RelativeItem().Text(text =>
                    {
                        text.Span("⏱️ Durata: ").Bold().FontColor(BrandBrown);
                        text.Span($"{itinerary.Days.Count} giorni").FontSize(10);
                    });
                });
            });

            column.Item().PaddingVertical(10).Background(BrandAccent).Height(1);

            // Consigli del tour operator
            if (itinerary.Tips.Any())
            {
                column.Item().PaddingTop(10).Column(inner =>
                {
                    inner.Item().Text("💡 CONSIGLI DAL TOUR OPERATOR")
                        .FontSize(14)
                        .Bold()
                        .FontColor(BrandOrange);
                    
                    inner.Item().PaddingTop(8).Column(tips =>
                    {
                        foreach (var tip in itinerary.Tips)
                        {
                            tips.Item().PaddingBottom(4).Row(row =>
                            {
                                row.ConstantItem(15).Text("✓").FontColor(BrandBrown).Bold();
                                row.RelativeItem().Text(tip).FontSize(10);
                            });
                        }
                    });
                });

                column.Item().PaddingVertical(10).Background(BrandAccent).Height(1);
            }

            // Giorni dettagliati
            foreach (var day in itinerary.Days.OrderBy(d => d.DayNumber))
            {
                column.Item().PageBreak();
                
                column.Item().Column(dayColumn =>
                {
                    // Header del giorno
                    dayColumn.Item().Background(BrandOrange).Padding(10).Column(dayHeader =>
                    {
                        dayHeader.Item().Text($"GIORNO {day.DayNumber}: {day.Title.ToUpper()}")
                            .FontSize(14)
                            .Bold()
                            .FontColor(Colors.White);
                        
                        if (day.Date.HasValue)
                        {
                            dayHeader.Item().Text($"📅 {day.Date:dd/MM/yyyy}")
                                .FontSize(10)
                                .FontColor(Colors.White);
                        }
                    });

                    // Tempo di guida
                    if (day.DriveHoursEstimate.HasValue && day.DriveHoursEstimate > 0)
                    {
                        dayColumn.Item().PaddingTop(10).Text(text =>
                        {
                            text.Span("🚗 Tempo di guida stimato: ").Bold().FontColor(BrandBrown);
                            text.Span($"{day.DriveHoursEstimate:F1} ore").FontSize(10);
                        });
                    }

                    // Tappe
                    if (day.Stops.Any())
                    {
                        dayColumn.Item().PaddingTop(15).Column(stopsSection =>
                        {
                            stopsSection.Item().Text("📍 TAPPE")
                                .FontSize(12)
                                .Bold()
                                .FontColor(BrandOrange);
                            
                            stopsSection.Item().PaddingTop(8).Column(stops =>
                            {
                                foreach (var stop in day.Stops)
                                {
                                    stops.Item().PaddingBottom(8).Column(stopItem =>
                                    {
                                        var emoji = GetStopEmoji(stop.Type);
                                        
                                        stopItem.Item().Text(text =>
                                        {
                                            text.Span($"{emoji} ").FontSize(12);
                                            text.Span(stop.Name).Bold().FontSize(11).FontColor(BrandBrown);
                                        });
                                        
                                        if (!string.IsNullOrEmpty(stop.Description))
                                        {
                                            stopItem.Item().PaddingLeft(20).PaddingTop(2).Text(stop.Description)
                                                .FontSize(10)
                                                .FontColor(Colors.Grey.Darken2);
                                        }
                                    });
                                }
                            });
                        });
                    }

                    // Attività
                    if (day.Activities.Any())
                    {
                        dayColumn.Item().PaddingTop(15).Column(activitiesSection =>
                        {
                            activitiesSection.Item().Text("🎯 ATTIVITÀ CONSIGLIATE")
                                .FontSize(12)
                                .Bold()
                                .FontColor(BrandOrange);
                            
                            activitiesSection.Item().PaddingTop(8).Column(activities =>
                            {
                                foreach (var activity in day.Activities)
                                {
                                    activities.Item().PaddingBottom(4).Row(row =>
                                    {
                                        row.ConstantItem(15).Text("•").FontColor(BrandBrown);
                                        row.RelativeItem().Text(activity).FontSize(10);
                                    });
                                }
                            });
                        });
                    }

                    // Sosta notturna
                    if (!string.IsNullOrEmpty(day.OvernightStopRecommendation))
                    {
                        dayColumn.Item().PaddingTop(15).Background(BrandLight).Padding(10).Column(overnight =>
                        {
                            overnight.Item().Text("🌙 SOSTA NOTTURNA CONSIGLIATA")
                                .FontSize(11)
                                .Bold()
                                .FontColor(BrandBrown);
                            
                            overnight.Item().PaddingTop(5).Text(day.OvernightStopRecommendation)
                                .FontSize(10)
                                .FontColor(BrandDark);
                        });
                    }
                });
            }
        });
    }

    private static string GetStopEmoji(string stopType)
    {
        return stopType.ToLowerInvariant() switch
        {
            "attraction" => "🎭",
            "village" => "🏘️",
            "camper_area" => "🏕️",
            "viewpoint" => "👁️",
            "food" => "🍽️",
            "restaurant" => "🍴",
            "cafe" => "☕",
            "museum" => "🏛️",
            "park" => "🌳",
            "beach" => "🏖️",
            "mountain" => "⛰️",
            "lake" => "🏞️",
            "shopping" => "🛍️",
            "nightlife" => "🌃",
            _ => "📍"
        };
    }

    private static string FormatPeriod(Itinerary itinerary)
    {
        return itinerary.Period.Type switch
        {
            Domain.Enums.TravelPeriodType.FixedDates => $"{itinerary.Period.StartDate:dd/MM/yyyy} → {itinerary.Period.EndDate:dd/MM/yyyy}",
            Domain.Enums.TravelPeriodType.Month => $"{GetMonthName(itinerary.Period.Month)} {itinerary.Period.Year}",
            _ => "Periodo consigliato"
        };
    }

    private static string GetMonthName(int? month)
    {
        if (!month.HasValue) return "Mese sconosciuto";
        
        return month.Value switch
        {
            1 => "Gennaio",
            2 => "Febbraio",
            3 => "Marzo",
            4 => "Aprile",
            5 => "Maggio",
            6 => "Giugno",
            7 => "Luglio",
            8 => "Agosto",
            9 => "Settembre",
            10 => "Ottobre",
            11 => "Novembre",
            12 => "Dicembre",
            _ => "Mese sconosciuto"
        };
    }
}
