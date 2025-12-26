using InventoryApp.Data;
using InventoryApp.Enums;
using InventoryApp.Services;
using Microsoft.EntityFrameworkCore;

class Program
{
    static async Task Main(string[] args)
    {
        var contextFactory = new SampleContextFactory();

        using (var context = contextFactory.CreateDbContext(args))
        {
            Console.WriteLine("Проверка наличия данных...");
            context.Database.Migrate();
            DbSeeder.Seed(context);
            Console.Clear();

            var service = new InventoryService(context);
            var playerName = "Player_01";

            bool exit = false;
            while (!exit)
            {
                Console.WriteLine($"Добро пожаловать, {playerName}!");

                Console.WriteLine("\nВыберите действие");
                Console.WriteLine("1. Мой инвентарь");
                Console.WriteLine("2. Магазин");
                Console.WriteLine("3. Обмен валют");
                Console.WriteLine("0. Выход");
                Console.Write("> ");

                string? input = Console.ReadLine();

                switch (input)
                {
                    case "1":
                        await ShowInventory(service, playerName); break;
                    case "2":
                        await Shopping(service, playerName); break;
                    case "3":
                        await OpenCurrencyExchanger(service); break;
                    case "0":
                        exit = true; break;
                    default:
                        Console.WriteLine("Неизвестная команда"); break;
                }
            }
        }
    }

    static async Task ShowInventory(InventoryService service, string playerName)
    {
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
                    Console.WriteLine($"  [{slot.Amount} шт] {slot.Item.Name}");
                }
            }
            else
                Console.WriteLine("Инвентарь пуст.");
        }

        Console.WriteLine("Нажмите любую клавишу для продолжения.");
        WaitAndClear();
    }

    static async Task Shopping(InventoryService service, string playerName)
    {
        var items = await service.GetShopItemsAsync();
        bool exit = false;

        while (!exit)
        {
            var player = await service.GetPlayerAsync(playerName);

            Console.WriteLine("\n----------------  СЕМЁРОЧКА  ----------------");
            if (player != null)
                Console.WriteLine($"Баланс: {player.Gold} золота | {player.Gems} брюлликов");

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
            }

            Console.WriteLine("\n~ Введите номер товара для покупки или 0, чтобы выйти:");
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
                string result = await service.BuyItemAsync(playerName, items[choice - 1].Name);
                Console.WriteLine(result);
                Console.WriteLine("Нажмите любую клавишу для продолжения.");
            }
            else Console.WriteLine("Некорректный ввод. Нажмите любую клавишу.");

            WaitAndClear();
        }
    }

    static async Task OpenCurrencyExchanger(InventoryService service)
    {
        Console.WriteLine("Функция в разработке...");
        WaitAndClear();
    }

    static void WaitAndClear()
    {
        Console.ReadKey(true);
        Console.Clear();
    }
}