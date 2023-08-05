using edu.stanford.nlp.ie.crf;
using NameRecognizer;
using System.Drawing;
using Tesseract;
using TextExtraction.APIModels;
using TextExtraction.Model;
using TextExtraction.Services;

namespace TextExtraction.Initialization
{
    public class Initializer
    {
        private readonly ILogger<Initializer> _logger;
        private readonly IConfiguration _config;
        private readonly IAPIService _service;

        public Initializer(ILogger<Initializer> logger, IConfiguration config, IAPIService service)
        {
            _logger = logger;
            _config = config;
            _service = service;
        }

        public TesseractEngine? InitOCR()
        {
            TesseractEnviornment.CustomSearchPath = Environment.CurrentDirectory;

            _logger.LogInformation("OCR engine Path :{path}", Environment.CurrentDirectory + "\\tessdata");
            try
            {
                TesseractEngine engine = OCR.Image.LoadEnglishEngine(Directory.GetCurrentDirectory() + "\\tessdata");
                _logger.LogInformation("OCR Engine Initialize successfully on {time}", DateTimeOffset.Now);
                return engine;
            }
            catch (Exception ex)
            {
                _logger.LogError("OCR Engine Initialize failed.\n Exception : {ex}", ex);
                return null;
            }
        }
        public CRFClassifier? InitNLP()
        {
            bool extractPatientDetails = _config.GetValue<string>("ExtractPatientDetails").IsNullOrEmpty() ? false : System.Convert.ToBoolean(_config.GetValue<string>("ExtractPatientDetails"));
            if (extractPatientDetails)
            {
                string? enginePath = Directory.GetCurrentDirectory() + "\\stanford-ner-4.2.0";
                CRFClassifier classifier = EntityRecognizer.LoadNLPEngine(enginePath);
                if (classifier is null)
                {
                    _logger.LogError("NLP Engine Initialization failed - path: {path}", enginePath);
                    return null;
                }
                _logger.LogInformation("NLP Engine Initialize successfully on {time}", DateTimeOffset.Now);
                return classifier;
            }
            return null;
        }
        public ConfigurationSettings? GetConfiguredFields()
        {
            string? temaplateId = _config.GetValue<string>("TemaplateId").IsNullOrEmpty() ? "" : _config.GetValue<string>("TemaplateId");

            bool isLoggedIn = Login();
            if (isLoggedIn)
            {
                ConfigurationSettings? template = _service.GetAsync<ConfigurationSettings>($"api/app/config/{temaplateId}").GetAwaiter().GetResult();
                if (template is null)
                {
                    _logger.LogError("No Template found for TemplateId {temaplateId}", temaplateId);
                    return null;
                }
                else
                {
                    _logger.LogInformation("Template recovered from API tamplate ID: {temaplateId}", temaplateId);
                    return template;
                }
            }
            return null;
        }
        public Registration? GetRegisterTemplate(string vendorId)
        {
            Registration registerTemplate = _service.GetAsync<Registration>($"api/app/registration/config-by-vendor-no?VendorNo={vendorId}").GetAwaiter().GetResult();
            if (registerTemplate is null)
            {
                _logger.LogError("No Template found for Vendor {vendorId}", vendorId);
                return null;
            }
            else
            {
                _logger.LogInformation("Template recovered from API vendor : {vendorId}", vendorId);
                return registerTemplate;
            }
        }
        public ImageOcrResponse? SaveOCRData(ImageOcr request)
        {
            ImageOcrResponse result = _service.PostAsync<ImageOcr, ImageOcrResponse?>("api/app/ocr", request).GetAwaiter().GetResult();
            return result;
        }

        public NewFile? SentToRegistration(NewFile request)
        {
            bool isLoggedIn = Login();
            if (isLoggedIn)
            {
                NewFile result = _service.PostAsync<NewFile, NewFile?>("api/app/registration", request).GetAwaiter().GetResult();
                return result;
            }
            return null;
        }

        public string GetVendorID(string fileName)
        {
            VendorDetails vendor = _service.GetAsync<VendorDetails>($"/api/app/ocr/vendor?EmailSr={fileName}").GetAwaiter().GetResult();
            if (vendor is null)
            {
                _logger.LogError("No VendorId found for file {file}", fileName);
                return null;
            }
            else
            {
                _logger.LogInformation("VedorId for file {file} recovered from API vendor : {@vendor}", fileName, vendor);
                return vendor.VendorId;
            }
        }
        public bool Login()
        {
            var request = new LoginRequest { UserNameOrEmailAddress = "serviceuser", Password = "1q2w3E*", RememberMe = true };
            LoginResponse? result = _service.PostAsync<LoginRequest, LoginResponse?>("api/account/login", request).GetAwaiter().GetResult();
            if (result is not null && result.Description.Equals("Success"))
            {
                return true;
            }
            _logger.LogError("Login API failed");
            return false;
        }
    }
}
