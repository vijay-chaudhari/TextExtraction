using javax.swing;
using System.ComponentModel.DataAnnotations.Schema;
using Volo.Abp.Domain.Entities.Auditing;

namespace TextExtraction.Model
{
    [Table("AppImageOcr")]
    public class ImageOcr : AuditedEntity<Guid>
    {
        public string InputPath { get; set; }
        public string OutputPath { get; set; }
        public string OCRText { get; set; }
        public string Confidence { get; set; }
        public string Output { get; set; }

    }

    public class TextExtractionFields
    {
        public Invoice Invoice { get; set; }
        public TextExtractionFields()
        {
            Invoice = new();
            //Patient = new();
        }
    }

    #region Invoice Processing Models
    public class Invoice
    {
        public List<Field> AdditionalFields { get; set; }
        public Table TableDetails { get; set; }
        public Invoice()
        {
            AdditionalFields = new List<Field>();
            TableDetails = new Table();
        }
    }

    public class Field
    {
        public string FieldName { get; set; }
        public string Text { get; set; }
        public string Rectangle { get; set; }
        public int PageNumber { get; set; }
        public double Confidence { get; set; }
    }

    public class Table
    {   
        public int Rows { get; set; }
        public int Columns { get; set; }
        public List<Cell> Cells { get; set; } = new List<Cell>();
       
    }
    public class Cell
    {
        public int CellNo { get; set; }
        public string Text { get; set; }
        public string Rectangle { get; set; }
        public double Confidence { get; set; }
    }
    #endregion
}
