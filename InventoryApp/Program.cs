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
                Console.WriteLine("3. Ломбард");
                Console.WriteLine("4. Гринд");
                Console.WriteLine("5. Обмен валют");
                Console.WriteLine("0. Выход");
                Console.Write("> ");

                string? input = Console.ReadLine();
                Console.WriteLine("\n");

                switch (input)
                {
                    case "1":
                        await ShowInventory(service, playerName); break;
                    case "2":
                        await Shopping(service, playerName); break;
                    case "3":
                        await OpenLombard(service, playerName); break;
                    case "4":
                        await Grind(service, playerName); break;
                    case "5":
                        await OpenCurrencyExchanger(service, playerName); break;
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

        Console.WriteLine("\nНажмите любую клавишу для продолжения.");
        WaitAndClear();
    }

    static async Task Shopping(InventoryService service, string playerName)
    {
        var items = await service.GetShopItemsAsync();
        bool exit = false;

        while (!exit)
        {
            var player = await service.GetPlayerAsync(playerName);
            if (player != null)
                Console.WriteLine($"Баланс: {player.Gold} золота | {player.Gems} брюлликов");

            Console.WriteLine("\n----------------  СЕМЁРОЧКА  ----------------");

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
        bool exit = false;
        while (!exit)
        {
            var player = await service.GetPlayerAsync(playerName);
            if (player == null)
            {
                Console.WriteLine("Игрок не найден");
                exit = true;
                return;
            }

            Console.WriteLine("--------------  СКУПОЙ РЫЦАРЬ  --------------");
            Console.WriteLine($"Баланс: {player.Gold} золота | {player.Gems} брюлликов");
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
        Console.CursorVisible = false;
        try
        {
            string[] activities = [
            "Зачистка данжа",
            "Фарминг мобов",
            "Пылесосинг локации"];
            string phrase = activities[new Random().Next(activities.Length)];

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

            var rnd = new Random();
            int goldRew = rnd.Next(10, 15);
            int gemsRew = rnd.Next(1, 4);

            var result = await service.AddRewardAsync(playerName, goldRew, gemsRew);
            Console.WriteLine($"\n{result}");
        }
        finally
        {
            Console.CursorVisible = true;
        }

        WaitAndClear();
    }

    static async Task OpenCurrencyExchanger(InventoryService service, string playerName)
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