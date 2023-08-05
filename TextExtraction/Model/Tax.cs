using System.Drawing;
using System.Text.RegularExpressions;
using Tesseract;

namespace TextExtraction.Model
{
    public class Tax
    {
        public string Text { get; set; }
        public int PageNumber { get; set; }
        public string Rectangle { get; set; }

        //public static bool Extract(LineData line, int pageNumber, Tax tax, List<Rect> rects = null)
        //{
        //    try
        //    {
        //        //var amount = Regex.Match(line.Text, @"\b(TOTAL|RATE|BALANCE DUE)\W+\$\d+(,\d{3})*(\.\d{2})?");
        //        var amount = Regex.Match(line.Text, @"\b(TAX AMOUNT)\W+\d+(,\d{3})*(\.\d{2})?");
        //        if (amount.Success)
        //        {
        //            //total.Text = amount.Value.Replace("TOTAL", "").Replace("RATE", "").Replace("BALANCE DUE", "").Trim();
        //            tax.Text = amount.Value.Replace("TAX AMOUNT", "").Trim();
        //            Rect rect = line.Words.Single(x => x.Text == tax.Text).Coordinates;
        //            //rects.Add(rect);
        //            tax.Rectangle = Helper.ConvertToPdfPoints(rect);
        //            tax.PageNumber = pageNumber;
        //            return true;
        //        }
        //        return false;
        //    }
        //    catch { return false; }
        //}
    }
}
