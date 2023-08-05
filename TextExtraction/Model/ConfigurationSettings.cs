using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using Volo.Abp.Domain.Entities;
using Volo.Abp.Domain.Entities.Auditing;

namespace TextExtraction.Model
{
    [Keyless]
    public class ConfigurationSettings : FullAuditedAggregateRoot<Guid>
    {
        public string TemplateName { get; set; }
        public ICollection<FieldConfig> Fields { get; set; }
        public ConfigurationSettings()
        {
            Fields = new List<FieldConfig>();
        }
    }

    public class FieldConfig : Entity<Guid>
    {
        [Required]
        public Guid TemplateId { get; set; }
        public string FieldName { get; set; }
        public string RegExpression { get; set; }
        public string CoOrdinates { get; set; }
        public int PageNumber { get; set; }
        public double Confidenece { get; set; }
        public string Value { get; set; }
    }

    public class FieldValueModel
    {
        public string FieldName { get; set; }
        public string Value { get; set; }
        public string CoOrdinates { get; set; }
        public int PageNum { get; set;}
    }
}
