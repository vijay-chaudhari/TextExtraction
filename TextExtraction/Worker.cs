using com.sun.rowset.@internal;
using com.sun.xml.@internal.ws.api.pipe;
using edu.stanford.nlp.ie.crf;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Emgu.CV.Util;
using iTextSharp.text.pdf;
using Microsoft.EntityFrameworkCore.Storage;
using NameRecognizer;
using Newtonsoft.Json;
using System.Drawing;
using System.Reflection;
using System.Reflection.Emit;
using System.Reflection.Metadata.Ecma335;
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
        private string? _pendingPath;
        private string? _ghostScriptPath;
        private string _tempFolder = Directory.GetCurrentDirectory() + "\\Temp";
        private readonly ILogger<Worker> _logger;
        private readonly IConfiguration _config;
        private bool _isUserLoggedIn;
        private Initializer _initializer;
        private TesseractEngine? _engine;
        public CRFClassifier? _crfClassifier;

        public Worker(ILogger<Worker> logger, IConfiguration config, Initializer initializer)
        {
            _logger = logger;
            _config = config;
            _initializer = initializer;
        }
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("TextExtraction started at: {time}", DateTimeOffset.Now);
                _inputPath = _config.GetValue<string>("InputFolderPath");
                _outputPath = _config.GetValue<string>("OutputFolderPath");
                _pendingPath = _config.GetValue<string>("PendingPath");
                _ghostScriptPath = _config.GetValue<string>("GhostScript");
                if (Directory.Exists(_inputPath) && Directory.Exists(_outputPath) && Directory.Exists(_pendingPath) && Directory.Exists(_ghostScriptPath))
                {
                    _logger.LogInformation("Directory paths mentioned in AppSettings.json file does not exist. Please verify all folder paths..!");
                    break;
                }

                string[] pdfFiles = Directory.GetFiles(_inputPath, "*.pdf");
                _logger.LogInformation("Pdf count : {count}", pdfFiles.Length);

                if (pdfFiles.Length > 0)
                {
                    _engine = _initializer.InitOCR();
                    _isUserLoggedIn = _initializer.Login();
                    _logger.LogInformation("Service user logged in : {flag}", _isUserLoggedIn);
                    if (_engine is not null)
                        RunOcr(pdfFiles);
                }
                // Wait for 10 minute before checking the input folder again
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
        }

        private void RunOcr(string[] pdfFiles)
        {
            foreach (var pdf in pdfFiles)
            {
                double pageConfidence = 0;
                int totalFields = 0;
                string fileName = Path.GetFileName(pdf);
                string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(pdf);
                var fileNumber = fileName.Split('_')[0];

                if (!string.IsNullOrEmpty(fileNumber))
                {
                    string vendorId = _initializer.GetVendorID(fileNumber);
                    if (string.IsNullOrEmpty(vendorId))
                    {
                        if (MoveFile(_inputPath, _pendingPath, fileName))
                        {
                            _logger.LogInformation("No vendor id found. File : {filename} move to location : {path}", fileName, _pendingPath);

                            NewFile? resp = _initializer.SentToRegistration(new NewFile
                            {
                                id = Guid.NewGuid(),
                                filePath = Path.Combine(_pendingPath, fileName)
                            });

                            if (resp is not null)
                            {
                                _logger.LogInformation("{FileName} sent for registration, Its Id is : {id} and created at : {time}", fileName, resp.id, resp.creationTime);
                            }
                            else
                            {
                                _logger.LogError("Something is wrong with Registration API, Data is not saved, Check FaxVerification Sites logs for more details.");
                            }
                            continue;
                        }
                        continue;
                    }

                    Registration? registration = _initializer.GetRegisterTemplate(vendorId);
                    if (registration is null)
                    {

                        if (MoveFile(_inputPath, _pendingPath, fileName))
                        {
                            _logger.LogInformation("No registered template found, File: {filename} move to location : {path}", fileName, _pendingPath);

                            NewFile? resp = _initializer.SentToRegistration(new NewFile
                            {
                                id = Guid.NewGuid(),
                                filePath = Path.Combine(_pendingPath, fileName)
                            });

                            if (resp is not null)
                            {
                                _logger.LogInformation("{FileName} sent for registration, Its Id is : {id} and created at : {time}", fileName, resp.id, resp.creationTime);
                            }
                            else
                            {
                                _logger.LogError("Something is wrong with Registration API, Data is not saved, Check FaxVerification Sites logs for more details.");
                            }
                        }
                        continue;
                    }
                    var streams = Pdf_To_ImageStream.Convert.ToStreams(_ghostScriptPath, pdf);

                    if (streams is not null)
                    {
                        TextExtractionFields textExtraction = new TextExtractionFields();
                        try
                        {
                            var rootObj = JsonConvert.DeserializeObject<RegisterdTemplate>(registration.OCRConfig);
                            if (rootObj is not null)
                            {
                                foreach (var item in rootObj.AdditionalFields)
                                {
                                    if (item.PageNumber <= 0)
                                    {
                                        continue;
                                    }
                                    int pno = item.PageNumber - 1;
                                    Rect rect = ConvertCords(item.Rectangle);
                                    if (rect.Width <= 0 || rect.Height <= 0)
                                        continue;
                                    if (item.Text == "Table" || item.FieldName == "TableCordinates")
                                    {
                                        Bitmap source = new Bitmap(streams[pno]);
                                        Rectangle section = new Rectangle(new Point(rect.X1, rect.Y1), new Size(rect.Width, rect.Height));
                                        Bitmap CroppedImage = CropImage(source, section);

                                        string imagePath = Path.Combine(_tempFolder, fileNameWithoutExtension + ".TIFF");
                                        string tableImagePath = Path.Combine(_tempFolder, fileNameWithoutExtension + "_table.TIFF");
                                        CroppedImage.Save(imagePath);
                                        CroppedImage.Save(tableImagePath);
                                        GetTable(textExtraction, imagePath, tableImagePath, pageConfidence, totalFields);
                                        continue;
                                    }
                                    using var image = Pix.LoadTiffFromMemory(streams[pno].ToArray());
                                    using var page = _engine.Process(image, rect);
                                    double conf = page.GetMeanConfidence() * 100;
                                    pageConfidence += conf;
                                    totalFields++;
                                    textExtraction.Invoice.AdditionalFields.Add(new Field
                                    {
                                        FieldName = item.FieldName,
                                        Text = page.GetText().Trim(),
                                        PageNumber = item.PageNumber,
                                        Confidence = conf,
                                        Rectangle = item.Rectangle
                                    });

                                }
                                _logger.LogInformation("Data extraction is Completed..!");
                                CopyToArchive(_inputPath, fileName);

                                var request = new ImageOcr
                                {
                                    Confidence = $"{pageConfidence / totalFields}",
                                    InputPath = Path.Combine(_inputPath, fileName),
                                    OutputPath = Path.Combine(_outputPath, fileName),
                                    OCRText = "",
                                    Output = JsonConvert.SerializeObject(textExtraction)
                                };

                                bool testing = _config.GetValue<string>("Testing").IsNullOrEmpty() ? false : System.Convert.ToBoolean(_config.GetValue<string>("Testing"));
                                if (testing)
                                {
                                    _logger.LogInformation("Output: {@textExtraction}", request);
                                    continue; ;
                                }

                                var isMoved = MoveFile(_inputPath, _outputPath, fileName);
                                if (isMoved)
                                {
                                    _logger.LogInformation("{filename} is processed and move to {path}", fileName, _outputPath);
                                    ImageOcrResponse resp = _initializer.SaveOCRData(request);
                                    if (resp is not null)
                                    {
                                        _logger.LogInformation("{FileName} data is saved Successfully, Its Id is : {id} and created at : {time}", fileName, resp.id, resp.creationTime);
                                    }
                                    else
                                    {
                                        _logger.LogError("Something is wrong with OCR API, Data is not saved, Check FaxVerification Sites logs for more details.");
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError("Error Occured..!\n {ex}", ex);
                        }
                    }
                }
                else
                {
                    _logger.LogInformation("Cannot fetch first 8 digits of file {fileName}, Skiped from extraction.", fileName);
                    continue;
                }
            }
        }

        private string? GetVendorID(string fileNumber)
        {
            //Here we will call an API which will eventually return a vendorId by sending the fileNumber
            List<FileKey> fileKeys = _config.GetSection("FileKey").Get<List<FileKey>>();

            foreach (var obj in fileKeys)
            {
                if (obj.Key.Equals(fileNumber))
                    return obj.VendorId;
            }
            return string.Empty;
        }

        public Rect ConvertCords(string cords)
        {
            try
            {
                string[] arr = cords.Split(',');

                double X = Convert.ToDouble(arr[0]);
                double Y = Convert.ToDouble(arr[1]);
                double W = Convert.ToDouble(arr[2]);
                double H = Convert.ToDouble(arr[3]);

                W = (W - X) * 4.166666666666667;
                H = (H - Y) * 4.166666666666667;
                X = X * 4.166666666666667;
                Y = Y * 4.166666666666667;
                int x = Convert.ToInt32(X);
                int y = Convert.ToInt32(Y);
                int w = Convert.ToInt32(W);
                int h = Convert.ToInt32(H);
                return new Rect(x, y, w, h);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error Converting cords\n {ex}");
            }
            return new Rect(); ;
        }

        public Bitmap CropImage(Bitmap source, Rectangle section)
        {
            var bitmap = new Bitmap(section.Width, section.Height);
            using (var g = Graphics.FromImage(bitmap))
            {
                g.DrawImage(source, 0, 0, section, GraphicsUnit.Pixel);
                return bitmap;
            }
        }

        public void GetTable(TextExtractionFields textExtraction, string imagePath, string tableImagePath, double pageConfidence, double totalFields)
        {
            try
            {
                var cells = new List<System.Drawing.Rectangle>();
                var im = new Emgu.CV.Image<Bgr, byte>(imagePath);
                var gray = im.Convert<Gray, byte>();
                var binary = gray.ThresholdBinary(new Gray(100), new Gray(155));
                im.Save(imagePath);
                var contours = new VectorOfVectorOfPoint();
                CvInvoke.FindContours(binary, contours, null, RetrType.List, ChainApproxMethod.ChainApproxSimple);
                int index = 0;
                using var image = Pix.LoadFromFile(imagePath);

                int loop = contours.Size - 1;
                for (int k = loop; k > 0; k--) //for (int k = 0; k < contours.Size; k++)
                {
                    var area = CvInvoke.ContourArea(contours[k]);
                    if (area > 5000 && area < 200000)
                    {
                        var rect = CvInvoke.BoundingRectangle(contours[k]);
                        cells.Add(rect);
                        Rect roi = new(rect.X, rect.Y, rect.Width, rect.Height);
                        using var page = _engine.Process(image, roi);
                        string txt = page.GetText().Trim();
                        double conf = page.GetMeanConfidence() * 100;
                        pageConfidence += conf;
                        totalFields++;
                        if (txt.IsNullOrEmpty())
                        {
                            textExtraction.Invoice.TableDetails.Cells.Add(new Cell { CellNo = index++, Text = string.Empty, Confidence = conf });
                            continue;
                        }

                        DrawRect(rect.X, rect.Y, rect.Width, rect.Height, index, tableImagePath);
                        textExtraction.Invoice.TableDetails.Cells.Add(new Cell { CellNo = index++, Text = txt, Rectangle = $"{rect.X}, {rect.Y}, {rect.Width}, {rect.Height}", Confidence = page.GetMeanConfidence() * 100 });
                    }
                }

                var Rows = cells.GroupBy(x => x.X).OrderBy(i => i.Key);

                foreach (var item in Rows)
                {
                    if (textExtraction.Invoice.TableDetails.Rows <= item.Count())
                    {
                        textExtraction.Invoice.TableDetails.Rows = item.Count();
                    }
                }

                var Cols = cells.GroupBy(x => x.Y).OrderByDescending(i => i.Key);
                foreach (var item in Cols)
                {
                    if (textExtraction.Invoice.TableDetails.Columns <= item.Count())
                    {
                        textExtraction.Invoice.TableDetails.Columns = item.Count();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Error while extracting Table details\n{ex}", ex);
            }
            finally
            {
                Thread.Sleep(300);
                GC.Collect();
                File.Delete(imagePath);
            }
        }

        public bool MoveFile(string source, string destination, string fileName)
        {
            try
            {
                Thread.Sleep(3000);
                GC.Collect();

                if (!File.Exists(Path.Combine(destination, fileName)))
                {
                    File.Move(Path.Combine(source, fileName), Path.Combine(destination, fileName));
                    return true;
                }
                File.Delete(Path.Combine(source, fileName));
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError("Cannot move file.\n {ex}", ex);
                return false;
            }
        }
        public void CopyToArchive(string source, string fileName)
        {
            string? archive = _config.GetValue<string>("ArchiveFolder");
            if (!string.IsNullOrEmpty(archive))
            {
                if (!File.Exists(Path.Combine(archive, fileName)))
                {
                    File.Copy(Path.Combine(source, fileName), Path.Combine(archive, fileName));
                }
            }
        }

        public static void DrawRect(int x, int y, int width, int height, int index, string imagePath)
        {
            using (System.Drawing.Image image = System.Drawing.Image.FromFile(imagePath))
            {
                // Create a graphics object to draw on the image
                using (Graphics graphics = Graphics.FromImage(image))
                {
                    Random random = new Random();

                    // Generate random values for each RGB component
                    int red = random.Next(256);
                    int green = random.Next(256);
                    int blue = random.Next(256);

                    // Create a new Color object using the random RGB values
                    Color randomColor = Color.FromArgb(red, green, blue);

                    float x1 = x + 5;
                    float y1 = y + 3;
                    using (Pen pen = new Pen(Color.DarkRed, 2))
                    {
                        graphics.DrawRectangle(pen, x, y, width, height);
                        graphics.DrawString(index.ToString(), new System.Drawing.Font("Arial", 14, FontStyle.Bold), new SolidBrush(Color.DarkRed), x1, y1);
                    }

                }

                // Save the modified image to a temporary file
                string tempImagePath = Path.Combine(Path.GetDirectoryName(imagePath), index.ToString() + Path.GetExtension(imagePath));
                image.Save(tempImagePath);

                // Close the original image file
                image.Dispose();

                // Delete the original image file
                Thread.Sleep(200);
                GC.Collect();
                File.Delete(imagePath);

                // Rename the temporary file to the original filename
                File.Move(tempImagePath, imagePath);
            }
        }

        public class FileKey
        {
            public string Key { get; set; }
            public string VendorId { get; set; }
        }
    }

}