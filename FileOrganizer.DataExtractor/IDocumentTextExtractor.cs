namespace FileOrganizer.DataExtractor;

public interface IDocumentTextExtractor
{
    ExtractedText ExtractTextAsync(string documentName, string fileType, FileStream document);
}