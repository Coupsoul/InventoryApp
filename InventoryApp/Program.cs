using InventoryApp.Data;
using InventoryApp.Enums;
using InventoryApp.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

class Program
{
    private static readonly Random _rnd = new Random();

    static async Task Main(string[] args)
    {
        var host = Host.CreateDefaultBuilder(args)
            .ConfigureServices((context, services) =>
            {
                string connection = context.Configuration.GetConnectionString("DefaultConnection")!;

                services.AddDbContext<ApplicationContext>(options => options.UseSqlServer(connection));
                services.AddHttpClient<CurrencyService>();
                services.AddTransient<InventoryService>();
            })
            .Build();

        using var scope = host.Services.CreateScope();
        var services = scope.ServiceProvider;

        var context = services.GetRequiredService<ApplicationContext>();
        var invService = services.GetRequiredService<InventoryService>();
        var curService = services.GetRequiredService<CurrencyService>();

        Console.WriteLine("Проверка данных...");
        context.Database.Migrate();
        DbSeeder.Seed(context);
        Console.Clear();

        var playerName = "Player_01";

        bool exit = false;
        while (!exit)
        {
            Console.WriteLine($"Добро пожаловать, {playerName}!");

            Console.WriteLine("\nВыберите действие");
            Console.WriteLine("1. Мой инвентарь");
            Console.WriteLine("2. Магазин");
            Console.WriteLine("3. Ломбард");
            Console.WriteLine("4. Гринд");
            Console.WriteLine("5. Обмен валют");
            Console.WriteLine("0. Выход");
            Console.Write("> ");

            string? input = Console.ReadLine();
            Console.WriteLine();

            switch (input)
            {
                case "1":
                    await ShowInventory(invService, playerName); break;
                case "2":
                    await Shopping(invService, playerName); break;
                case "3":
                    await OpenLombard(invService, playerName); break;
                case "4":
                    await Grind(invService, playerName); break;
                case "5":
                    await OpenCurrencyExchanger(curService, invService, playerName); break;
                case "0":
                    exit = true; break;
                default:
                    Console.WriteLine("Неизвестная команда."); WaitAndClear(); break;
            }
        }
    }

    static async Task ShowInventory(InventoryService service, string playerName)
    {
        Console.Clear();
        Console.WriteLine("----------------  ИНВЕНТАРЬ  ----------------");

        var player = await service.GetPlayerAsync(playerName);

        if (player != null)
        {
            Console.WriteLine($"{player.Name}");
            Console.WriteLine($"Баланс: {player.Gold} золота | {player.Gems} брюлликов");
            Console.WriteLine("Инвентарь:");

            if (player.Inventory.Count != 0)
            {
                foreach (var slot in player.Inventory)
                {
                    Console.WriteLine($" [{slot.Amount} шт] {slot.Item.Name}");
                    if (!string.IsNullOrEmpty(slot.Item.Description))
                        Console.WriteLine($"  \"{slot.Item.Description}\"");
                }
            }
            else
                Console.WriteLine("Инвентарь пуст.");
        }

        Console.WriteLine("\nНажмите любую клавишу для продолжения.");
        WaitAndClear();
    }

    static async Task Shopping(InventoryService service, string playerName)
    {
        Console.Clear();
        var items = await service.GetShopItemsAsync();
        bool exit = false;

        while (!exit)
        {
            var player = await service.GetPlayerAsync(playerName);
            if (player == null)
            {
                Console.WriteLine("Игрок не найден.");
                exit = true;
                return;
            }

            Console.WriteLine("----------------  СЕМЁРОЧКА  ----------------");
            Console.WriteLine($"Баланс: {player.Gold} золота | {player.Gems} брюлликов.\n");

            for (int i = 0; i < items.Count; i++)
            {
                var item = items[i];
                string currency = item.PriceCurrency switch
                {
                    Currency.Gold => "золота",
                    Currency.Gems => "брюлликов",
                    _ => item.PriceCurrency.ToString()
                };
                string priceStr = item.Price == 0 ? "бесплатно" : $"{item.Price} {currency}";

                Console.WriteLine($"{i + 1}. {item.Name}  -  {priceStr}");
                if (!string.IsNullOrEmpty(item.Description))
                    Console.WriteLine($"  \"{item.Description}\"");
                Console.WriteLine();
            }

            Console.WriteLine("~ Введите номер товара для покупки или 0, чтобы выйти:");
            Console.Write("> ");
            string? input = Console.ReadLine();
            if (input == "0")
            {
                exit = true;
                Console.Clear();
                return;
            }
            else if (int.TryParse(input, out int choice) && choice > 0 && choice <= items.Count)
            {
                Console.WriteLine(await service.BuyItemAsync(playerName, items[choice - 1].Name));
                Console.WriteLine("Нажмите любую клавишу для продолжения.");
            }
            else Console.WriteLine("Некорректный ввод. Нажмите любую клавишу.");

            WaitAndClear();
        }
    }

    static async Task OpenLombard(InventoryService service, string playerName)
    {
        Console.Clear();
        bool exit = false;
        while (!exit)
        {
            var player = await service.GetPlayerAsync(playerName);
            if (player == null)
            {
                Console.WriteLine("Игрок не найден.");
                exit = true;
                return;
            }

            Console.WriteLine("--------------  СКУПОЙ РЫЦАРЬ  --------------");
            Console.WriteLine($"Баланс: {player.Gold} золота | {player.Gems} брюлликов.");
            Console.WriteLine("Ваш инвентарь:");

            if (player.Inventory.Count == 0)
            {
                Console.WriteLine("   (пусто)\n");
                Console.WriteLine("Заходи, когда будет что продать.");
                WaitAndClear();
                return;
            }

            var invItems = player.Inventory.ToList();
            for (int i = 0; i < invItems.Count; i++)
            {
                var slot = invItems[i];
                var sellPrice = slot.Item.GetSellPrice();
                var currencyName = slot.Item.PriceCurrency == Currency.Gold ? "золота" : "брюлликов";
                Console.WriteLine($"{i + 1}. {slot.Item.Name}  ({slot.Amount} шт.)\t-\t{sellPrice} {currencyName}.");
            }

            Console.WriteLine("\n~ Введите номер предмета, который хотите продать, или 0, чтобы выйти:");
            Console.Write("> ");
            string? input = Console.ReadLine();

            if (input == "0")
            {
                exit = true;
                Console.Clear();
                return;
            }
            else if (int.TryParse(input, out int choice) && choice > 0 && choice <= invItems.Count)
            {
                Console.WriteLine(await service.SellItemAsync(playerName, invItems[choice - 1].Item.Name));
                Console.WriteLine("Нажмите любую клавишу для продолжения.");
            }
            else Console.WriteLine("Некорректный ввод. Нажмите любую клавишу.");

            WaitAndClear();
        }
    }

    static async Task Grind(InventoryService service, string playerName)
    {
        Console.Clear();
        Console.CursorVisible = false;
        try
        {
            string[] activities = [
            "Зачистка данжа",
            "Фарминг мобов",
            "Пылесосинг локации"];
            string phrase = activities[_rnd.Next(activities.Length)];

            Console.Write($"~ {phrase}");
            for (int dot = 0; dot < 6; dot++)
            {
                if (dot != 3) await Task.Delay(1000);
                Console.Write('.');
                if (dot == 2 || dot == 5)
                {
                    await Task.Delay(1000);
                    Console.Write("\b\b\b   \b\b\b");
                }
            }

            int goldRew = _rnd.Next(10, 17);
            int gemsRew = _rnd.Next(0, 3);

            var result = await service.AddRewardAsync(playerName, goldRew, gemsRew);
            Console.WriteLine($"\n{result}");
        }
        finally
        {
            Console.CursorVisible = true;
        }

        Console.WriteLine("\nНажмите любую клавишу.");
        WaitAndClear();
    }

    static async Task OpenCurrencyExchanger(CurrencyService curService, InventoryService invService, string playerName)
    {
        Console.Clear();
        Console.WriteLine("Загрузка курса с биржи...");
        int curRate = await curService.GetGemPriceInGoldAsync();

        bool exit = false;
        while (!exit)
        {
            var player = await invService.GetPlayerAsync(playerName);

            Console.Clear();
            Console.WriteLine("-----------------  ОБМЕННИК  -----------------");
            Console.WriteLine($"Курс валют: 1 Брл = {curRate} Злт");
            Console.WriteLine($"Ваш баланс: {player!.Gold} золота | {player.Gems} брюлликов\n");
            Console.WriteLine("1. Обменять золото на брюллики");
            Console.WriteLine("2. Обменять брюллики на золото");
            Console.WriteLine("0. Выйти");
            Console.Write("> ");
            string? input = Console.ReadLine();

            switch (input)
            {
                case "1":
                    await TryExchange(invService, playerName, curRate, isBuying: true); break;
                case "2":
                    await TryExchange(invService, playerName, curRate, isBuying: false); break;
                case "0":
                    exit = true; Console.Clear(); return;
                default:
                    Console.WriteLine("Неизвестная команда"); break;
            }

            WaitAndClear();
        }
    }

    static async Task TryExchange(InventoryService invService, string playerName, int rate, bool isBuying)
    {
        string actionText = isBuying ? "получить" : "обменять";
        Console.Write($"\nВведите количество Брюлликов, которое хотите {actionText}: ");

        if (!int.TryParse(Console.ReadLine(), out int amount) || amount <= 0)
        {
            Console.WriteLine("Ошибка ввода.");
            return;
        }

        int totalGold = rate * amount;
        Console.WriteLine(isBuying
            ? $"Это будет стоить {totalGold} Злт."
            : $"Вы получите {totalGold} Злт.");
        Console.WriteLine("Нажмите [Enter] для подтверждения или [Esc] для отмены.\n");

        while (true)
        {
            var key = Console.ReadKey(intercept: true).Key;

            if (key == ConsoleKey.Enter)
            {
                int gemChange = isBuying ? amount : -amount;
                string reuslt = await invService.ExchangeGemsAsync(playerName, gemChange, rate);
                Console.WriteLine(reuslt);
                break;
            }
            else if (key == ConsoleKey.Escape)
            {
                Console.WriteLine("Операция отменена.");
                break;
            }
        }
    }

    static void WaitAndClear()
    {
        Console.ReadKey(true);
        Console.Clear();
    }
}