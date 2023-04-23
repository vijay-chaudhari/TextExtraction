using Tesseract;

namespace OCR
{
    public static class Image
    {
        public static float Confidence { get; private set; }
        public static TesseractEngine LoadEnglishEngine(string path)
        {
			try
			{
                return new TesseractEngine(path, "eng", EngineMode.Default);
            }
            catch (Exception ex)
			{
                throw;
            }
        }

        public static string GetTextFromFile(TesseractEngine engine, string imagePath)
        {
            try
            {
                Confidence = 0;
                using var image = Pix.LoadFromFile(imagePath);
                using var page = engine.Process(image);
                Confidence = page.GetMeanConfidence();
                return page.GetText();
            }
            catch (Exception)
            {
                throw;
            }
        }

        public static string GetTextFromImageStream(TesseractEngine engine, MemoryStream stream)
        {
            try
            {
                Confidence = 0;
                using var image = Pix.LoadFromMemory(stream.ToArray());
                using var page = engine.Process(image);
                Confidence = page.GetMeanConfidence();
                return page.GetText();
            }
            catch (Exception)
            {
                throw;
            }
        }

        public static string GetTextFromTiffImageStream(TesseractEngine engine, MemoryStream stream)
        {
            try
            {
                Confidence = 0;
                using var image = Pix.LoadTiffFromMemory(stream.ToArray());
                using var page = engine.Process(image);
                Confidence = page.GetMeanConfidence();
                return page.GetText();
            }
            catch (Exception)
            {
                throw;
            }
        }

        public static Page GetPageFromFile(TesseractEngine engine, string imagePath)
        {
            try
            {
                using var image = Pix.LoadFromFile(imagePath);
                using var page = engine.Process(image);
                return page;
            }
            catch (Exception)
            {
                throw;
            }
        }

        public static Page GetPageFromImageStream(TesseractEngine engine, MemoryStream stream)
        {
            try
            {
                using var image = Pix.LoadFromMemory(stream.ToArray());
                using var page = engine.Process(image);
                return page;
            }
            catch (Exception)
            {
                throw;
            }
        }

        public static Page GetPageFromTiffImageStream(TesseractEngine engine, MemoryStream stream)
        {
            try
            {
                using var image = Pix.LoadFromMemory(stream.ToArray());
                using var page = engine.Process(image);
                return page;
            }
            catch (Exception)
            {
                throw;
            }
        }
    }
}