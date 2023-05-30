using edu.stanford.nlp.ie.crf;
using NameRecognizer;
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
            this._logger = logger;
            this._config = config;
            this._service = service;
        }

        public bool Initialize(ref CRFClassifier? crfClassifier, ref TesseractEngine? engine, ref ConfigurationSettings? template)
        {
            bool isInitialized;
            (template, isInitialized) = GetTemplate();
            if (!isInitialized) { return isInitialized; };

            (crfClassifier, isInitialized) = InitNLP();
            if (!isInitialized) { return isInitialized; };

            (engine, isInitialized) = InitOCR();
            if (!isInitialized) { return isInitialized; };

            return isInitialized;
        }
        public (TesseractEngine?, bool) InitOCR()
        {
            TesseractEnviornment.CustomSearchPath = Environment.CurrentDirectory;
            TesseractEngine? engine = null;

            _logger.LogInformation("OCR engine Path :{path}", Directory.GetCurrentDirectory() + "\\tessdata");
            try
            {
                engine = OCR.Image.LoadEnglishEngine(Directory.GetCurrentDirectory() + "\\tessdata");
            }
            catch (Exception ex)
            {
                _logger.LogError("OCR Engine Initialize failed.\n Exception : {ex}", ex);
                return (engine, false);
            }
            _logger.LogInformation("OCR Engine Initialize successfully on {time}", DateTimeOffset.Now);
            return (engine, true);
        }
        public (CRFClassifier?, bool) InitNLP()
        {
            bool extractPatientDetails = _config.GetValue<string>("ExtractPatientDetails").IsNullOrEmpty() ? false : System.Convert.ToBoolean(_config.GetValue<string>("ExtractPatientDetails"));
            if (extractPatientDetails)
            {
                string? enginePath = Directory.GetCurrentDirectory() + "\\stanford-ner-4.2.0";
                CRFClassifier classifier = EntityRecognizer.LoadNLPEngine(enginePath);
                if (classifier is null)
                {
                    _logger.LogError("NLP Engine Initialization failed - path: {path}", enginePath);
                    return (classifier, false);
                }
                _logger.LogInformation("NLP Engine Initialize successfully on {time}", DateTimeOffset.Now);
                return (classifier, true);
            }
            return (null, true);
        }
        public (ConfigurationSettings?, bool) GetTemplate()
        {
            ConfigurationSettings? template = null;
            string temaplateId = _config.GetValue<string>("TemaplateId").IsNullOrEmpty() ? "" : _config.GetValue<string>("TemaplateId");
            var request = new LoginRequest { UserNameOrEmailAddress = "serviceuser", Password = "1q2w3E*", RememberMe = true };
            LoginResponse? result = _service.PostAsync<LoginRequest, LoginResponse?>("api/account/login", request).GetAwaiter().GetResult();

            if (result is not null && result.Description.Equals("Success"))
            {
                template = _service.GetAsync<ConfigurationSettings>($"api/app/config/{temaplateId}").GetAwaiter().GetResult();
                if (template is null)
                {
                    _logger.LogError("No Template found for TemplateId {temaplateId}", temaplateId);
                    return (template, false);
                }
                else
                {
                    _logger.LogInformation("Template recovered from API tamplate ID: {temaplateId}", temaplateId);
                    return (template, true);
                }
            }
            else
            {
                _logger.LogError("Login API failed");
                return (template, false);
            }
        }
    }
}
