using MimeTypes;

namespace FileOrganizer.DataExtractor.DocumentConnectors;

public class DocumentTextExtractor(TextExtractorFactory textExtractorFactory) : IDocumentTextExtractor
{
    public ExtractedText ExtractTextAsync(string documentName, string fileType, FileStream memStream)
    {
        var mimeType = MimeTypeMap.GetMimeType(documentName);
        var extension = MimeTypeMap.GetExtension(mimeType, false);
        var extractor = textExtractorFactory.GetExtractor(extension);
        return extractor.Extract(documentName, fileType, memStream) ?? new ExtractedText(string.Empty, string.Empty, 0);
    }
}