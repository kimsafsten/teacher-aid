using DocumentFormat.OpenXml.Packaging;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;

namespace TeacherAid.Api.Services;

public class DocumentExtractorService
{
    public string ExtractText(string filePath)
    {
        var extension = Path.GetExtension(filePath).ToLowerInvariant();

        return extension switch
        {
            ".pdf" => ExtractFromPdf(filePath),
            ".docx" => ExtractFromDocx(filePath),
            ".txt" => File.ReadAllText(filePath),
            _ => throw new NotSupportedException($"Filformatet '{extension}' stöds inte. Använd .pdf, .docx eller .txt.")
        };
    }

    private string ExtractFromPdf(string filePath)
    {
        using var pdf = PdfDocument.Open(filePath);
        var pages = pdf.GetPages()
            .Select(p => p.Text);
        return string.Join("\n", pages);
    }

    private string ExtractFromDocx(string filePath)
    {
        using var doc = WordprocessingDocument.Open(filePath, false);
        var body = doc.MainDocumentPart?.Document?.Body;
        if (body == null) return string.Empty;

        return string.Join("\n", body.InnerText
            .Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Select(l => l.Trim())
            .Where(l => l.Length > 0));
    }
}
