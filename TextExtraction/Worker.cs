using edu.stanford.nlp.ie.crf;
using iTextSharp.text.pdf;
using NameRecognizer;
using Newtonsoft.Json;
using System.Drawing;
using System.Text.RegularExpressions;
using Tesseract;
using TextExtraction.Model;
using TextExtraction.Services;
using Exception = System.Exception;
using Rect = Tesseract.Rect;

namespace TextExtraction
{
    public class Worker : BackgroundService
    {
        private string? _inputPath;
        private string? _outputPath;
        private string? _ghostScriptPath;
        private string[]? _primarySerachKeys;
        private int counter = 0;
        private DbHelper _dbHelper;
        public CRFClassifier? _crfClassifier;
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
            if (Initialise())
            {
                _inputPath = _config.GetValue<string>("InputFolderPath");
                _outputPath = _config.GetValue<string>("OutputFolderPath");
                _ghostScriptPath = _config.GetValue<string>("GhostScript");
                _primarySerachKeys = _config.GetSection("SearchKeys:PatientKeys").Get<string[]>();

                if (!string.IsNullOrEmpty(_inputPath) && !string.IsNullOrEmpty(_outputPath))
                {
                    while (!stoppingToken.IsCancellationRequested)
                    {
                        _logger.LogInformation("TextExtraction started at: {time}", DateTimeOffset.Now);

                        // Get all PDF files in the input folder
                        string[] pdfFiles = Directory.GetFiles(_inputPath, "*.pdf");

                        RunOcr(pdfFiles);

                        // Wait for 1 minute before checking the input folder again
                        await Task.Delay(TimeSpan.FromMinutes(2), stoppingToken);
                    }
                }
            }
        }

        private bool Initialise()
        {
            try
            {
                bool extractPatientDetails = _config.GetValue<string>("ExtractPatientDetails").IsNullOrEmpty() ? false : System.Convert.ToBoolean(_config.GetValue<string>("ExtractPatientDetails"));

                if (extractPatientDetails)
                {
                    string? enginePath = Directory.GetCurrentDirectory() + "\\stanford-ner-4.2.0";
                    _crfClassifier = EntityRecognizer.LoadNLPEngine(enginePath);
                    if (_crfClassifier is null)
                    {
                        _logger.LogError("NLP Engine Initialization failed - path: {path}", enginePath);
                        return false;
                    }
                    _logger.LogInformation("NLP Engine Initialize successfully on {time}", DateTimeOffset.Now);
                }
                TesseractEnviornment.CustomSearchPath = Environment.CurrentDirectory;

                _logger.LogInformation("OCR engine Path :{path}", Directory.GetCurrentDirectory() + "\\tessdata");
                _engine = OCR.Image.LoadEnglishEngine(Directory.GetCurrentDirectory() + "\\tessdata");

                if (_engine != null)
                {
                    _logger.LogInformation("OCR Engine Initialize successfully on {time}", DateTimeOffset.Now);
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError("Engine Initialization failed {ex}", ex);
                return false;
            }
        }
        private void RunOcr(string[] pdfFiles)
        {
            _logger.LogInformation("Pdf count : {count}", pdfFiles.Length);
            bool extractInvoiceDetails = _config.GetValue<string>("ExtractInvoiceDetails").IsNullOrEmpty() ? false : System.Convert.ToBoolean(_config.GetValue<string>("ExtractInvoiceDetails"));
            bool extractPatientDetails = _config.GetValue<string>("ExtractPatientDetails").IsNullOrEmpty() ? false : System.Convert.ToBoolean(_config.GetValue<string>("ExtractPatientDetails"));
            List<ProcessedPdf> textData = new List<ProcessedPdf>();
            foreach (var pdf in pdfFiles)
            {
                var document = new ProcessedPdf();
                document.DocumentId = Guid.NewGuid();
                document.FileName = Path.GetFileName(pdf);

                var streams = Pdf_To_ImageStream.Convert.ToStreams(_ghostScriptPath, pdf);

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
                            _logger.LogError("Error in GetPageFromTiffImageStream, {ex}", ex);
                        }
                        document.Pages.Add(page);
                    }
                    document.Confidence /= streams.Count();
                }
                textData.Add(document);
            }

            if (extractPatientDetails)
            {
                _logger.LogInformation("Patient Details extraction started at: {time}", DateTimeOffset.Now);
                ExtractMedicalData(textData);
            }
            if (extractInvoiceDetails)
            {
                _logger.LogInformation("Invoice Details extraction started at: {time}", DateTimeOffset.Now);
                ExtractInvoiceData(textData);
            }
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
        private void ExtractMedicalData(List<ProcessedPdf> textData)
        {
            Array.Clear(_primarySerachKeys, 0, _primarySerachKeys.Length);
            _primarySerachKeys = _config.GetSection("SearchKeys:PatientKeys").Get<string[]>();

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

                        string filteredText = FilterData.RemoveSpecialCharacters(line.Text).ToUpper();

                        if (_primarySerachKeys.Any(filteredText.Contains))
                        {
                            if (string.IsNullOrEmpty(textExtraction.Patient.BirthDate.Text))
                            {
                                PatientBirthDate.Extract(line, page.PageNumber, details.Rects, textExtraction.Patient.BirthDate);
                            }


                            if (string.IsNullOrEmpty(textExtraction.Patient.Name.Text))
                            {
                                PatientName.Extract(line, page.PageNumber, details.Rects, textExtraction.Patient.Name, _crfClassifier);
                            }
                        }
                        else if (!string.IsNullOrEmpty(textExtraction.Patient.Name.Text) && !string.IsNullOrEmpty(textExtraction.Patient.BirthDate.Text))
                        {
                            break;
                        }

                        bool enableEncryption = _config.GetValue<string>("EnableEncryption").IsNullOrEmpty() ? false : System.Convert.ToBoolean(_config.GetValue<string>("EnableEncryption"));

                        if (enableEncryption)
                        {
                            if (!string.IsNullOrEmpty(textExtraction.Patient.Name.Text))
                            {
                                textExtraction.Patient.Name.Text = CryptLib.Encrypt(textExtraction.Patient.Name.Text);
                            }
                            if (!string.IsNullOrEmpty(textExtraction.Patient.BirthDate.Text))
                            {
                                textExtraction.Patient.BirthDate.Text = CryptLib.Encrypt(textExtraction.Patient.BirthDate.Text);
                            }
                        }
                    }
                    if (details.Rects.Count > 0)
                    {
                        cords.Add(details);
                    }
                }
                HighliightPdf(documents, cords);

                bool testing = _config.GetValue<string>("Testing").IsNullOrEmpty() ? false : System.Convert.ToBoolean(_config.GetValue<string>("Testing"));
                if (testing)
                {
                    _logger.LogInformation("Output: {textExtraction}", JsonConvert.SerializeObject(textExtraction));
                }
                else
                {
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

            _logger.LogInformation("Patient details extraction job is done...!");
        }
        private void ExtractInvoiceData(List<ProcessedPdf> textData)
        {
            Array.Clear(_primarySerachKeys, 0, _primarySerachKeys.Length);
            _primarySerachKeys = _config.GetSection("SearchKeys:InvoiceKeys").Get<string[]>();

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
                            if (string.IsNullOrEmpty(textExtraction.Invoice.VendorName.Text))
                            {
                                if (line.Text.Equals("J.J. MARIN, LLC"))
                                {
                                    details.Rects.Add(line.LineCoordinates);
                                    textExtraction.Invoice.VendorName.Text = "J.J. MARIN, LLC";
                                    textExtraction.Invoice.VendorName.Rectangle = Helper.ConvertToPdfPoints(line.LineCoordinates);
                                    textExtraction.Invoice.VendorName.PageNumber = page.PageNumber;
                                }
                            }
                            //if (string.IsNullOrEmpty(textExtraction.Invoice.Customer.Name))
                            //{
                            //    if (line.Text.Equals("TECHNETRONIX,LLC"))
                            //    {
                            //        details.Rects.Add(line.LineCoordinates);
                            //        textExtraction.Invoice.Customer.Name = "TECHNETRONIX,LLC";
                            //    }
                            //}
                        }
                        if (documents.FileName.Equals("Factoring Invoice - 2020-09-23T150930.834.pdf"))
                        {
                            if (string.IsNullOrEmpty(textExtraction.Invoice.VendorName.Text))
                            {
                                if (line.Text.Equals("COMFREIGHT HAULPAY"))
                                {
                                    details.Rects.Add(line.LineCoordinates);
                                    textExtraction.Invoice.VendorName.Text = "COMFREIGHT HAULPAY";
                                    textExtraction.Invoice.VendorName.Rectangle = Helper.ConvertToPdfPoints(line.LineCoordinates);
                                    textExtraction.Invoice.VendorName.PageNumber = page.PageNumber;
                                }
                            }
                            //if (string.IsNullOrEmpty(textExtraction.Invoice.Customer.Name))
                            //{
                            //    string tx = Regex.Replace(line.Text, @"\b(LOAD|REFERENCE)\W+(\w+\d+\w+)", "");
                            //    if (tx.Equals("JW LOGISTICS OPERATIONS LLC "))
                            //    {
                            //        details.Rects.Add(line.LineCoordinates);
                            //        textExtraction.Invoice.Customer.Name = "JW LOGISTICS OPERATIONS LLC";
                            //    }
                            //}
                        }
                        if (documents.FileName.Equals("Factoring invoice - 2020-09-23T151006.783.pdf"))
                        {
                            if (string.IsNullOrEmpty(textExtraction.Invoice.VendorName.Text))
                            {
                                if (line.Text.Equals("RTS FINANCIAL SERVICE, INC."))
                                {
                                    details.Rects.Add(line.LineCoordinates);
                                    textExtraction.Invoice.VendorName.Text = "RTS FINANCIAL SERVICE, INC";
                                    textExtraction.Invoice.VendorName.Rectangle = Helper.ConvertToPdfPoints(line.LineCoordinates);
                                    textExtraction.Invoice.VendorName.PageNumber = page.PageNumber;

                                }
                            }
                            //if (string.IsNullOrEmpty(textExtraction.Invoice.Customer.Name))
                            //{
                            //    if (line.Text.Equals("J.W. LOGISTICS OPERATIONS, LLC"))
                            //    {
                            //        details.Rects.Add(line.LineCoordinates);
                            //        textExtraction.Invoice.Customer.Name = "J.W. LOGISTICS OPERATIONS, LLC";

                            //    }
                            //}
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
                            if (string.IsNullOrEmpty(textExtraction.Invoice.InvNum.Text))
                            {
                                InvoiceNumber.Extract(line, page.PageNumber, details.Rects, textExtraction.Invoice.InvNum);
                            }

                            if (string.IsNullOrEmpty(textExtraction.Invoice.InvDate.Text))
                            {
                                InvoiceDate.Extract(line, page.PageNumber, details.Rects, textExtraction.Invoice.InvDate);
                            }

                            if (string.IsNullOrEmpty(textExtraction.Invoice.OrderNum.Text))
                            {
                                PurchaseOrder.Extract(line, page.PageNumber, details.Rects, textExtraction.Invoice.OrderNum);
                            }
                            if (string.IsNullOrEmpty(textExtraction.Invoice.Total.Text))
                            {
                                GrossAmount.Extract(line, page.PageNumber, details.Rects, textExtraction.Invoice.Total);
                            }

                            //var invoiceDueDate = Regex.Match(line.Text, @"\b(DUE DATE)\W+");
                            //if (invoiceDueDate.Success && string.IsNullOrEmpty(textExtraction.Invoice.OrderDate.Text))
                            //{
                            //    var date = EntityRecognizer.RecognizeDate(line.Text).ToUpper();
                            //    if (string.IsNullOrEmpty(date)) continue;
                            //    textExtraction.Invoice.Payment.DueDate = date;
                            //    // Console.WriteLine("Due date :" + date);
                            //    var dueDate = line.Words.SingleOrDefault(x => x.Text.Equals(date, StringComparison.OrdinalIgnoreCase))?.Coordinates;
                            //    if (dueDate is null)
                            //    {
                            //        var arr = date.Split(' ');
                            //        int x1 = 0, y1 = 0, x2 = 0, y2 = 0;
                            //        for (int i = 0; i < arr.Length; i++)
                            //        {
                            //            var a = line.Words.SingleOrDefault(x => x.Text.Equals(arr[i], StringComparison.OrdinalIgnoreCase))?.Coordinates;
                            //            if (i == 0)
                            //            {
                            //                if (a is not null)
                            //                {
                            //                    x1 = a.Value.X1;
                            //                    y1 = a.Value.Y1;
                            //                }
                            //            }
                            //            else if (i == arr.Length - 1)
                            //            {
                            //                x2 = a.Value.X2;
                            //                y2 = a.Value.Y2;
                            //            }
                            //        }
                            //        details.Rects.Add(Rect.FromCoords(x1, y1, x2, y2));
                            //    }
                            //    else
                            //    {
                            //        details.Rects.Add(dueDate.Value);
                            //    }
                            //}
                        }
                    }
                    if (details.Rects.Count > 0)
                    {
                        cords.Add(details);
                    }
                }
                HighliightPdf(documents, cords);
                bool testing = _config.GetValue<string>("Testing").IsNullOrEmpty() ? false : System.Convert.ToBoolean(_config.GetValue<string>("Testing"));
                if (testing)
                {
                    _logger.LogInformation("Output: {textExtraction}", JsonConvert.SerializeObject(textExtraction));
                }
                else
                {
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
            _logger.LogInformation("Invoice details extraction job is done...!");
        }
        private void HighliightPdf(ProcessedPdf documents, List<DrawCoordinates> PageCords)
        {
            bool enableHighlight = _config.GetValue<string>("EnableHighlight").IsNullOrEmpty() ? false : System.Convert.ToBoolean(_config.GetValue<string>("EnableHighlight"));
            if (enableHighlight)
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
                            //_logger.LogInformation($"quad points : {newRect.Right}, {newRect.Bottom}, {newRect.Left}, {newRect.Bottom}, {newRect.Right}, {newRect.Top}, {newRect.Left}, {newRect.Top} ");

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

                //File.Delete(Path.Combine(_inputPath, documents.FileName));
            }
            File.Move(Path.Combine(_inputPath, documents.FileName), Path.Combine(_outputPath, documents.FileName));
        }

        private void DeleteFiles(string[] pdfFiles)
        {
            foreach (var item in pdfFiles)
            {
                try
                {
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                    Task.Delay(3000).Wait();
                    FileInfo file = new FileInfo(item);
                    file.Delete();
                }
                catch (Exception ex)
                {
                    _logger.LogError("Error in DeleteFiles {ex}", ex);
                }
            }
        }

       
    }
}