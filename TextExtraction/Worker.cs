using edu.stanford.nlp.ie.crf;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Emgu.CV.Util;
using iTextSharp.text.pdf;
using NameRecognizer;
using Newtonsoft.Json;
using System.Reflection;
using System.Reflection.Emit;
using Tesseract;
using TextExtraction.APIModels;
using TextExtraction.Initialization;
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
        private readonly IAPIService service;
        public CRFClassifier? _crfClassifier;
        private TesseractEngine? _engine;
        private readonly ILogger<Worker> _logger;
        private readonly IConfiguration _config;
        private ConfigurationSettings? _template;
        private Initializer _initializer;

        public Worker(ILogger<Worker> logger, IConfiguration config, DbHelper dbHelper, IAPIService service, Initializer initializer)
        {
            _logger = logger;
            _config = config;
            _dbHelper = dbHelper;
            this.service = service;
            _initializer = initializer;
        }
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("TextExtraction started at: {time}", DateTimeOffset.Now);
                _inputPath = _config.GetValue<string>("InputFolderPath");
                string[] pdfFiles = Directory.GetFiles(_inputPath, "*.pdf");
                _logger.LogInformation("Pdf count : {count}", pdfFiles.Length);
                if (pdfFiles.Length > 0)
                {
                    bool allGood = _initializer.Initialize(ref _crfClassifier, ref _engine, ref _template);
                    if (allGood)
                    {
                        _outputPath = _config.GetValue<string>("OutputFolderPath");
                        _ghostScriptPath = _config.GetValue<string>("GhostScript");
                        _primarySerachKeys = _config.GetSection("SearchKeys:PatientKeys").Get<string[]>();
                        if (!string.IsNullOrEmpty(_inputPath) && !string.IsNullOrEmpty(_outputPath))
                        {
                            RunOcr(pdfFiles);
                        }
                    }
                }
                // Wait for 1 minute before checking the input folder again
                await Task.Delay(TimeSpan.FromMinutes(10), stoppingToken);
            }
        }

        private bool Initialise()
        {
            try
            {

                bool extractPatientDetails = _config.GetValue<string>("ExtractPatientDetails").IsNullOrEmpty() ? false : System.Convert.ToBoolean(_config.GetValue<string>("ExtractPatientDetails"));
                string temaplateId = _config.GetValue<string>("TemaplateId").IsNullOrEmpty() ? "" : _config.GetValue<string>("TemaplateId");

                var request = new LoginRequest { UserNameOrEmailAddress = "serviceuser", Password = "1q2w3E*", RememberMe = true };

                LoginResponse? result = service.PostAsync<LoginRequest, LoginResponse?>("api/account/login", request).GetAwaiter().GetResult();

                if (result is not null && result.Description.Equals("Success"))
                {
                    _template = service.GetAsync<ConfigurationSettings>($"api/app/config/{temaplateId}").GetAwaiter().GetResult();
                    if (_template is null)
                    {
                        _logger.LogError("No Template found for TemplateId {temaplateId}", temaplateId);
                        //_template = _dbHelper.GetTemplateById(temaplateId);
                    }
                    else
                    {
                        _logger.LogInformation("Template recovered from API tamplate ID: {temaplateId}", temaplateId);
                    }
                }
                else
                {
                    _logger.LogError("Login API failed");
                }

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
            foreach (var documents in textData)
            {
                List<DrawCoordinates> cords = new();
                TextExtractionFields textExtraction = new TextExtractionFields();

                foreach (var addField in _template.Fields)
                {
                    var newProp = GetObject(addField.FieldName);
                    textExtraction.Invoice.AdditionalFields.Add(newProp);

                }

                foreach (var page in documents.Pages)
                {
                    var details = new DrawCoordinates { PageNumber = page.PageNumber };
                    foreach (var line in page.Lines)
                    {
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
                    var request = new ImageOcr
                    {
                        Confidence = string.Format("{0:0.00}", documents.Confidence),
                        InputPath = Path.Combine(_inputPath, documents.FileName),
                        OutputPath = Path.Combine(_outputPath, documents.FileName),
                        OCRText = JsonConvert.SerializeObject(documents.Pages),
                        Output = JsonConvert.SerializeObject(textExtraction),
                    };

                    ImageOcrResponse? result = service.PostAsync<ImageOcr, ImageOcrResponse?>("api/app/ocr", request).GetAwaiter().GetResult();
                    if (result is not null)
                    {
                        _logger.LogInformation("OCR and extraction id done for {FileName}, Its Id is : {id} and created at : {time}", documents.FileName, result.id, result.creationTime);
                    }
                    else
                    {
                        _logger.LogError("Something is wrong with OCR API, Check FaxVerification Sites logs for more details.");
                    }

                    //_dbHelper.InsertData(new ImageOcr
                    //{
                    //    Confidence = string.Format("{0:0.00}", documents.Confidence),
                    //    InputPath = Path.Combine(_inputPath, documents.FileName),
                    //    OutputPath = Path.Combine(_outputPath, documents.FileName),
                    //    OCRText = JsonConvert.SerializeObject(documents.Pages),
                    //    Output = JsonConvert.SerializeObject(textExtraction),
                    //});
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

                foreach (var addField in _template.Fields)
                {
                    var newProp = GetObject(addField.FieldName);
                    textExtraction.Invoice.AdditionalFields.Add(newProp);
                }
                foreach (var page in documents.Pages)
                {
                    //var details = new DrawCoordinates { PageNumber = page.PageNumber };

                    foreach (var line in page.Lines)
                    {
                        _primarySerachKeys = _primarySerachKeys.Select(x => x.ToUpper()).ToArray();

                        line.Text = line.Text.ToUpper();

                        #region Jugad code may be delete later
                        //string? orgName = EntityRecognizer.GetOrganizationName(line.Text, _crfClassifier);
                        //if (!string.IsNullOrEmpty(orgName)) //&& string.IsNullOrEmpty(textExtraction.Invoice.Supplier.CompanyName))
                        //{
                        //    var reallyAOrg = Regex.Match(orgName, @"\b\W+INC|LLC$\b");
                        //    if (reallyAOrg.Success && string.IsNullOrEmpty(textExtraction.Invoice.Supplier.CompanyName))
                        //    {
                        //        textExtraction.Invoice.Supplier.CompanyName = orgName;
                        //    } 
                        //} 
                        #endregion

                        if (_primarySerachKeys.Any(line.Text.Contains))
                        {
                            if (string.IsNullOrEmpty(textExtraction.Invoice.InvNum.Text))
                            {
                                bool flag = InvoiceNumber.Extract(line, page.PageNumber, textExtraction.Invoice.InvNum /*,details.Rects*/);
                                if (flag)
                                {
                                    continue;
                                }
                            }

                            if (string.IsNullOrEmpty(textExtraction.Invoice.InvDate.Text))
                            {
                                bool flag = InvoiceDate.Extract(line, page.PageNumber, textExtraction.Invoice.InvDate /*,details.Rects,*/);
                                if (flag)
                                {
                                    continue;
                                }
                            }

                            if (string.IsNullOrEmpty(textExtraction.Invoice.OrderNum.Text))
                            {
                                bool flag = PurchaseOrder.Extract(line, page.PageNumber, textExtraction.Invoice.OrderNum /*,details.Rects,*/);
                                if (flag)
                                {
                                    continue;
                                }
                            }

                            if (string.IsNullOrEmpty(textExtraction.Invoice.OrderDate.Text))
                            {
                                bool flag = PurchaseOrderDate.Extract(line, page.PageNumber, textExtraction.Invoice.OrderDate /*,details.Rects,*/);
                                if (flag)
                                {
                                    continue;
                                }
                            }

                            if (string.IsNullOrEmpty(textExtraction.Invoice.GrossAmount.Text))
                            {
                                bool flag = GrossAmount.Extract(line, page.PageNumber, textExtraction.Invoice.GrossAmount /*, details.Rects,*/);
                                if (flag)
                                {
                                    continue;
                                }
                            }

                            if (string.IsNullOrEmpty(textExtraction.Invoice.InvoiceAmount.Text))
                            {
                                bool flag = GrossAmount.Extract(line, page.PageNumber, textExtraction.Invoice.InvoiceAmount /*, details.Rects,*/);
                                if (flag)
                                {
                                    continue;
                                }
                            }

                            if (string.IsNullOrEmpty(textExtraction.Invoice.TaxAmount.Text))
                            {
                                bool flag = Tax.Extract(line, page.PageNumber, textExtraction.Invoice.TaxAmount /*, details.Rects,*/);
                                if (flag)
                                {
                                    continue;
                                }
                            }

                            if (string.IsNullOrEmpty(textExtraction.Invoice.VendorNum.Text))
                            {
                                bool flag = VenderNumber.Extract(line, page.PageNumber, textExtraction.Invoice.VendorNum /*, details.Rects,*/);
                                if (flag)
                                {
                                    continue;
                                }
                            }



                            #region Payment Due date
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
                            #endregion
                        }
                    }
                    //if (details.Rects.Count > 0)
                    //{
                    //    cords.Add(details);
                    //}

                    foreach (var newField in textExtraction.Invoice.AdditionalFields)
                    {
                        string prop = GetProperty(newField, "Text");
                        if (string.IsNullOrEmpty(prop))
                        {
                            string fieldName = GetProperty(newField, "FieldName");
                            switch (fieldName)
                            {
                                case "Vendor Name":
                                    SetProperty(newField, "Text", textExtraction.Invoice.VendorName.Text);
                                    SetProperty(newField, "Rectangle", textExtraction.Invoice.VendorName.Rectangle);
                                    SetProperty(newField, "PageNumber", textExtraction.Invoice.VendorName.PageNumber);
                                    break;
                                case "Invoice No":
                                    SetProperty(newField, "Text", textExtraction.Invoice.InvNum.Text);
                                    SetProperty(newField, "Rectangle", textExtraction.Invoice.InvNum.Rectangle);
                                    SetProperty(newField, "PageNumber", textExtraction.Invoice.InvNum.PageNumber);
                                    break;
                                case "PO Type":
                                    //SetProperty(newField, "Text", textExtraction.Invoice.InvNum.Text);
                                    //SetProperty(newField, "Rectangle", textExtraction.Invoice.InvNum.Rectangle);
                                    //SetProperty(newField, "PageNumber", textExtraction.Invoice.InvNum.PageNumber);
                                    break;
                                case "Due Dt":
                                    SetProperty(newField, "Text", textExtraction.Invoice.OrderDate.Text);
                                    SetProperty(newField, "Rectangle", textExtraction.Invoice.OrderDate.Rectangle);
                                    SetProperty(newField, "PageNumber", textExtraction.Invoice.OrderDate.PageNumber);
                                    break;
                                case "Vendor No":
                                    SetProperty(newField, "Text", textExtraction.Invoice.VendorNum.Text);
                                    SetProperty(newField, "Rectangle", textExtraction.Invoice.VendorNum.Rectangle);
                                    SetProperty(newField, "PageNumber", textExtraction.Invoice.VendorNum.PageNumber);
                                    break;
                                case "Gross Amount":
                                    SetProperty(newField, "Text", textExtraction.Invoice.GrossAmount.Text);
                                    SetProperty(newField, "Rectangle", textExtraction.Invoice.GrossAmount.Rectangle);
                                    SetProperty(newField, "PageNumber", textExtraction.Invoice.GrossAmount.PageNumber);
                                    break;
                                case "Tax Amount":
                                    SetProperty(newField, "Text", textExtraction.Invoice.TaxAmount.Text);
                                    SetProperty(newField, "Rectangle", textExtraction.Invoice.TaxAmount.Rectangle);
                                    SetProperty(newField, "PageNumber", textExtraction.Invoice.TaxAmount.PageNumber);
                                    break;
                                case "PO Number":
                                    SetProperty(newField, "Text", textExtraction.Invoice.OrderNum.Text);
                                    SetProperty(newField, "Rectangle", textExtraction.Invoice.OrderNum.Rectangle);
                                    SetProperty(newField, "PageNumber", textExtraction.Invoice.OrderNum.PageNumber);
                                    break;
                                case "Inv Amount":
                                    SetProperty(newField, "Text", textExtraction.Invoice.InvoiceAmount.Text);
                                    SetProperty(newField, "Rectangle", textExtraction.Invoice.InvoiceAmount.Rectangle);
                                    SetProperty(newField, "PageNumber", textExtraction.Invoice.InvoiceAmount.PageNumber);
                                    break;
                                case "Invoice Dt":
                                    SetProperty(newField, "Text", textExtraction.Invoice.InvDate.Text);
                                    SetProperty(newField, "Rectangle", textExtraction.Invoice.InvDate.Rectangle);
                                    SetProperty(newField, "PageNumber", textExtraction.Invoice.InvDate.PageNumber);
                                    break;
                                default:
                                    //string regex = _template.Fields.FirstOrDefault(x => x.FieldName.Equals(fieldName)).RegExpression;
                                    //var additionalFields = Regex.Match(line.Text, regex);
                                    //if (additionalFields.Success)
                                    //{
                                    //    SetProperty(newField, "Text", additionalFields.Value);
                                    //    string rectangle = Helper.ConvertToPdfPoints(line.LineCoordinates);
                                    //    SetProperty(newField, "Rectangle", rectangle);
                                    //    SetProperty(newField, "PageNumber", page.PageNumber);
                                    //}
                                    break;
                            }
                        }
                    }
                }

                //Extract table
                var table = new List<Table>();
                var cells = new List<System.Drawing.Rectangle>();
                string imagePath = Path.Combine(Directory.GetCurrentDirectory(), Path.GetFileNameWithoutExtension(documents.FileName) + ".tiff");
                var im = new Emgu.CV.Image<Bgr, byte>(imagePath);
                var gray = im.Convert<Gray, byte>();
                var binary = gray.ThresholdBinary(new Gray(100), new Gray(155));
                //var cells = new List<System.Drawing.Rectangle>();
                var contours = new VectorOfVectorOfPoint();
                CvInvoke.FindContours(binary, contours, null, RetrType.List, ChainApproxMethod.ChainApproxSimple);
                int cell =0;
                int index = 0;
                //filter out text contours by checking the size

                //int loop = contours.Size - 1;
                //for (int k = loop; k > 0; k--)
                for (int k = 0; k < contours.Size ; k++)
                {
                    //get the area of the contour
                    var area = CvInvoke.ContourArea(contours[k]);

                    // filter out text contours using the area
                    if (area > 5000 && area < 200000)
                    {
                        //check if the shape of the contour is a square or a rectangle
                        var rect = CvInvoke.BoundingRectangle(contours[k]);
                        //var aspectRatio = (double)rect.Width / rect.Height;
                        //cell++;
                        //if (cell >= 10 && cell <= 44)
                        //{
                            //add the cell to the list
                            cells.Add(rect);
                            //Rect roi = new Rect(rect.X, rect.Y, rect.Width, rect.Height);
                            //using var image = Pix.LoadFromFile(imagePath);
                            //using (var page = _engine.Process(image, roi))
                            //{
                            //    string txt = page.GetText().Trim();
                            //    if(txt.IsNullOrEmpty())
                            //    {
                            //        continue;
                            //    }
                            //    textExtraction.Invoice.TableDetails.Add(new Table { CellNo = index++, Text = txt });
                                
                            //    var lst = textExtraction.Invoice.TableDetails.
                                
                            //}
                        //}
                    }
                }


                bool testing = _config.GetValue<string>("Testing").IsNullOrEmpty() ? false : System.Convert.ToBoolean(_config.GetValue<string>("Testing"));
                if (testing)
                {
                    _logger.LogInformation("Output: {textExtraction}", JsonConvert.SerializeObject(textExtraction));
                }
                else
                {
                    var request = new ImageOcr
                    {
                        Confidence = string.Format("{0:0.00}", documents.Confidence),
                        InputPath = Path.Combine(_inputPath, documents.FileName),
                        OutputPath = Path.Combine(_outputPath, documents.FileName),
                        OCRText = JsonConvert.SerializeObject(documents.Pages),
                        Output = JsonConvert.SerializeObject(textExtraction)
                    };
                    ImageOcrResponse? result = service.PostAsync<ImageOcr, ImageOcrResponse?>("api/app/ocr", request).GetAwaiter().GetResult();
                    if (result is not null && result.id != string.Empty)
                    {
                        _logger.LogInformation("OCR and extraction id done for {FileName}, Its Id is : {id} and created at : {time}", documents.FileName, result.id, result.creationTime);
                        HighliightPdf(documents, cords);
                    }
                    else
                    {
                        _logger.LogError("Something is wrong with OCR API, Check FaxVerification Sites logs for more details.");
                    }
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

                File.Delete(Path.Combine(_inputPath, documents.FileName));
            }
            if (File.Exists(Path.Combine(_outputPath, documents.FileName)))
            {
                _logger.LogInformation("{filename} already exist to path : {path}", documents.FileName, _outputPath);
            }
            else
            {
                File.Move(Path.Combine(_inputPath, documents.FileName), Path.Combine(_outputPath, documents.FileName));
                _logger.LogInformation("{filename} move to path : {path}", documents.FileName, _outputPath);
            }
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

        private dynamic GetObject(string propertyName)
        {
            // Create a dynamic object similar to InvoiceNumber
            var dynamicObject = CreateDynamicObject(propertyName, typeof(DynamicObject));

            //// Set values for the properties of the dynamic object
            SetProperty(dynamicObject, "FieldName", propertyName);
            SetProperty(dynamicObject, "Text", "");
            SetProperty(dynamicObject, "PageNumber", 0);
            SetProperty(dynamicObject, "Rectangle", "");

            //// Access the values from the dynamic object
            //var text = GetProperty(dynamicObject, "Text");
            //var pageNumber = GetProperty(dynamicObject, "PageNumber");
            //var rectangle = GetProperty(dynamicObject, "Rectangle");

            return dynamicObject;
        }

        public static object CreateDynamicObject(string typeName, System.Type baseType)
        {
            var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("DynamicAssembly"), AssemblyBuilderAccess.Run);

            var moduleBuilder = assemblyBuilder.DefineDynamicModule("DynamicModule");

            var typeBuilder = moduleBuilder.DefineType(typeName, TypeAttributes.Public, baseType);

            var dynamicType = typeBuilder.CreateType();

            var dynamicObject = Activator.CreateInstance(dynamicType);

            return dynamicObject;
        }

        public static void SetProperty(object obj, string propertyName, object value)
        {
            var property = obj.GetType().GetProperty(propertyName);
            if (property != null && property.CanWrite)
            {
                property.SetValue(obj, Convert.ChangeType(value, property.PropertyType));
            }
        }

        public static object GetProperty(object obj, string propertyName)
        {
            var property = obj.GetType().GetProperty(propertyName);
            if (property != null && property.CanRead)
            {
                return property.GetValue(obj);
            }
            return null;
        }

    }

    public class DynamicObject
    {
        public string FieldName { get; set; }
        public string Text { get; set; }
        public int PageNumber { get; set; }
        public string Rectangle { get; set; }
    }
}