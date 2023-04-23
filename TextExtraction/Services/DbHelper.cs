using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TextExtraction.Model;

namespace TextExtraction.Services
{
    public class DbHelper
    {
        private AppDbContext dbContext { get; set; }

        private DbContextOptions<AppDbContext> DbContextOptions()
        {
            var optionBuilder = new DbContextOptionsBuilder<AppDbContext>();
            optionBuilder.UseSqlServer(AppSettings.ConfigurationString);
            return optionBuilder.Options;
        }

        //getdata
        public List<ImageOcr> GetAllDocumentDetails()
        {
            using var dbContext = new AppDbContext(DbContextOptions());
            var data = dbContext.ImageOcr.ToList();
            if (data is null)
            {
                return new List<ImageOcr>();
            }
            return data;

        }
        //seed Data

        public void InsertData(ImageOcr imageOcr)
        {
            using var dbContext = new AppDbContext(DbContextOptions());
            dbContext.Add(imageOcr);
            dbContext.SaveChanges();
        }
    }
}
