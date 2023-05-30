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

        public static void Extract(LineData line, int pageNumber, PurchaseOrder purchaseOrder, List<Rect> rects = null)
        {
            var orderNumber = Regex.Match(line.Text, @"\b(LOAD|REFERENCE)\W+(\w+\d+\w+)");
            if (orderNumber.Success)
            {
                //Console.WriteLine("Order number :" + orderNumber.Groups[2].Value);
                purchaseOrder.Text = orderNumber.Groups[2].Value;
                Rect rect = line.Words.Single(x => x.Text == orderNumber.Groups[2].Value).Coordinates;
                purchaseOrder.Rectangle = Helper.ConvertToPdfPoints(rect);
                purchaseOrder.PageNumber = pageNumber;
                //rects.Add(rect);
            }
        }
    }
}
