namespace FileOrganizer.DataExtractor.DocumentConnectors;

public class TxtTextExtractor(ITextExtractorService textExtractorService) : ITextExtractor
{
    public ExtractedText? Extract(string documentName, string documentType, Stream stream)
    {
        var extractedText = textExtractorService.TxtTextFromPdf(documentName, documentType, stream);
        return extractedText;
    }
}