
using lab1_load_balancing.Services;

namespace lab1_load_balancing
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddControllers();
            builder.Services.AddSingleton<CacheService>();

            var app = builder.Build();

            app.MapControllers();

            app.Run();
        }
    }
}
