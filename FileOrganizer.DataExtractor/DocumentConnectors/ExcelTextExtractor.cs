namespace FileOrganizer.DataExtractor.DocumentConnectors;

public class ExcelTextExtractor(ITextExtractorService textExtractorService) : ITextExtractor
{
    public ExtractedText? Extract(string documentName, string documentType, Stream stream)
        => textExtractorService.ExtractTextFromExcel(documentName, documentType, stream);
}