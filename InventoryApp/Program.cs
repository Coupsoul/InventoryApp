using InventoryApp.Data;
using InventoryApp.Services;
using InventoryApp.UI;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;


class Program
{
    static async Task Main(string[] args)
    {
        var host = Host.CreateDefaultBuilder(args)
            .ConfigureServices((context, services) =>
            {
                string connection = context.Configuration.GetConnectionString("DefaultConnection")!;

                services.AddDbContext<ApplicationContext>(options => options.UseSqlServer(connection));
                services.AddHttpClient<CurrencyService>();
                services.AddTransient<InventoryService>();
                services.AddTransient<ConsoleInterface>();
            })
            .Build();

        using var scope = host.Services.CreateScope();
        var services = scope.ServiceProvider;

        var context = services.GetRequiredService<ApplicationContext>();
        var invService = services.GetRequiredService<InventoryService>();
        var curService = services.GetRequiredService<CurrencyService>();
        var ui = services.GetRequiredService<ConsoleInterface>();

        Console.WriteLine("Проверка данных...");
        context.Database.Migrate();
        DbSeeder.Seed(context);
        Console.Clear();

        var playerName = "admin";

        await ui.RunMainMenuAsync(playerName);
    }
}