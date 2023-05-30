using System.ComponentModel.DataAnnotations.Schema;
using System.Drawing;
using Tesseract;
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
        public Patient Patient { get; set; }
        public Invoice Invoice { get; set; }
        public TextExtractionFields()
        {
            Invoice = new();
            Patient = new();
        }
    }

    #region Patient Details Model
    public class Patient
    {

        public PatientName Name { get; set; }
        public PatientBirthDate BirthDate { get; set; }
        public List<dynamic> AdditionalFields { get; set; } 

        public Patient()
        {
            Name = new();
            BirthDate = new();
            AdditionalFields = new List<dynamic>();

        }
        //public string Name { get; set; }
        //public string BirthDate { get; set; }
    }
    #endregion

    #region Invoice Processing Models
    public class Invoice
    {
        public InvoiceNumber InvNum { get; set; }
        public InvoiceDate InvDate { get; set; }
        public PurchaseOrder OrderNum { get; set; }
        public PurchaseOrderDate OrderDate { get; set; }
        public VendorName VendorName { get; set; }
        public Tax Tax { get; set; }
        public GrossAmount Total { get; set; }
        public List<dynamic> AdditionalFields { get; set; }
        public Invoice()
        {
            InvNum = new();
            InvDate = new();
            OrderNum = new();
            OrderDate = new();
            VendorName = new();
            Tax = new();
            Total = new();
            AdditionalFields = new List<dynamic>();
        }

    }

    public class Supplier
    {
        public string CompanyName { get; set; }
        public Rectangle VendorNameCord { get; set; }
        public string Address { get; set; }
        public string BusinessNumber { get; set; }
        public string PhoneNumber { get; set; }
        public string Fax { get; set; }
        public string Email { get; set; }
        public string Website { get; set; }
    }

    public class Customer
    {
        public string Number { get; set; }
        public string Name { get; set; }
        public string CompanyName { get; set; }
        public string BillingAddress { get; set; }
        public string DeliveryAddress { get; set; }
        public string BusinessNumber { get; set; }
        public string PhoneNumber { get; set; }
        public string Fax { get; set; }
        public string Email { get; set; }
    }

    public class Payment
    {
        public string DueDate { get; set; }
        public string BaseAmount { get; set; }
        public string Tax { get; set; }
        public Rectangle TaxCord { get; set; }
        public string Total { get; set; }
        public Rectangle TotalCord { get; set; }
        public string Paid { get; set; }
        public string DueAmount { get; set; }
        public string PaymentReference { get; set; }
    }
    #endregion

    #region Text Extraction with Coords
    public class ProcessedPdf
    {
        public Guid DocumentId { get; set; }
        public string FileName { get; set; }
        public float Confidence { get; set; }
        public List<PageData> Pages { get; set; }
        public ProcessedPdf()
        {
            Pages = new();
        }
    }
    public class PageData
    {
        public int PageNumber { get; set; }
        public List<LineData> Lines { get; set; }
        public PageData()
        {
            Lines = new();
        }
    }
    public class LineData
    {
        public int LineNumber { get; set; }
        public Rect LineCoordinates { get; set; }
        public string Text { get; set; }
        public List<WordData> Words { get; set; }
        public LineData()
        {
            Words = new();
        }
    }
    public class WordData
    {
        public string Text { get; set; }
        public Tesseract.Rect Coordinates { get; set; }
    }
    public class DrawCoordinates
    {
        public int PageNumber { get; set; }
        public List<Rect> Rects { get; set; }
        public DrawCoordinates()
        {
            Rects = new();
        }

    }
    #endregion

}
