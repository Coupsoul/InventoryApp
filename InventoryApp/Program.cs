using InventoryApp.Data;
using InventoryApp.Entities;
using InventoryApp.Services;
using InventoryApp.Services.Interfaces;
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
                services.AddHttpClient<ICurrencyService, CurrencyService>();
                services.AddTransient<IInventoryService, InventoryService>();
                services.AddTransient<IUserService, UserService>();
                services.AddTransient<ConsoleInterface>();
            })
            .Build();

        using var scope = host.Services.CreateScope();
        var services = scope.ServiceProvider;

        var context = services.GetRequiredService<ApplicationContext>();
        var invService = services.GetRequiredService<IInventoryService>();
        var curService = services.GetRequiredService<ICurrencyService>();
        var userService = services.GetRequiredService<IUserService>();
        var ui = services.GetRequiredService<ConsoleInterface>();

        Console.WriteLine("Проверка данных...");
        context.Database.Migrate();
        DbSeeder.Seed(context);
        Console.Clear();

        Player player = await ui.AuthorizeAsync();

        await ui.RunMainMenuAsync(player);
    }
}