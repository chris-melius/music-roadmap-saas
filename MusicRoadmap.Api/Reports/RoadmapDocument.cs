using System.Text.Json;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using QuestPDF.Companion; // Required for the .ShowInCompanion() extension

namespace MusicRoadmap.Api.Reports;

// Map the JSON structure to C# objects
public class RoadmapData
{
    public string StudentName { get; set; } = "";
    public int MinutesPerWeek { get; set; }
    public PhaseDetail Phase1 { get; set; } = new();
    public PhaseDetail Phase2 { get; set; } = new();
    public PhaseDetail Phase3 { get; set; } = new();
}

public class PhaseDetail
{
    public string Title { get; set; } = "";
    public string Weeks { get; set; } = "";
    public string Exercises { get; set; } = "";
    public string Pieces { get; set; } = "";
}

public class RoadmapDocument
{
    public static byte[] GeneratePdfBytes(string mockJson)
    {
 // 1. Parse the JSON into a strongly-typed C# object
    var data = JsonSerializer.Deserialize<RoadmapData>(mockJson, new JsonSerializerOptions 
    { 
        PropertyNameCaseInsensitive = true 
    }) ?? new RoadmapData();

    // 2. Create the beautiful visual document
    var document = Document.Create(container =>
    {
        container.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.Margin(0.75f, Unit.Inch);
            page.PageColor(Colors.White);
            page.DefaultTextStyle(x => x.FontSize(11).FontColor(Colors.Grey.Darken3));

            // Header Section (Professional Branding)
            page.Header().BorderBottom(2).BorderColor(Colors.DeepPurple.Darken2).PaddingBottom(10).Row(row =>
            {
                row.RelativeItem().Column(col =>
                {
                    col.Item().Text("12-Week Musical Roadmap").Bold().FontSize(24).FontColor(Colors.DeepPurple.Darken2);
                    col.Item().Text($"Customized for: {data.StudentName}").FontSize(14).FontColor(Colors.Grey.Darken1);
                });

                row.ConstantItem(100).AlignRight().AlignMiddle().Column(col => 
                {
                    col.Item().Text("Commitment").Bold().FontSize(10).FontColor(Colors.Grey.Darken1);
                    col.Item().Text($"{data.MinutesPerWeek} mins/wk").FontSize(12).Bold().FontColor(Colors.Green.Darken2);
                });
            });

            // Content Section (The Data Table)
            page.Content().PaddingVertical(20).Column(column =>
            {
                column.Spacing(15);
                
                column.Item().Text("Your Personalized Learning Path").FontSize(16).Bold();

                // Advanced QuestPDF Table
                column.Item().Table(table =>
                {
                    // Define Columns: 150 points for Timeline, 350 for the meat of the content
                    table.ColumnsDefinition(columns =>
                    {
                        columns.ConstantColumn(120); // Timeline
                        columns.RelativeColumn();     // Action Plan
                    });

                    // We create a reusable styling function to keep code clean
                    static IContainer HeaderStyle(IContainer c) => c.Background(Colors.DeepPurple.Darken2).Padding(10);
                    static IContainer CellStyle(IContainer c) => c.Padding(10).BorderBottom(1).BorderColor(Colors.Grey.Lighten2);

                    // Table Headers
                    table.Header(header =>
                    {
                        header.Cell().Element(HeaderStyle).Text("Timeline").Bold().FontColor(Colors.White);
                        header.Cell().Element(HeaderStyle).Text("Milestones & Action Steps").Bold().FontColor(Colors.White);
                    });

                    // Row 1: Phase 1
                    table.Cell().Element(CellStyle).Column(col => {
                        col.Item().Text(data.Phase1.Weeks).Bold().FontColor(Colors.DeepPurple.Darken1);
                        col.Item().Text(data.Phase1.Title).FontSize(10).Italic();
                    });
                    table.Cell().Element(CellStyle).Column(col => {
                        col.Item().Text("Focus Exercises:").Bold().FontSize(10);
                        col.Item().PaddingBottom(5).Text(data.Phase1.Exercises);
                        col.Item().Text("Target Pieces:").Bold().FontSize(10);
                        col.Item().Text(data.Phase1.Pieces);
                    });

                    // Row 2: Phase 2
                    table.Cell().Element(CellStyle).Column(col => {
                        col.Item().Text(data.Phase2.Weeks).Bold().FontColor(Colors.DeepPurple.Darken1);
                        col.Item().Text(data.Phase2.Title).FontSize(10).Italic();
                    });
                    table.Cell().Element(CellStyle).Column(col => {
                        col.Item().Text("Focus Exercises:").Bold().FontSize(10);
                        col.Item().PaddingBottom(5).Text(data.Phase2.Exercises);
                        col.Item().Text("Target Pieces:").Bold().FontSize(10);
                        col.Item().Text(data.Phase2.Pieces);
                    });

                    // Row 3: Phase 3
                    table.Cell().Element(CellStyle).Column(col => {
                        col.Item().Text(data.Phase3.Weeks).Bold().FontColor(Colors.DeepPurple.Darken1);
                        col.Item().Text(data.Phase3.Title).FontSize(10).Italic();
                    });
                    table.Cell().Element(CellStyle).Column(col => {
                        col.Item().Text("Focus Exercises:").Bold().FontSize(10);
                        col.Item().PaddingBottom(5).Text(data.Phase3.Exercises);
                        col.Item().Text("Target Pieces:").Bold().FontSize(10);
                        col.Item().Text(data.Phase3.Pieces);
                    });
                });
            });

            // Footer Section
            page.Footer().BorderTop(1).BorderColor(Colors.Grey.Lighten2).PaddingTop(10).AlignCenter().Text(x =>
            {
                x.Span("Plan powered by music-roadmap. Page ");
                x.CurrentPageNumber();
            });
        });
    });

        // 2. Instead of generating a file, shoot it straight to the Companion app!
        return document.GeneratePdf();
    }
}