using edu.stanford.nlp.ie.crf;
using NameRecognizer;
using System.Drawing;
using Tesseract;

namespace TextExtraction.Model
{
    public class PatientName
    {
        public string Text { get; set; }
        public int PageNumber { get; set; }
        public string Rectangle { get; set; }

        public static void Extract(LineData line, int pageNumber, List<Rect> rects, PatientName name, CRFClassifier? _crfClassifier)
        {
            string filteredText = FilterData.RemoveSpecialCharacters(line.Text).ToUpper();
            string? personName = EntityRecognizer.GetPersonName(filteredText, _crfClassifier);
            if (!string.IsNullOrEmpty(personName))
            {
                name.Text = personName;
                Rect rect = line.LineCoordinates;
                name.Rectangle = Helper.ConvertToPdfPoints(rect);
                name.PageNumber = pageNumber;
                //Console.WriteLine("Invoice Number :" + Regex.Match(line.Text, @"[.\d]+").Value);
                rects.Add(rect);
            }
        }
    }
}
