namespace FileOrganizer.DataExtractor.DocumentConnectors;

public class RtfTextExtractor(ITextExtractorService textExtractorService) : ITextExtractor
{
    public ExtractedText? Extract(string documentName, string documentType, Stream stream)
        => textExtractorService.ExtractTextFromRtf(documentName, documentType, stream);
}

public class HtmlTextExtractor(ITextExtractorService textExtractorService) : ITextExtractor
{
    public ExtractedText? Extract(string documentName, string documentType, Stream stream)
        => textExtractorService.ExtractTextFromHtml(documentName, documentType, stream);
}

public class ImageTextExtractor(ITextExtractorService textExtractorService) : ITextExtractor
{
    public ExtractedText? Extract(string documentName, string documentType, Stream stream)
        => textExtractorService.ExtractTextFromImage(documentName, documentType, stream);
}

public class OdtTextExtractor(ITextExtractorService textExtractorService) : ITextExtractor
{
    public ExtractedText? Extract(string documentName, string documentType, Stream stream)
        => textExtractorService.ExtractTextFromOdt(documentName, documentType, stream);
}