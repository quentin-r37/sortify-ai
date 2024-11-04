using System.IO.Compression;
using System.Text;
using System.Xml;

namespace FileOrganizer.DataExtractor.DocumentConnectors;

internal static class OdtFileExtensions
{
    internal static string ReadText(this Stream documentStream)
    {
        using var archive = new ZipArchive(documentStream, ZipArchiveMode.Read, leaveOpen: true);
        var contentEntry = archive.GetEntry("content.xml");
        if (contentEntry == null)
        {
            throw new InvalidOperationException("content.xml is missing in the ODT file.");
        }

        using var stream = contentEntry.Open();
        using var reader = XmlReader.Create(stream);
        var content = new StringBuilder();
        var inTextElement = false;

        while (reader.Read())
        {
            if (reader is { NodeType: XmlNodeType.Element, Name: "text:p" })
            {
                inTextElement = true;
                content.Append(Environment.NewLine);
            }
            else if (reader is { NodeType: XmlNodeType.EndElement, Name: "text:p" })
            {
                inTextElement = false;
            }
            else if (reader.NodeType == XmlNodeType.Text && inTextElement)
            {
                content.Append(reader.Value);
            }
        }

        return content.ToString().Trim();
    }
}