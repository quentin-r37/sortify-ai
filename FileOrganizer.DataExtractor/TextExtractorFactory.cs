using FileOrganizer.DataExtractor.DocumentConnectors;

namespace FileOrganizer.DataExtractor;

public class TextExtractorFactory : ITextExtractorFactory
{
    private readonly IDictionary<string, Func<ITextExtractor>> _extractors;

    public TextExtractorFactory(ITextExtractorService textExtractorService)
    {
        var textExtractorService1 = textExtractorService;
        _extractors = new Dictionary<string, Func<ITextExtractor>>(StringComparer.OrdinalIgnoreCase)
        {
            [".xls"] = () => new OldExcelTextExtractor(textExtractorService1),
            [".xlsx"] = () => new ExcelTextExtractor(textExtractorService1),
            [".xlsm"] = () => new ExcelTextExtractor(textExtractorService1),
            [".doc"] = () => new WordTextExtractor(textExtractorService1),
            [".docx"] = () => new WordTextExtractor(textExtractorService1),
            [".odt"] = () => new OdtTextExtractor(textExtractorService1),
            [".pdf"] = () => new PdfTextExtractor(textExtractorService1),
            [".msg"] = () => new MailMessageTextExtractor(textExtractorService1),
            [".png"] = () => new ImageTextExtractor(textExtractorService1),
            [".rtf"] = () => new RtfTextExtractor(textExtractorService1),
            [".html"] = () => new HtmlTextExtractor(textExtractorService1),
            [".txt"] = () => new TxtTextExtractor(textExtractorService1)
        };
    }

    public ITextExtractor GetExtractor(string extension)
    {
        if (!_extractors.TryGetValue(extension, out var extractorFactory))
        {
            throw new ArgumentOutOfRangeException(nameof(extension),
                $@"{extension} extension is not supported yet");
        }

        return extractorFactory();
    }


}