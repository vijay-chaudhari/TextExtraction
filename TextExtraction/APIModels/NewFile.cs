namespace TextExtraction.APIModels
{
    public class NewFile
    {
        public string templateName { get; set; }
        public string vendorNo { get; set; }
        public string filePath { get; set; }
        public string ocrConfig { get; set; }
        public object lastModificationTime { get; set; }
        public object lastModifierId { get; set; }
        public DateTime creationTime { get; set; }
        public object creatorId { get; set; }
        public Guid id { get; set; }
    }
}
