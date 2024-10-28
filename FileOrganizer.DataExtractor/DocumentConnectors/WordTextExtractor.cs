namespace FileOrganizer.DataExtractor.DocumentConnectors;

public class WordTextExtractor(ITextExtractorService textExtractorService) : ITextExtractor
{
    public ExtractedText? Extract(string documentName, string documentType, Stream stream)
        => textExtractorService.ExtractTextFromWord(documentName, documentType, stream);
}