namespace FileOrganizer.DataExtractor;

public interface ITextExtractorFactory
{
    ITextExtractor GetExtractor(string extension);
}