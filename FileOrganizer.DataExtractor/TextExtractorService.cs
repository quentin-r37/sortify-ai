using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using FileOrganizer.DataExtractor.DocumentConnectors;
using HtmlAgilityPack;
using NPOI.HSSF.UserModel;
using NPOI.HWPF;
using NPOI.HWPF.Extractor;
using NPOI.POIFS.FileSystem;
using RtfPipe;
using SkiaSharp;
using System.Collections.Concurrent;
using System.Text;
using System.Text.RegularExpressions;
using Tesseract;
using UglyToad.PdfPig;

namespace FileOrganizer.DataExtractor
{
    public class TextExtractorService(string rootPath) : ITextExtractorService
    {
        public ExtractedText ExtractTextFromPdf(string documentName, string documentType, Stream stream,
            bool shouldConvertToPicture = false)
        {
            return shouldConvertToPicture ? ExtractTextFromPdfImages(documentName, documentType, stream)
                : ExtractDirectTextFromPdf(documentName, documentType, stream);
        }

        public ExtractedText ExtractTextFromWord(string documentName, string documentType, Stream stream)
        {
            using var document = WordprocessingDocument.Open(stream, false);
            var text = document.ReadText();
            return new ExtractedText(documentName, text, text.Length);
        }

        public ExtractedText ExtractTextFromOdt(string documentName, string documentType, Stream stream)
        {
            var text = stream.ReadText();
            return new ExtractedText(documentName, text, text.Length);
        }

        public ExtractedText ExtractTextFromMailMessage(string documentName, string documentType, Stream stream)
        {
            using var reader = new MsgReader.Outlook.Storage.Message(stream);
            var textBody = reader.BodyText;
            var htmlBody = reader.BodyHtml;
            var content = textBody ?? htmlBody ?? string.Empty;
            var length = content.Length;
            return new ExtractedText(documentName, content, length);
        }

        public ExtractedText ExtractTextFromOldWord(string documentName, string documentType, Stream stream)
        {
            var streamForHwpf = new MemoryStream();
            var streamForWordProcessing = new MemoryStream();

            stream.CopyTo(streamForHwpf);
            stream.Seek(0, SeekOrigin.Begin);
            stream.CopyTo(streamForWordProcessing);

            streamForHwpf.Seek(0, SeekOrigin.Begin);
            streamForWordProcessing.Seek(0, SeekOrigin.Begin);

            try
            {
                var doc = new HWPFDocument(streamForHwpf);
                var extractor = new WordExtractor(doc);
                var extractedText = extractor.Text;
                return new ExtractedText(documentName, extractedText, extractedText.Length);
            }
            catch (OfficeXmlFileException)
            {
                streamForWordProcessing.Seek(0, SeekOrigin.Begin);
                using var document = WordprocessingDocument.Open(streamForWordProcessing, false);
                var text = document.ReadText();
                return new ExtractedText(documentName, text, text.Length);
            }
        }

        public ExtractedText TxtTextFromPdf(string documentName, string documentType, Stream stream)
        {
            using var reader = new StreamReader(stream);
            var text = reader.ReadToEnd();
            return new ExtractedText(documentName, text, text.Length);
        }

        public ExtractedText ExtractTextFromImage(string documentName, string documentType, Stream stream)
        {
            var tessdataPath = Path.Combine(rootPath, "tessdata");
            using var engine = new TesseractEngine(tessdataPath, "fra", EngineMode.Default);
            using var memoryStream = new MemoryStream();
            stream.CopyTo(memoryStream);
            using var img = Pix.LoadFromMemory(memoryStream.ToArray());
            using var page = engine.Process(img);
            var text = page.GetText();
            return new ExtractedText(documentName, text, text.Length);
        }

        public ExtractedText ExtractTextFromRtf(string documentName, string documentType, Stream stream)
        {
            using var reader = new StreamReader(stream);
            var rtfContent = reader.ReadToEnd();
            var plainText = Rtf.ToHtml(rtfContent);
            var doc = new HtmlDocument();
            doc.LoadHtml(plainText);
            var text = ExtractText(doc.DocumentNode);
            return new ExtractedText(documentName, text, text.Length);
        }

        public ExtractedText ExtractTextFromHtml(string documentName, string documentType, Stream stream)
        {
            using var reader = new StreamReader(stream);
            var htmlContent = reader.ReadToEnd();
            var doc = new HtmlDocument();
            doc.LoadHtml(htmlContent);
            var text = ExtractText(doc.DocumentNode);
            return new ExtractedText(documentName, text, text.Length);
        }

        public ExtractedText ExtractTextFromExcel(string documentName, string documentType, Stream stream)
        {
            var text = new StringBuilder();
            using (var document = SpreadsheetDocument.Open(stream, false))
            {
                var workbookPart = document.WorkbookPart;
                var sheets = workbookPart?.Workbook.Sheets;
                if (sheets != null)
                    foreach (var openXmlElement in sheets)
                    {
                        var sheet = (Sheet)openXmlElement;
                        var worksheetPart = (WorksheetPart)workbookPart?.GetPartById(sheet.Id);
                        var sheetData = worksheetPart?.Worksheet.GetFirstChild<SheetData>();
                        if (sheetData == null) continue;
                        foreach (var row in sheetData.Elements<Row>())
                        {
                            foreach (var cell in row.Elements<Cell>())
                            {
                                if (cell.CellFormula != null)
                                    continue;
                                if (workbookPart != null) text.Append(GetCellValue(workbookPart, cell));
                                text.Append(' ');
                            }
                            text.AppendLine();
                        }
                    }
            }
            var extractedText = text.ToString();
            return new ExtractedText(documentName, extractedText, extractedText.Length);
        }

        private static string GetCellValue(WorkbookPart workbookPart, Cell cell)
        {
            if (cell.DataType == null || cell.DataType.Value != CellValues.SharedString)
                return cell.CellValue?.InnerText ?? string.Empty;
            if (workbookPart.SharedStringTablePart == null) return string.Empty;
            var stringTable = workbookPart.SharedStringTablePart.SharedStringTable;
            return cell.CellValue?.InnerText != null ? stringTable.ElementAt(int.Parse(cell.CellValue.InnerText)).InnerText : string.Empty;
        }

        public ExtractedText ExtractTextFromOldExcel(string documentName, string documentType, Stream stream)
        {
            var workbook = new HSSFWorkbook(stream);
            var text = new StringBuilder();
            for (var i = 0; i < workbook.NumberOfSheets; i++)
            {
                var sheet = workbook.GetSheetAt(i);
                for (var j = 0; j <= sheet.LastRowNum; j++)
                {
                    var row = sheet.GetRow(j);
                    if (row == null) continue;
                    foreach (var cell in row.Cells)
                    {
                        text.Append(cell.ToString());
                        text.Append(' ');
                    }
                    text.AppendLine();
                }
            }

            var output = Regex.Replace(text.ToString(), @"(\s*\r\n\s*)+", "\r\n", RegexOptions.Compiled | RegexOptions.ExplicitCapture | RegexOptions.IgnoreCase);
            return new ExtractedText(documentName, output, output.Length);
        }


        private static string ExtractText(HtmlNode? node)
        {
            if (node == null)
                return string.Empty;
            if (node.NodeType == HtmlNodeType.Text)
            {
                return HtmlEntity.DeEntitize(node.InnerText);
            }
            var sb = new StringBuilder();
            foreach (var child in node.ChildNodes)
            {
                sb.Append(ExtractText(child) + " ");
            }
            return sb.ToString().Trim();
        }

        private ExtractedText ExtractTextFromPdfImages(string documentName, string documentType, Stream stream)
        {
            var images = PDFtoImage.Conversion.ToImages(stream).ToList();
            var b = new StringBuilder();
            var results = new ConcurrentDictionary<int, string>();

            Parallel.For(0, images.Count, i =>
            {
                using var bitmap = images[i];
                using var image = SKImage.FromBitmap(bitmap);
                using var data = image.Encode(SKEncodedImageFormat.Png, 80);
                using var memoryStream = new MemoryStream();
                data.SaveTo(memoryStream);
                memoryStream.Seek(0, SeekOrigin.Begin);
                var extractedText = ExtractTextFromImage(documentName, documentType, memoryStream);
                results[i] = extractedText.Text;
            });

            for (var i = 0; i < images.Count; i++)
            {
                b.Append(results[i]);
            }

            return new ExtractedText(documentName, b.ToString(), b.Length);
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