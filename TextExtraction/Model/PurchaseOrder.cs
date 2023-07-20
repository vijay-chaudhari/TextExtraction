using System.Drawing;
using System.Text.RegularExpressions;
using Tesseract;

namespace TextExtraction.Model
{
    public class PurchaseOrder
    {
        public string Text { get; set; }
        public int PageNumber { get; set; }
        public string Rectangle { get; set; }

        public static bool Extract(LineData line, int pageNumber, PurchaseOrder purchaseOrder, List<Rect> rects = null)
        {
            try
            {
                //var orderNumber = Regex.Match(line.Text, @"\b(LOAD|REFERENCE|ORDER NUMBER)\W+(\w+\d+\w+)");
                var orderNumber = Regex.Match(line.Text, @"\b(PURCHASE ORDER NO)\W+(\w+\/\d+\w+)");
                if (orderNumber.Success)
                {
                    //Console.WriteLine("Order number :" + orderNumber.Groups[2].Value);
                    purchaseOrder.Text = orderNumber.Groups[2].Value;
                    Rect rect = line.Words.Single(x => x.Text.Equals(purchaseOrder.Text, StringComparison.OrdinalIgnoreCase)).Coordinates;
                    purchaseOrder.Rectangle = Helper.ConvertToPdfPoints(rect);
                    purchaseOrder.PageNumber = pageNumber;
                    //rects.Add(rect);
                    return true;
                }
                return false;
            }
            catch { return false; }
        }
    }
}
