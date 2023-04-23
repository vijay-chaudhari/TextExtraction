using com.sun.org.apache.bcel.@internal.generic;
using com.sun.swing.@internal.plaf.metal.resources;
using com.sun.tools.javac.jvm;
using edu.stanford.nlp.ie.crf;
using edu.stanford.nlp.parser.lexparser;
using iTextSharp.text.pdf;
using java.lang;
using javax.sound.sampled;
using javax.xml.soap;
using Microsoft.VisualBasic;
using NameRecognizer;
using Newtonsoft.Json;
using OCR;
using Org.BouncyCastle.Utilities.IO;
using Pdf_To_ImageStream;
using System.Drawing;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Text.RegularExpressions;
using Tesseract;
using TextExtraction.Model;
using TextExtraction.Services;
using static iTextSharp.text.pdf.AcroFields;
using Exception = System.Exception;
using StringBuilder = System.Text.StringBuilder;

namespace TextExtraction
{
    public class Worker : BackgroundService
    {
        private string? _inputPath;
        private string? _outputPath;
        private string[]? _primarySerachKeys;
        private int counter = 0;
        private DbHelper _dbHelper;
        private CRFClassifier? _crfClassifier;
        private TesseractEngine? _engine;
        private readonly ILogger<Worker> _logger;
        private readonly IConfiguration _config;

        public Worker(ILogger<Worker> logger, IConfiguration config, DbHelper dbHelper)
        {
            _logger = logger;
            _config = config;
            _dbHelper = dbHelper;
        }
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (Initialise() && !string.IsNullOrEmpty(_inputPath) && !string.IsNullOrEmpty(_outputPath) && _primarySerachKeys.Any())
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);

                    // Get all PDF files in the input folder
                    string[] pdfFiles = Directory.GetFiles(_inputPath, "*.pdf");
                    //Dictionary<int, ImageOcr> _data = new Dictionary<int, ImageOcr>();

                    PrepareInvoiceData(pdfFiles);
                    //DeleteFiles(pdfFiles);
                    // Process each PDF file
                    #region GetText and process
                    //foreach (var pdf in pdfFiles)
                    //{
                    //    StringBuilder sb = new StringBuilder();

                    //    var streams = Pdf_To_ImageStream.Convert.ToStreams(@"C:\Program Files\gs\gs10.00.0\bin\gsdll64.dll", pdf);

                    //    if (streams is not null)
                    //    {
                    //        float confidense = 0;
                    //        foreach (var stream in streams)
                    //        {
                    //            string text = OCR.Image.GetTextFromTiffImageStream(_engine, stream);
                    //            sb.append(Regex.Replace(text, @"^\s*$\n", string.Empty, RegexOptions.Multiline).TrimEnd());
                    //            confidense += OCR.Image.Confidence;
                    //        }

                    //        string outputFilePath = Path.Combine(_outputPath, Path.GetFileName(pdf));
                    //        _data.Add(counter++, new ImageOcr { InputPath = pdf, OutputPath = outputFilePath, OCRText = sb.toString(), Confidence = string.Format("{0:0.00}", confidense / streams.Count()) });
                    //    }
                    //}

                    //foreach (var item in _data)
                    //{
                    //    Console.WriteLine(item.Value.OutputPath);
                    //   TextExtractionFields detail = new TextExtractionFields();
                    //    var lines = item.Value.OCRText.Split('\n');
                    //    _primarySerachKeys = _primarySerachKeys.Select(x => x.ToUpper()).ToArray();
                    //    foreach (var line in lines)
                    //    {
                    //        string filteredText = FilterData.RemoveSpecialCharacters(line).ToUpper();
                    //        if (_primarySerachKeys.Any(filteredText.Contains))
                    //        {
                    //            var amount = Regex.Match(line.ToUpper(), @"\b(TOTAL|RATE|BALANCE DUE)\s+\$\d+(,\d{3})*(\.\d{2})?");
                    //            if (amount.Success)
                    //            {
                    //                detail.TotalAmount = amount.Value.Replace("TOTAL", "").Replace("RATE", "").Replace("BALANCE DUE", "");
                    //            }

                    //            var invoiceNumber = Regex.Match(filteredText, @"\bINVOICE\s+(\d+)\b");
                    //            if (invoiceNumber.Success)
                    //            {
                    //                detail.Invoice = Regex.Match(filteredText, @"[.\d]+").Value;
                    //            }
                    //            var orderNumber = Regex.Match(filteredText, @"\b(LOAD|REFERENCE)\s+(\w+\d+\w+)");
                    //            if (orderNumber.Success)
                    //            {
                    //                detail.OrderNumber = orderNumber.Groups[2].Value;
                    //            }
                    //            var invoiceDate = Regex.Match(filteredText, @"\b(INVOICE DATE|DATE)\s+");
                    //            if (invoiceDate.Success && string.IsNullOrEmpty(detail.InvoiceDate))
                    //            {
                    //                var date = EntityRecognizer.RecognizeDate(filteredText);
                    //                detail.InvoiceDate = date;
                    //            }
                    //            var invoiceDueDate = Regex.Match(filteredText, @"\b(DUE DATE)\s+");
                    //            if (invoiceDueDate.Success)
                    //            {
                    //                var date = EntityRecognizer.RecognizeDate(filteredText);
                    //                detail.InvoiceDueDate = date;
                    //            }
                    //        }
                    //    }

                    //    item.Value.Output = JsonConvert.SerializeObject(detail);
                    //    //_dbHelper.InsertData(item.Value);
                    //    Console.WriteLine($"{item.Value.Output}");


                    #region Medical details
                    //    //var lines = item.Value.OCRText.Split('\n');
                    //    //foreach (var line in lines)
                    //    //{
                    //    //    string filteredText = FilterData.RemoveSpecialCharacters(line).ToUpper();
                    //    //    _primarySerachKeys = _primarySerachKeys.Select(x => x.ToUpper()).ToArray();

                    //    //    if (_primarySerachKeys.Any(filteredText.Contains))
                    //    //    {
                    //    //        if (string.IsNullOrEmpty(detail.BirthDate))
                    //    //        {
                    //    //            if (filteredText.Contains("Date Of Birth", StringComparison.OrdinalIgnoreCase) || filteredText.Contains("DOB", StringComparison.OrdinalIgnoreCase) || filteredText.Contains("Birth Date", StringComparison.OrdinalIgnoreCase))
                    //    //            {
                    //    //                string date = EntityRecognizer.RecognizeDate(filteredText);
                    //    //                if (!string.IsNullOrEmpty(date))
                    //    //                {
                    //    //                    detail.BirthDate = date;
                    //    //                }
                    //    //            }
                    //    //        }
                    //    //        if (string.IsNullOrEmpty(detail.Name))
                    //    //        {

                    //    //            string? personName = EntityRecognizer.GetPersonName(filteredText, _crfClassifier);
                    //    //            if (!string.IsNullOrEmpty(personName))
                    //    //            {
                    //    //                detail.Name = personName;
                    //    //            }
                    //    //        }
                    //    //    }
                    //    //    else if (!string.IsNullOrEmpty(detail.Name) && !string.IsNullOrEmpty(detail.BirthDate))
                    //    //    {
                    //    //        break;
                    //    //    }
                    //    //}
                    //    //if (!string.IsNullOrEmpty(detail.Name))
                    //    //{
                    //    //    detail.Name = CryptLib.Encrypt(detail.Name);
                    //    //}
                    //    //if (!string.IsNullOrEmpty(detail.BirthDate))
                    //    //{
                    //    //    detail.BirthDate = CryptLib.Encrypt(detail.BirthDate);
                    //    //}

                    //    //item.Value.Output = JsonConvert.SerializeObject(detail);
                    //    //_dbHelper.InsertData(item.Value);
                    //    //// Move the processed PDF file to the output folder
                    //    //File.Move(item.Value.InputPath, item.Value.OutputPath);
                    //    //Console.WriteLine($"{item.Key}  {item.Value.InputPath}  {item.Value.OutputPath} {item.Value.Output} {item.Value.Confidence}"); 
                    #endregion
                    //} 
                    #endregion

                    // Wait for 1 minute before checking the input folder again
                    await Task.Delay(TimeSpan.FromMinutes(60), stoppingToken);
                }
            }
            else
            {
                _logger.LogError("Input/Output path should not empty!");
            }
        }

        private void PrepareInvoiceData(string[] pdfFiles)
        {
            List<ProcessedPdf> textData = new List<ProcessedPdf>();
            foreach (var pdf in pdfFiles)
            {
                var document = new ProcessedPdf();
                document.DocumentId = Guid.NewGuid();
                document.FileName = Path.GetFileName(pdf);

                var streams = Pdf_To_ImageStream.Convert.ToStreams(@"C:\Program Files\gs\gs10.00.0\bin\gsdll64.dll", pdf);

                if (streams is not null)
                {
                    for (int i = 0; i < streams.Count; i++)
                    {
                        var page = new PageData();
                        int j = i;
                        page.PageNumber = ++j;
                        try
                        {
                            page = GetPageFromTiffImageStream(_engine, streams[i], page, out float confidence);
                            document.Confidence += confidence;
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex.Message);
                        }
                        document.Pages.Add(page);
                    }
                    document.Confidence /= streams.Count();
                }
                textData.Add(document);
            }

            ExtractInvoiceData(textData);
        }
        private PageData GetPageFromTiffImageStream(TesseractEngine engine, MemoryStream stream, PageData pageData, out float confidence)
        {
            try
            {
                using var image = Pix.LoadTiffFromMemory(stream.ToArray());
                using var page = engine.Process(image);
                confidence = page.GetMeanConfidence();
                using var iter = page.GetIterator();
                iter.Begin();
                LineData lineData;
                WordData wordData;
                int srNo = 1;
                do
                {
                    do
                    {
                        do
                        {
                            lineData = new();
                            do
                            {
                                wordData = new();
                                string resultText = iter.GetText(PageIteratorLevel.Word);
                                resultText = FilterData.RemoveSpecialCharacters(resultText);
                                if (!string.IsNullOrWhiteSpace(resultText))
                                {
                                    wordData.Text = resultText;
                                    iter.TryGetBoundingBox(PageIteratorLevel.Word, out Rect bound);
                                    wordData.Coordinates = bound;
                                    lineData.Words.Add(wordData);
                                }

                                if (iter.IsAtFinalOf(PageIteratorLevel.TextLine, PageIteratorLevel.Word))
                                {
                                    string lineText = iter.GetText(PageIteratorLevel.TextLine);
                                    lineText = FilterData.RemoveSpecialCharacters(lineText);
                                    if (!string.IsNullOrWhiteSpace(lineText))
                                    {
                                        lineData.LineNumber = srNo++;
                                        lineData.Text = lineText;
                                        iter.TryGetBoundingBox(PageIteratorLevel.TextLine, out Rect bound);
                                        lineData.LineCoordinates = bound;
                                        pageData.Lines.Add(lineData);
                                    }
                                }
                            } while (iter.Next(PageIteratorLevel.TextLine, PageIteratorLevel.Word));
                        } while (iter.Next(PageIteratorLevel.Para, PageIteratorLevel.TextLine));
                    } while (iter.Next(PageIteratorLevel.Block, PageIteratorLevel.Para));
                } while (iter.Next(PageIteratorLevel.Block));

                return pageData;
            }
            catch (Exception ex)
            {
                throw;
            }
        }
        private void ExtractInvoiceData(List<ProcessedPdf> textData)
        {
            foreach (var documents in textData)
            {
                List<DrawCoordinates> cords = new();
                TextExtractionFields textExtraction = new TextExtractionFields();

                foreach (var page in documents.Pages)
                {
                    var details = new DrawCoordinates { PageNumber = page.PageNumber };
                    foreach (var line in page.Lines)
                    {
                        _primarySerachKeys = _primarySerachKeys.Select(x => x.ToUpper()).ToArray();
                        line.Text = line.Text.ToUpper();

                        //string? orgName = EntityRecognizer.GetOrganizationName(line.Text, _crfClassifier);
                        if (documents.FileName.Equals("Invoice_4329_from_JJ_MARIN_LLC.pdf"))
                        {
                            if (string.IsNullOrEmpty(textExtraction.Invoice.Supplier.CompanyName))
                            {
                                if (line.Text.Equals("J.J. MARIN, LLC"))
                                {
                                    details.Rects.Add(line.LineCoordinates);
                                    textExtraction.Invoice.Supplier.CompanyName = "J.J. MARIN, LLC";
                                }
                            }
                            if (string.IsNullOrEmpty(textExtraction.Invoice.Customer.Name))
                            {
                                if (line.Text.Equals("TECHNETRONIX,LLC"))
                                {
                                    details.Rects.Add(line.LineCoordinates);
                                    textExtraction.Invoice.Customer.Name = "TECHNETRONIX,LLC";
                                }
                            }
                        }
                        if (documents.FileName.Equals("Factoring Invoice - 2020-09-23T150930.834.pdf"))
                        {
                            if (string.IsNullOrEmpty(textExtraction.Invoice.Supplier.CompanyName))
                            {
                                if (line.Text.Equals("COMFREIGHT HAULPAY"))
                                {
                                    details.Rects.Add(line.LineCoordinates);
                                    textExtraction.Invoice.Supplier.CompanyName = "COMFREIGHT HAULPAY";
                                }
                            }
                            if (string.IsNullOrEmpty(textExtraction.Invoice.Customer.Name))
                            {
                                string tx = Regex.Replace(line.Text, @"\b(LOAD|REFERENCE)\W+(\w+\d+\w+)", "");
                                if (tx.Equals("JW LOGISTICS OPERATIONS LLC "))
                                {
                                    details.Rects.Add(line.LineCoordinates);
                                    textExtraction.Invoice.Customer.Name = "JW LOGISTICS OPERATIONS LLC";
                                }
                            }
                        }
                        if (documents.FileName.Equals("Factoring invoice - 2020-09-23T151006.783.pdf"))
                        {
                            if (string.IsNullOrEmpty(textExtraction.Invoice.Supplier.CompanyName))
                            {
                                if (line.Text.Equals("RTS FINANCIAL SERVICE, INC."))
                                {
                                    details.Rects.Add(line.LineCoordinates);
                                    textExtraction.Invoice.Supplier.CompanyName = "RTS FINANCIAL SERVICE, INC";

                                }
                            }
                            if (string.IsNullOrEmpty(textExtraction.Invoice.Customer.Name))
                            {
                                if (line.Text.Equals("J.W. LOGISTICS OPERATIONS, LLC"))
                                {
                                    details.Rects.Add(line.LineCoordinates);
                                    textExtraction.Invoice.Customer.Name = "J.W. LOGISTICS OPERATIONS, LLC";

                                }
                            }
                        }

                        //if (!string.IsNullOrEmpty(orgName)) //&& string.IsNullOrEmpty(textExtraction.Invoice.Supplier.CompanyName))
                        //{
                        //    var reallyAOrg = Regex.Match(orgName, @"\b\W+INC|LLC$\b");
                        //    if (reallyAOrg.Success && string.IsNullOrEmpty(textExtraction.Invoice.Supplier.CompanyName))
                        //    {
                        //        textExtraction.Invoice.Supplier.CompanyName = orgName;
                        //    } 
                        //}

                        if (_primarySerachKeys.Any(line.Text.Contains))
                        {
                            var amount = Regex.Match(line.Text, @"\b(TOTAL|RATE|BALANCE DUE)\W+\$\d+(,\d{3})*(\.\d{2})?");

                            if (amount.Success && string.IsNullOrEmpty(textExtraction.Invoice.Payment.Total))
                            {
                                string s = amount.Value.Replace("TOTAL", "").Replace("RATE", "").Replace("BALANCE DUE", "").Trim();
                                textExtraction.Invoice.Payment.Total = s;
                               // Console.WriteLine("Amount :" + amount.Value.Replace("TOTAL", "").Replace("RATE", "").Replace("BALANCE DUE", ""));
                                details.Rects.Add(line.Words.Single(x => x.Text == s).Coordinates);
                            }

                            var invoiceNumber = Regex.Match(line.Text, @"\b(INVOICE)(\W+|\s+)(\d+)\b");
                            if (invoiceNumber.Success && string.IsNullOrEmpty(textExtraction.Invoice.Number))
                            {
                                string number = Regex.Match(line.Text, @"[.\d]+").Value;
                                textExtraction.Invoice.Number = number;
                                //Console.WriteLine("Invoice Number :" + Regex.Match(line.Text, @"[.\d]+").Value);
                                details.Rects.Add(line.Words.Single(x => x.Text == number).Coordinates);
                            }

                            var orderNumber = Regex.Match(line.Text, @"\b(LOAD|REFERENCE)\W+(\w+\d+\w+)");
                            if (orderNumber.Success && string.IsNullOrEmpty(textExtraction.Invoice.PurchaseOrderNumber))
                            {
                                //Console.WriteLine("Order number :" + orderNumber.Groups[2].Value);
                                textExtraction.Invoice.PurchaseOrderNumber = orderNumber.Groups[2].Value;
                                details.Rects.Add(line.Words.Single(x => x.Text == orderNumber.Groups[2].Value).Coordinates);
                            }

                            var invoiceDate = Regex.Match(line.Text, @"^(?!.*DUE.*DATE)(?=.*(?:INVOICE\s+)?DATE).*$");
                            if (invoiceDate.Success && string.IsNullOrEmpty(textExtraction.Invoice.Date))
                            {
                                var date = EntityRecognizer.RecognizeDate(line.Text).ToUpper();
                                if (string.IsNullOrEmpty(date)) continue;
                                textExtraction.Invoice.Date = date;
                                //Console.WriteLine("Invoice date :" + date);
                                var result = line.Words.SingleOrDefault(x => x.Text.Equals(date, StringComparison.OrdinalIgnoreCase))?.Coordinates;
                                if (result is null)
                                {
                                    var arr = date.Split(' ');
                                    int x1 = 0, y1 = 0, x2 = 0, y2 = 0;
                                    for (int i = 0; i < arr.Length; i++)
                                    {
                                        var a = line.Words.SingleOrDefault(x => x.Text.Equals(arr[i], StringComparison.OrdinalIgnoreCase))?.Coordinates;
                                        if (i == 0)
                                        {
                                            if (a is not null)
                                            {
                                                x1 = a.Value.X1;
                                                y1 = a.Value.Y1;
                                            }
                                        }
                                        else if (i == arr.Length - 1)
                                        {
                                            x2 = a.Value.X2;
                                            y2 = a.Value.Y2;
                                        }
                                    }
                                    details.Rects.Add(Rect.FromCoords(x1, y1, x2, y2));
                                }
                                else
                                {
                                    details.Rects.Add(result.Value);
                                }

                            }

                            var invoiceDueDate = Regex.Match(line.Text, @"\b(DUE DATE)\W+");
                            if (invoiceDueDate.Success && string.IsNullOrEmpty(textExtraction.Invoice.Payment.DueDate))
                            {
                                var date = EntityRecognizer.RecognizeDate(line.Text).ToUpper();
                                if (string.IsNullOrEmpty(date)) continue;
                                textExtraction.Invoice.Payment.DueDate = date;
                               // Console.WriteLine("Due date :" + date);
                                var dueDate = line.Words.SingleOrDefault(x => x.Text.Equals(date, StringComparison.OrdinalIgnoreCase))?.Coordinates;
                                if (dueDate is null)
                                {
                                    var arr = date.Split(' ');
                                    int x1 = 0, y1 = 0, x2 = 0, y2 = 0;
                                    for (int i = 0; i < arr.Length; i++)
                                    {
                                        var a = line.Words.SingleOrDefault(x => x.Text.Equals(arr[i], StringComparison.OrdinalIgnoreCase))?.Coordinates;
                                        if (i == 0)
                                        {
                                            if (a is not null)
                                            {
                                                x1 = a.Value.X1;
                                                y1 = a.Value.Y1;
                                            }
                                        }
                                        else if (i == arr.Length - 1)
                                        {
                                            x2 = a.Value.X2;
                                            y2 = a.Value.Y2;
                                        }
                                    }
                                    details.Rects.Add(Rect.FromCoords(x1, y1, x2, y2));
                                }
                                else
                                {
                                    details.Rects.Add(dueDate.Value);
                                }
                            }


                        }
                    }
                    if (details.Rects.Count > 0)
                    {
                        cords.Add(details);
                    }
                }
                //Console.WriteLine(JsonConvert.SerializeObject(textExtraction));
                //HighliightPdf(documents, cords);
                _dbHelper.InsertData(new ImageOcr
                {
                    Confidence = string.Format("{0:0.00}", documents.Confidence),
                    InputPath = Path.Combine(_inputPath, documents.FileName),
                    OutputPath = Path.Combine(_outputPath, documents.FileName),
                    OCRText = JsonConvert.SerializeObject(documents.Pages),
                    Output = JsonConvert.SerializeObject(textExtraction),
                });
            }
        }
        private void HighliightPdf(ProcessedPdf documents, List<DrawCoordinates> PageCords)
        {

            string path = Path.Combine(_inputPath, documents.FileName);
            string outputPath = Path.Combine(_outputPath, documents.FileName);
            float constant = 4.166666666666667f;

            PdfReader reader = new PdfReader(path);
            using FileStream fs = new FileStream(outputPath, FileMode.Create, FileAccess.Write, FileShare.None);
            using PdfStamper stamper = new(reader, fs);
            foreach (var page in PageCords)
            {
                try
                {
                    iTextSharp.text.Rectangle rectangle = reader.GetPageSize(page.PageNumber);

                    foreach (var rect in page.Rects)
                    {

                        float newX1 = rect.X1 / constant;
                        float newY1 = rectangle.Height - (rect.Y1 / constant);
                        float newX2 = rect.X2 / constant;
                        float newY2 = rectangle.Height - (rect.Y2 / constant);
                        //Create an array of quad points based on that rectangle. NOTE: The order below doesn't appear to match the actual spec but is what Acrobat produces
                        iTextSharp.text.Rectangle newRect = new iTextSharp.text.Rectangle(newX1, newY1, newX2, newY2);
                        float[] quad = { newRect.Right, newRect.Bottom, newRect.Left, newRect.Bottom, newRect.Right, newRect.Top, newRect.Left, newRect.Top };

                        //Create our hightlight
                        PdfAnnotation highlight = PdfAnnotation.CreateMarkup(stamper.Writer, newRect, null, PdfAnnotation.MARKUP_HIGHLIGHT, quad);

                        //Set the color
                        highlight.Color = iTextSharp.text.BaseColor.YELLOW;

                        //Add the annotation
                        stamper.AddAnnotation(highlight, page.PageNumber);
                    }
                }
                catch (Exception ex)
                {

                    _logger.LogError("Error in HighliightPdf {ex}", ex);
                }

                #region Pixel to Point concept
                // W:612 H:792 points, where 1 point contains = 1/72 = 0.0138888888888889 pixel here 72 is standard number for pdf documents
                // W:8.5 H: 11 inch
                // 1 inch = 72 pixel
                // Tiff image DPI = 300 means 300 pixel in 1 inch
                // 300 / 72 = 4.166666666666667 pixel
                // So pdf pixel size will be
                // W: 612 x 4.166666666666667 H:792 x 4.166666666666667
                // W : 2550 H: 3300 pixel

                // Now we have rectangle Coords as Pixel so to highlight it on pdf convert them to points
                // x1 = 1526 pixel / 4.166666666666667 = 326.24 points
                // y1 = 552 pixel / 4.166666666666667 = 132.48 points
                // x2 = 2308 pixel / 4.166666666666667 = 553.92 points
                //y2 = 603 pixel / 4.166666666666667 = 144.72 points

                //float w = (rectangle.Width * (300 / 72));
                //float x_scale = rectangle.Width / w;
                //float h = (rectangle.Height * (300 / 72));
                //float y_scale = rectangle.Height / h; 
                #endregion
            }

            GC.Collect();

            //File.Delete(Path.Combine(_inputPath, documents.FileName));
        }

        private void DeleteFiles(string[] pdfFiles)
        {
            foreach (var item in pdfFiles)
            {
                try
                {
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                    FileInfo file = new FileInfo(item);
                    file.Delete();
                }
                catch (Exception ex)
                {
                    _logger.LogError("Error in DeleteFiles {ex}", ex);
                }
            }
        }

        private class ProcessedPdf
        {
            public Guid DocumentId { get; set; }
            public string FileName { get; set; }
            public float Confidence { get; set; }
            public List<PageData> Pages { get; set; }
            public ProcessedPdf()
            {
                Pages = new();
            }
        }
        private class PageData
        {
            public int PageNumber { get; set; }
            public List<LineData> Lines { get; set; }
            public PageData()
            {
                Lines = new();
            }
        }
        private class LineData
        {
            public int LineNumber { get; set; }
            public Rect LineCoordinates { get; set; }
            public string Text { get; set; }
            public List<WordData> Words { get; set; }
            public LineData()
            {
                Words = new();
            }
        }
        private class WordData
        {
            public string Text { get; set; }
            public Tesseract.Rect Coordinates { get; set; }
        }
        private class DrawCoordinates
        {
            public int PageNumber { get; set; }
            public List<Rect> Rects { get; set; }
            public DrawCoordinates()
            {
                Rects = new();
            }

        }
        private bool Initialise()
        {
            try
            {
                string? enginePath = _config.GetValue<string>("Engine");
                string? testData = _config.GetValue<string>("TestDataPath");
                _engine = OCR.Image.LoadEnglishEngine(testData);
                //_crfClassifier = EntityRecognizer.LoadNLPEngine(enginePath);

               // if (_engine != null || _crfClassifier != null)
                if (_engine != null)
                {
                    _primarySerachKeys = _config.GetSection("SearchKeys").Get<string[]>();
                    _inputPath = _config.GetValue<string>("InputFolderPath");
                    _outputPath = _config.GetValue<string>("OutputFolderPath");
                    //_logger.LogInformation("OCR and NLP Engine Initialize successfully on {time}", DateTimeOffset.Now);
                    _logger.LogInformation("OCR Engine Initialize successfully on {time}", DateTimeOffset.Now);
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                //_logger.LogError("Failed to initialise OCR engine on  {time}", DateTimeOffset.Now);
                _logger.LogError("Engine Initialization failed {ex}", ex);
                return false;
            }
        }
    }
}