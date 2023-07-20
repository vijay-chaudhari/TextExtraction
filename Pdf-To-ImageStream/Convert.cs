using Ghostscript.NET;
using Ghostscript.NET.Rasterizer;
using System.Drawing.Imaging;

namespace Pdf_To_ImageStream
{
    public static class Convert
    {
        public static List<MemoryStream>? ToStreams(string ghostScriptPath, string pdfDath)
        {
            try
            {
                if (OperatingSystem.IsWindows())
                {
                    int desired_dpi = 300;
                    GhostscriptVersionInfo gvi = new GhostscriptVersionInfo(ghostScriptPath);
                    using var resterizer = new GhostscriptRasterizer();
                    resterizer.Open(pdfDath, gvi, false);
                    var streams = new List<MemoryStream>();
                    for (var i = 1; i <= resterizer.PageCount; i++)
                    {
                        var stream = new MemoryStream();
                        resterizer.GetPage(desired_dpi, i).Save(stream, ImageFormat.Tiff);
                        resterizer.GetPage(desired_dpi, 1).Save($"{Path.GetFileNameWithoutExtension(pdfDath)}.tiff");
                        streams.Add(stream);
                    }
                    return streams;
                }
                return null;
            }
            catch (Exception)
            {
                throw;
            }
        }
    }
}