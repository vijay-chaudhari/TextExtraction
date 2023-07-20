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

        public static bool Extract(LineData line, int pageNumber, GrossAmount total, List<Rect> rects = null)
        {
            try
            {
                //var amount = Regex.Match(line.Text, @"\b(TOTAL|RATE|BALANCE DUE)\W+\$\d+(,\d{3})*(\.\d{2})?");
                var amount = Regex.Match(line.Text, @"\b(GROSS AMOUNT)\W+\d+(,\d{3})*(\.\d{2})?");
                if (amount.Success)
                {

                    //total.Text = amount.Value.Replace("TOTAL", "").Replace("RATE", "").Replace("BALANCE DUE", "").Trim();
                    total.Text = amount.Value.Replace("GROSS AMOUNT", "").Trim();
                    Rect rect = line.Words.Single(x => x.Text == total.Text).Coordinates;
                    //rects.Add(rect);
                    total.Rectangle = Helper.ConvertToPdfPoints(rect);

                    total.PageNumber = pageNumber;
                    return true;
                }
                else
                {
                    var Total = Regex.Match(line.Text, @"\b(TOTAL)\W+\d+(,\d{3})*(\.\d{2})?");
                    if (Total.Success)
                    {
                        //total.Text = amount.Value.Replace("TOTAL", "").Replace("RATE", "").Replace("BALANCE DUE", "").Trim();
                        total.Text = Total.Value.Replace("TOTAL", "").Trim();
                        Rect rect = line.Words.Single(x => x.Text.Equals(total.Text,StringComparison.OrdinalIgnoreCase)).Coordinates;

                        //rects.Add(rect);
                        total.Rectangle = Helper.ConvertToPdfPoints(rect);
                        total.PageNumber = pageNumber;
                        return true;
                    }
                }
                return false;
            }
            catch { return false; }
        }
    }


}
