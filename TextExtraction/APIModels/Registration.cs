namespace TextExtraction.APIModels
{
    public class Registration
    {
        public string OCRConfig { get; set; }
    }

    public class Rootobject
    {
        public List<Additionalfield> AdditionalFields { get; set; }
    }

    public class Additionalfield
    {
        public string FieldName { get; set; }
        public string Text { get; set; }
        public string Rectangle { get; set; }
        public int PageNumber { get; set; }
    }
}
