using System.Text.RegularExpressions;
using Tesseract;

namespace TextExtraction.Model
{
    public class VenderNumber
    {
        public string Text { get; set; }
        public int PageNumber { get; set; }
        public string Rectangle { get; set; }

        public static bool Extract(LineData line, int pageNumber, VenderNumber venderNumber, List<Rect> rects = null)
        {
            try
            {
                var invoiceNumber = Regex.Match(line.Text, @"\b(CUSTOMER NUMBER)(\W+|\s+)(\d+)\b");
                if (invoiceNumber.Success)
                {
                    venderNumber.Text = Regex.Match(line.Text, @"[.\d]+").Value;
                    Rect rect = line.Words.Single(x => x.Text == venderNumber.Text).Coordinates;
                    venderNumber.Rectangle = Helper.ConvertToPdfPoints(rect);
                    venderNumber.PageNumber = pageNumber;
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
