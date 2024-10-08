namespace FileOrganizer.DataExtractor;

public interface ITextExtractorService
{
    ExtractedText ExtractTextFromPdf(string documentName, string documentType, Stream stream, bool shouldConvertToPicture = false);
}