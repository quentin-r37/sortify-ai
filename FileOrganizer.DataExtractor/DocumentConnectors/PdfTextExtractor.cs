namespace FileOrganizer.DataExtractor.DocumentConnectors;

public class PdfTextExtractor(ITextExtractorService textExtractorService) : ITextExtractor
{
    public ExtractedText? Extract(string documentName, string documentType, Stream stream)
    {
        var extractedText = textExtractorService.ExtractTextFromPdf(documentName, documentType, stream);
        //if (extractedText?.Length == 0)
        //{
        //    extractedText = textExtractorService.ExtractTextFromPdf(documentName, documentType, stream, true);
        //}
        return extractedText;
    }
}