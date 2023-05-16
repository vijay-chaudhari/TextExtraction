using System.Drawing;
using System.Text.RegularExpressions;
using Tesseract;

namespace TextExtraction.Model
{
    public class PurchaseOrderDate
    {
        public string Text { get; set; }
        public int PageNumber { get; set; }
        public string Rectangle { get; set; }

        public static void Extract(LineData line, int pageNumber, List<Rect> rects, PurchaseOrderDate orderDate )
        {
            //var orderNumber = Regex.Match(line.Text, @"\b(LOAD|REFERENCE)\W+(\w+\d+\w+)");
            //if (orderNumber.Success)
            //{
            //    //Console.WriteLine("Order number :" + orderNumber.Groups[2].Value);
            //    purchaseOrder.Text = orderNumber.Groups[2].Value;
            //    Rect rect = line.Words.Single(x => x.Text == orderNumber.Groups[2].Value).Coordinates;
            //    purchaseOrder.Rectangle = new System.Drawing.Rectangle(rect.X1, rect.Y1, rect.X2, rect.Y2);
            //    purchaseOrder.PageNumber = pageNumber;
            //    rects.Add(rect);
            //}
        }
    }
}
