using System.Drawing;
using System.Text.RegularExpressions;
using Tesseract;

namespace TextExtraction.Model
{
    public class InvoiceNumber
    {
        public string Text { get; set; }
        public int PageNumber { get; set; }
        public string Rectangle { get; set; }

        public static bool Extract(LineData line, int pageNumber, InvoiceNumber invoice, List<Rect> rects = null)
        {
            try
            {
                var invoiceNumber = Regex.Match(line.Text, @"\b(INVOICE)(\W+|\s+)(\d+)\b");
                if (invoiceNumber.Success)
                {
                    invoice.Text = Regex.Match(line.Text, @"[.\d]+").Value;
                    Rect rect = line.Words.Single(x => x.Text == invoice.Text).Coordinates;
                    invoice.Rectangle = Helper.ConvertToPdfPoints(rect);
                    invoice.PageNumber = pageNumber;
                    //Console.WriteLine("Invoice Number :" + Regex.Match(line.Text, @"[.\d]+").Value);
                    //rects.Add(rect);
                    return true;
                }
                return false;

            }
            catch { return false; }
        }
    }
}
