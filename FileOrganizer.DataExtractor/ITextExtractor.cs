namespace FileOrganizer.DataExtractor;

public interface ITextExtractor
{
    ExtractedText? Extract(string documentName, string documentType, Stream stream);
}