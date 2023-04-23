
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Volo.Abp.Domain.Entities;
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
        //public string Name { get; set; }
        //public string BirthDate { get; set; }
        //public string Invoice { get; set; }
        //public string InvoiceDate { get; set; }
        //public string InvoiceDueDate { get; set; }
        //public string OrderNumber { get; set; }
        //public string TotalAmount { get; set; }
        //public string TaxAmount { get; set; }
        //public string VendorName { get; set; }
        public Invoice Invoice { get; set; }
        public TextExtractionFields()
        {
            Invoice = new();
        }
    }

    public class Invoice
    {
        public string Number { get; set; }
        public string Date { get; set; }
        public string OrderDate { get; set; }
        public string PurchaseOrderNumber { get; set; }
        public string Currency { get; set; }
        public Supplier Supplier { get; set; }
        public Customer Customer { get; set; }
        public Payment Payment { get; set; }
        public Invoice()
        {
            Supplier = new();
            Customer = new();
            Payment = new();
        }
    }

    public class Supplier
    {
        public string CompanyName { get; set; }
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
        public string Total { get; set; }
        public string Paid { get; set; }
        public string DueAmount { get; set; }
        public string PaymentReference { get; set; }
    }
}
