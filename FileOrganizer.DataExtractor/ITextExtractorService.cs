namespace FileOrganizer.DataExtractor;

public interface ITextExtractorService
{

    ExtractedText TxtTextFromPdf(string documentName, string documentType, Stream stream);
    ExtractedText ExtractTextFromImage(string documentName, string documentType, Stream stream);
    ExtractedText ExtractTextFromWord(string documentName, string documentType, Stream stream);
    ExtractedText ExtractTextFromOdt(string documentName, string documentType, Stream stream);
    ExtractedText ExtractTextFromOldWord(string documentName, string documentType, Stream stream);
    ExtractedText ExtractTextFromPdf(string documentName, string documentType, Stream stream, bool shouldConvertToPicture = false);
    ExtractedText ExtractTextFromMailMessage(string documentName, string documentType, Stream stream);
    ExtractedText ExtractTextFromRtf(string documentName, string documentType, Stream stream);
    ExtractedText ExtractTextFromHtml(string documentName, string documentType, Stream stream);
    ExtractedText ExtractTextFromExcel(string documentName, string documentType, Stream stream);
    ExtractedText ExtractTextFromOldExcel(string documentName, string documentType, Stream stream);
}