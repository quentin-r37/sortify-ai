namespace FileOrganizer.DataExtractor.DocumentConnectors;

public class OldExcelTextExtractor(ITextExtractorService textExtractorService) : ITextExtractor
{
    public ExtractedText? Extract(string documentName, string documentType, Stream stream)
        => textExtractorService.ExtractTextFromOldExcel(documentName, documentType, stream);
}