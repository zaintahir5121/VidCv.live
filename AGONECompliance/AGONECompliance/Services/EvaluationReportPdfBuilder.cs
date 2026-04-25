using AGONECompliance.Shared;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace AGONECompliance.Services;

public interface IEvaluationReportPdfBuilder
{
    byte[] Build(ComplianceReportDto report, string workspaceName, string runLabel);
}

public sealed class EvaluationReportPdfBuilder : IEvaluationReportPdfBuilder
{
    public byte[] Build(ComplianceReportDto report, string workspaceName, string runLabel)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Margin(25);
                page.Size(PageSizes.A4.Landscape());
                page.DefaultTextStyle(x => x.FontSize(9));

                page.Header().Column(header =>
                {
                    header.Item().Text(string.IsNullOrWhiteSpace(report.HeaderTitle)
                            ? "AG ONE Compliance Evaluation Report"
                            : report.HeaderTitle)
                        .FontSize(18)
                        .Bold();
                    header.Item().Text($"Workspace: {workspaceName}");
                    header.Item().Text($"Run: {runLabel}");
                    header.Item().Text($"Generated (UTC): {report.GeneratedAtUtc:yyyy-MM-dd HH:mm:ss}");
                });

                page.Content().Column(content =>
                {
                    content.Spacing(10);

                    content.Item().Row(row =>
                    {
                        row.RelativeItem().Element(c => Card(c, "Total Rules", report.TotalRules.ToString()));
                        row.RelativeItem().Element(c => Card(c, "Compliant", report.CompliantCount.ToString()));
                        row.RelativeItem().Element(c => Card(c, "Non-Compliant", report.NonCompliantCount.ToString()));
                        row.RelativeItem().Element(c => Card(c, "Needs Review", report.NeedsReviewCount.ToString()));
                    });

                    content.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(1);   // Rule
                            columns.RelativeColumn(1.4f); // Title
                            columns.RelativeColumn(0.8f); // Category
                            columns.RelativeColumn(0.8f); // Action Party
                            columns.RelativeColumn(0.9f); // Status
                            columns.RelativeColumn(0.8f); // Confidence
                            columns.RelativeColumn(1.2f); // Guide Ref
                            columns.RelativeColumn(2.4f); // Reason + Evidence
                            columns.RelativeColumn(0.9f); // Page
                        });

                        table.Header(header =>
                        {
                            HeaderCell(header.Cell(), "Rule");
                            HeaderCell(header.Cell(), "Title");
                            HeaderCell(header.Cell(), "Category");
                            HeaderCell(header.Cell(), "Action");
                            HeaderCell(header.Cell(), "Status");
                            HeaderCell(header.Cell(), "Confidence");
                            HeaderCell(header.Cell(), "Guide Ref");
                            HeaderCell(header.Cell(), "Reason / Evidence");
                            HeaderCell(header.Cell(), "Page");
                        });

                        foreach (var item in report.Items.OrderBy(x => x.RuleCode))
                        {
                            BodyCell(table, item.RuleCode);
                            BodyCell(table, item.RuleTitle);
                            BodyCell(table, item.RuleCategory);
                            BodyCell(table, item.RuleActionParty);
                            BodyCell(table, item.Status.ToString());
                            BodyCell(table, $"{Math.Round(item.ConfidenceScore * 100, 1)}%");
                            BodyCell(table, item.GuideReference);
                            BodyCell(
                                table,
                                $"{item.Reason}\n{(string.IsNullOrWhiteSpace(item.EvidenceExcerpt) ? "-" : item.EvidenceExcerpt)}");
                            BodyCell(table, item.PageNumber?.ToString() ?? "-");
                        }
                    });
                });

                page.Footer().AlignCenter().Text(x =>
                {
                    x.Span(string.IsNullOrWhiteSpace(report.FooterText) ? "Aventra Group" : report.FooterText);
                    x.Span(" · ");
                    x.Span("Page ");
                    x.CurrentPageNumber();
                });
            });
        }).GeneratePdf();
    }

    private static void Card(IContainer container, string label, string value)
    {
        container
            .Border(1)
            .BorderColor(Colors.Grey.Lighten2)
            .Padding(8)
            .Column(col =>
            {
                col.Item().Text(label).FontSize(8).FontColor(Colors.Grey.Darken1);
                col.Item().Text(value).FontSize(16).Bold();
            });
    }

    private static void HeaderCell(IContainer container, string text)
    {
        container
            .Background(Colors.Grey.Lighten3)
            .Border(1)
            .BorderColor(Colors.Grey.Lighten1)
            .Padding(4)
            .Text(text)
            .Bold();
    }

    private static void BodyCell(TableDescriptor table, string text)
    {
        table.Cell()
            .Border(1)
            .BorderColor(Colors.Grey.Lighten2)
            .Padding(4)
            .Text(text ?? string.Empty);
    }
}
