using java.awt;
using Microsoft.EntityFrameworkCore;
using TextExtraction.Services;
using Serilog;
using TextExtraction.Initialization;

namespace TextExtraction
{
    public class Program
    {
        public static void Main(string[] args)
        {
            try
            {
                Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Warning)
                .Enrich.FromLogContext()
                .WriteTo.Console()
                .WriteTo.File(@"C:\temp\TextExtraction\log.txt", rollingInterval: RollingInterval.Day)
                .CreateLogger();

                IHost host = Host.CreateDefaultBuilder(args)
                        .UseWindowsService()
                        .UseSerilog()
                        .ConfigureServices((hostcontext, services) =>
                        {
                            IConfiguration configuration = hostcontext.Configuration;
                            AppSettings.Configuration = configuration;
                            AppSettings.ConfigurationString = configuration.GetConnectionString("Default");
                            services.AddHttpClient<IAPIService, APIService>(client =>
                            {
                                client.BaseAddress = new Uri(configuration.GetValue<string>("APIServer"));
                            });
                            //var optionBuilder = new DbContextOptionsBuilder<AppDbContext>();
                            //optionBuilder.UseSqlServer(configuration.GetConnectionString("Default"));
                            //services.AddScoped<AppDbContext>(d => new AppDbContext(optionBuilder.Options));
                            services.AddWindowsService(options =>
                            {
                                options.ServiceName = "TextExtraction";
                            });
                            //services.AddSingleton<DbHelper>();
                            services.AddTransient<Initializer>();
                            services.AddHostedService<Worker>();
                        })
                        .Build();

                host.Run();
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "There was a problem starting the service");
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }
    }
}