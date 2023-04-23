using java.awt;
using Microsoft.EntityFrameworkCore;
using TextExtraction.Services;

namespace TextExtraction
{
    public class Program
    {
        public static void Main(string[] args)
        {
            IHost host = Host.CreateDefaultBuilder(args)
                .UseWindowsService()
                .ConfigureServices((hostcontext, services) =>
                {
                    IConfiguration configuration = hostcontext.Configuration;
                    AppSettings.Configuration = configuration;
                    AppSettings.ConfigurationString = configuration.GetConnectionString("Default");
                    var optionBuilder = new DbContextOptionsBuilder<AppDbContext>();
                    optionBuilder.UseSqlServer(configuration.GetConnectionString("Default"));
                    services.AddScoped<AppDbContext>(d => new AppDbContext(optionBuilder.Options));
                    services.AddWindowsService(options =>
                    {
                        options.ServiceName = "Text Extraction Service";
                    });
                    services.AddSingleton<DbHelper>();
                    services.AddHostedService<Worker>();
                })
                .Build();

            host.Run();
        }
    }
}