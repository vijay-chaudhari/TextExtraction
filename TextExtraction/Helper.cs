using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tesseract;

namespace TextExtraction
{
    public static class Helper
    {
        public static string ConvertToPdfPoints(Rect rect)
        {
            float constant = 4.166666666666667f;
            return $"{rect.X1 / constant},{rect.Y1 / constant},{rect.X2 / constant},{rect.Y2 / constant}";
        }
    }
}
    