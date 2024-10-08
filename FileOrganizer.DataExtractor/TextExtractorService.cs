using static System.Net.Mime.MediaTypeNames;
using System.Collections.Concurrent;
using System.Text;
using UglyToad.PdfPig;

namespace FileOrganizer.DataExtractor
{
    public class TextExtractorService : ITextExtractorService
    {
        public ExtractedText ExtractTextFromPdf(string documentName, string documentType, Stream stream,
            bool shouldConvertToPicture = false)
        {
            return ExtractDirectTextFromPdf(documentName, documentType, stream);
        }

        private static ExtractedText ExtractDirectTextFromPdf(string documentName, string documentType, Stream stream)
        {
            var b = new StringBuilder();
            using var document = PdfDocument.Open(stream);
            foreach (var page in document.GetPages())
            {
                foreach (var word in page.GetWords())
                {
                    b.Append(word.Text);
                    b.Append(' ');
                }
            }
            return new ExtractedText(documentName, b.ToString(), b.Length);
        }
    }
}