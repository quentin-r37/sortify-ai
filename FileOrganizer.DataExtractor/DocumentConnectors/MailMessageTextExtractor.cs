namespace FileOrganizer.DataExtractor.DocumentConnectors;

public class MailMessageTextExtractor(ITextExtractorService textExtractorService) : ITextExtractor
{
    public ExtractedText? Extract(string documentName, string documentType, Stream stream)
        => textExtractorService.ExtractTextFromMailMessage(documentName, documentType, stream);
}