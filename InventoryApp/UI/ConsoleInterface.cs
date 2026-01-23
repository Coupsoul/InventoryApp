using InventoryApp.Enums;
using InventoryApp.Services;

namespace InventoryApp.UI
{
    public class ConsoleInterface
    {
        private readonly InventoryService _invService;
        private readonly CurrencyService _curService;

        public ConsoleInterface(InventoryService invService, CurrencyService curService)
        {
            _invService = invService;
            _curService = curService;
        }


        public async Task RunMainMenuAsync(string playerName)
        {
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
                        await ShowInventoryAsync(playerName); break;
                    case "2":
                        await ShoppingAsync(playerName); break;
                    case "3":
                        await OpenLombardAsync(playerName); break;
                    case "4":
                        await GrindAsync(playerName); break;
                    case "5":
                        await OpenCurrencyExchangerAsync(playerName); break;
                    case "0":
                        exit = true; break;
                    default:
                        Console.WriteLine("Неизвестная команда."); WaitAndClear(); break;
                }
            }
        }


        private async Task ShowInventoryAsync(string playerName)
        {
            Console.Clear();
            Console.WriteLine("----------------  ИНВЕНТАРЬ  ----------------");

            var player = await _invService.GetPlayerAsync(playerName);

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


        private async Task ShoppingAsync(string playerName)
        {
            Console.Clear();
            var items = await _invService.GetShopItemsAsync();
            bool exit = false;

            while (!exit)
            {
                var player = await _invService.GetPlayerAsync(playerName);
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
                    Console.WriteLine(await _invService.BuyItemAsync(playerName, items[choice - 1].Name));
                    Console.WriteLine("Нажмите любую клавишу для продолжения.");
                }
                else Console.WriteLine("Некорректный ввод. Нажмите любую клавишу.");

                WaitAndClear();
            }
        }


        private async Task OpenLombardAsync(string playerName)
        {
            Console.Clear();
            bool exit = false;
            while (!exit)
            {
                var player = await _invService.GetPlayerAsync(playerName);
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
                    Console.WriteLine(await _invService.SellItemAsync(playerName, invItems[choice - 1].Item.Name));
                    Console.WriteLine("Нажмите любую клавишу для продолжения.");
                }
                else Console.WriteLine("Некорректный ввод. Нажмите любую клавишу.");

                WaitAndClear();
            }
        }


        private async Task GrindAsync(string playerName)
        {
            Console.Clear();
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

                var (gold, gems) = await _invService.ProcessGrindAsync(playerName);

                Console.WriteLine($"\nЗалутано {gold} золота и {gems} брюлликов.");
            }
            finally
            {
                Console.CursorVisible = true;
            }

            Console.WriteLine("\nНажмите любую клавишу.");
            WaitAndClear();
        }


        private async Task OpenCurrencyExchangerAsync(string playerName)
        {
            Console.Clear();
            Console.WriteLine("Загрузка курса с биржи...");
            int curRate = await _curService.GetGemPriceInGoldAsync();

            bool exit = false;
            while (!exit)
            {
                var player = await _invService.GetPlayerAsync(playerName);

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
                        await TryExchangeAsync(playerName, curRate, isBuying: true); break;
                    case "2":
                        await TryExchangeAsync(playerName, curRate, isBuying: false); break;
                    case "0":
                        exit = true; Console.Clear(); return;
                    default:
                        Console.WriteLine("Неизвестная команда"); break;
                }

                WaitAndClear();
            }
        }


        private async Task TryExchangeAsync(string playerName, int rate, bool isBuying)
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
                    string reuslt = await _invService.ExchangeGemsAsync(playerName, gemChange, rate);
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


        void WaitAndClear()
        {
            Console.ReadKey(true);
            Console.Clear();
        }
    }
}
