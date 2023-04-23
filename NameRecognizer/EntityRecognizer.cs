using edu.stanford.nlp.ie.crf;
using edu.stanford.nlp.ling;
using edu.stanford.nlp.pipeline;
using Microsoft.Recognizers.Text;
using Microsoft.Recognizers.Text.DateTime;
using System.Text.RegularExpressions;

namespace NameRecognizer
{
    public class EntityRecognizer
    {
        public static string RecognizeDate(string text)
        {
            //Console.WriteLine(text);
            // Extract person name
            //var name = Regex.Match(text, @"^(\w+)\s(\w+)").Value;
            //Console.WriteLine("Name: " + name);

            // Extract date of birth
            var results = DateTimeRecognizer.RecognizeDateTime(text, Culture.English).FirstOrDefault();
            //foreach (var result in results)
            if (results is not null)
            {
                return results.Text; //result.Resolution["date"]);
            }
            return string.Empty;
        }

        public static string? GetPersonName(string text, CRFClassifier classifier)
        {
            // Path to the folder with classifiers models


            //Console.WriteLine("{0}\n", classifier.classifyToString(text));

            //Console.WriteLine("{0}\n", classifier.classifyWithInlineXML(text));

            //Console.WriteLine("{0}\n", classifier.classifyToString(text, "xml", true));

            var result = classifier.classifyWithInlineXML(text);
            var match = Regex.Match(result, @"<PERSON>(.*?)</PERSON>");
            if (match.Success)
            {
                return match.Groups[1].Value;
            }
            return null;

        }

        public static CRFClassifier LoadNLPEngine(string enginePath)
        {
            try
            {
                //var jarRoot = @"C:\Users\vijay.chaudhari\Downloads\stanford-ner-4.2.0";
                var classifiersDirectory = enginePath + @"\classifiers";

                // Loading 3 class classifier model
                var classifier = CRFClassifier.getClassifierNoExceptions(classifiersDirectory + @"\english.muc.7class.distsim.crf.ser.gz");
                return classifier;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public static string GetAnnotations(string text, CRFClassifier classifier)
        {
            return classifier.classifyWithInlineXML(text);
        }

        public static string? GetOrganizationName(string text, CRFClassifier classifier)
        {
            // Path to the folder with classifiers models


            //Console.WriteLine("{0}\n", classifier.classifyToString(text));

            //Console.WriteLine("{0}\n", classifier.classifyWithInlineXML(text));

            //Console.WriteLine("{0}\n", classifier.classifyToString(text, "xml", true));

            var result = classifier.classifyWithInlineXML(text);
            var match = Regex.Match(result, @"<ORGANIZATION>(.*?)</ORGANIZATION>");
            if (match.Success)
            {
                return match.Groups[1].Value;
            }
            return null;

        }
    }
}