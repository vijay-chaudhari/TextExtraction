namespace TextExtraction.Model
{
    public class ConfigurationSettings
    {
        public Guid TemplateId { get; set; }
        public string TemplateName { get; set; }
        public List<FieldModel> Fields { get; set; }
    }

    public class FieldModel
    {
        public string FieldName { get; set; }
        public string RegExpression { get; set; }
        public string CoOrdinates { get; set; }
        public int FromPageNum { get; set; }

    }

    public class FieldValueModel
    {
        public string FieldName { get; set; }
        public string Value { get; set; }
        public string CoOrdinates { get; set; }
        public int PageNum { get; set;}
    }
}
