using Azure;
using javax.xml.soap;
using NameRecognizer;
using System.Drawing;
using System.Text.RegularExpressions;
using Tesseract;

namespace TextExtraction.Model
{
    public class InvoiceDate
    {
        public string Text { get; set; }
        public int PageNumber { get; set; }
        public string Rectangle { get; set; }

        public static bool Extract(LineData line, int pageNumber, InvoiceDate invDate, List<Rect> rects = null)
        {
            try
            {
                var invoiceDate = Regex.Match(line.Text, @"^(?!.*DUE.*DATE)(?=.*(?:INVOICE\s+)?DATE).*$");
                if (invoiceDate.Success)
                {
                    var date = EntityRecognizer.RecognizeDate(line.Text).ToUpper();
                    if (!string.IsNullOrEmpty(date))
                    {
                        invDate.Text = date;
                        //Console.WriteLine("Invoice date :" + date);
                        var result = line.Words.SingleOrDefault(x => x.Text.Equals(date, StringComparison.OrdinalIgnoreCase))?.Coordinates;
                        if (result is null)
                        {
                            var arr = date.Split(' ');
                            int x1 = 0, y1 = 0, x2 = 0, y2 = 0;
                            for (int i = 0; i < arr.Length; i++)
                            {
                                var a = line.Words.SingleOrDefault(x => x.Text.Equals(arr[i], StringComparison.OrdinalIgnoreCase))?.Coordinates;
                                if (i == 0)
                                {
                                    if (a is not null)
                                    {
                                        x1 = a.Value.X1;
                                        y1 = a.Value.Y1;
                                    }
                                }
                                else if (i == arr.Length - 1)
                                {
                                    x2 = a.Value.X2;
                                    y2 = a.Value.Y2;
                                }
                            }
                            Tesseract.Rect rect = Rect.FromCoords(x1, y1, x2, y2);
                            invDate.Rectangle = Helper.ConvertToPdfPoints(rect);
                            invDate.PageNumber = pageNumber;
                            //rects.Add(rect);
                            return true;
                        }
                        else
                        {
                            Tesseract.Rect rect = result.Value;
                            invDate.Rectangle = Helper.ConvertToPdfPoints(rect);
                            invDate.PageNumber = pageNumber;
                            //rects.Add(rect);
                            return false;
                        }
                    }
                    return false;
                }
                return false;
            }
            catch { return false; }
        }
    }
}
