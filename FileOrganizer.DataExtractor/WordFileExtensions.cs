using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using System.Text;

namespace FileOrganizer.DataExtractor;

internal static class WordFileExtensions
{
    internal static string ReadText(this WordprocessingDocument document)
    {
        if (document == null)
        {
            throw new ArgumentNullException(nameof(document), "Document cannot be null.");
        }

        StringBuilder sb = new();
        var mainPart = document.MainDocumentPart;
        if (mainPart == null)
        {
            throw new InvalidOperationException("The main document part is missing.");
        }

        var body = mainPart.Document.Body;
        if (body == null)
        {
            throw new InvalidOperationException("The document body is missing.");
        }

        foreach (var element in body.Elements())
        {
            AppendText(element, sb);
        }

        return sb.ToString();
    }

    private static void AppendText(OpenXmlElement element, StringBuilder sb)
    {
        if (element is Paragraph paragraph)
        {
            sb.AppendLine(paragraph.InnerText);
        }
        else if (element is Table table)
        {
            foreach (var row in table.Elements<TableRow>())
            {
                foreach (var cell in row.Elements<TableCell>())
                {
                    foreach (var cellElement in cell.Elements())
                    {
                        AppendText(cellElement, sb);
                    }
                }
            }
        }
        else if (element is SdtBlock sdtBlock)
        {
            foreach (var sdtElement in sdtBlock.Elements())
            {
                AppendText(sdtElement, sb);
            }
        }
        else
        {
            foreach (var childElement in element.Elements())
            {
                AppendText(childElement, sb);
            }
        }
    }

}