using Azure;
using javax.xml.soap;
using System.Drawing;
using System.Text.RegularExpressions;
using Tesseract;

namespace TextExtraction.Model
{
    public class GrossAmount
    {
        public string Text { get; set; }
        public int PageNumber { get; set; }
        public string Rectangle { get; set; }

        public static void Extract(LineData line, int pageNumber, GrossAmount total, List<Rect> rects = null)
        {
            var amount = Regex.Match(line.Text, @"\b(TOTAL|RATE|BALANCE DUE)\W+\$\d+(,\d{3})*(\.\d{2})?");
            if (amount.Success)
            {
                total.Text = amount.Value.Replace("TOTAL", "").Replace("RATE", "").Replace("BALANCE DUE", "").Trim();
                Rect rect = line.Words.Single(x => x.Text == total.Text).Coordinates;
                //rects.Add(rect);
                total.Rectangle = Helper.ConvertToPdfPoints(rect);
                total.PageNumber = pageNumber;
            }
        }
    }


}
