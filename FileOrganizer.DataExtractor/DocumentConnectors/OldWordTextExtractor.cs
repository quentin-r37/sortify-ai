namespace FileOrganizer.DataExtractor.DocumentConnectors;

public class OldWordTextExtractor(ITextExtractorService textExtractorService) : ITextExtractor
{
    public ExtractedText? Extract(string documentName, string documentType, Stream stream)
        => textExtractorService.ExtractTextFromOldWord(documentName, documentType, stream);
}