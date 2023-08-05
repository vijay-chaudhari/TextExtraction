using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TextExtraction.APIModels
{
    public class ImageOcrResponse
    {
        public DateTime creationTime { get; set; }
        public string creatorId { get; set; }
        public string id { get; set; }
    }
}
