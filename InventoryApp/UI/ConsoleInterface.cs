using InventoryApp.Entities;
using InventoryApp.Enums;
using InventoryApp.Services.Interfaces;

namespace InventoryApp.UI
{
    public class ConsoleInterface
    {
        private readonly Random _rnd = new Random();
        private readonly IInventoryService _invService;
        private readonly ICurrencyService _curService;
        private readonly IUserService _userService;

        public ConsoleInterface(IInventoryService invService, ICurrencyService curService, IUserService userService)
        {
            _invService = invService;
            _curService = curService;
            _userService = userService;
        }


        private string ReadLineWithLimit(int maxLength, bool isPassword = false, bool withNewLine = true)
        {
            string input = "";
            ConsoleKeyInfo key;

            do
            {
                key = Console.ReadKey(intercept: true);

                if (key.Key == ConsoleKey.Backspace && input.Length > 0)
                {
                    input = input.Remove(input.Length - 1);
                    Console.Write("\b \b");
                }
                else if (!char.IsControl(key.KeyChar) && key.KeyChar != '\u0000' && input.Length < maxLength)
                {
                    input += key.KeyChar;
                    Console.Write(isPassword ? '*' : key.KeyChar);
                }
            } while (key.Key != ConsoleKey.Enter || input.Length == 0);

            if (withNewLine) { Console.WriteLine(); }
            return input;
        }


        public async Task<Player> AuthorizeAsync()
        {
            Console.Clear();
            Console.WriteLine("===============  АВТОРИЗАЦИЯ  ===============");

            while (true)
            {
                Console.Write("\nВведите имя: ");
                string playerName = ReadLineWithLimit(50);

                try
                {
                    bool exists = await _userService.CheckExistPlayerAsync(playerName);

                    if (exists)
                    {
                        Console.Write("\nВведите пароль: ");
                        string password = ReadLineWithLimit(20, true);

                        var player = await _userService.SignInAsync(playerName, password);

                        if (player != null)
                        {
                            Console.WriteLine($"\nС возвращением, {playerName}!");
                            await Task.Delay(1500);
                            Console.Clear();
                            return player;
                        }
                        else
                        {
                            Console.WriteLine("\nНеверно введён пароль. Попробуйте снова.\n" + new string('_', 45));
                        }
                    }
                    else
                    {
                        Console.WriteLine("\nИгрока с таким именем ещё нет. Приступим к регистрации.");

                        string pass1, pass2;
                        do
                        {
                            Console.Write("\nВведите пароль: ");
                            pass1 = ReadLineWithLimit(20, true);
                            Console.Write("Повторите пароль: ");
                            pass2 = ReadLineWithLimit(20, true);

                            if (pass1 != pass2) Console.WriteLine("\nПароли не совпадают!\n" + new string('_', 45));
                        }
                        while (pass1 != pass2);

                        var newPlayer = await _userService.RegisterAsync(playerName, pass2);
                        Console.WriteLine($"\nРегистрация успешна! Добро пожаловать, {playerName}.");
                        await Task.Delay(1500);
                        Console.Clear();
                        return newPlayer;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"\nОшибка: {ex.Message}");
                }
            }
        }


        public async Task RunMainMenuAsync(Player player)
        {
            string playerName = player.Name;
            bool isAdmin = player.IsAdmin;
            bool exit = false;
            while (!exit)
            {
                Console.WriteLine($"[{playerName}]");

                Console.WriteLine("\nВыберите действие");
                Console.WriteLine("1. Мой инвентарь");
                Console.WriteLine("2. Магазин");
                Console.WriteLine("3. Ломбард");
                Console.WriteLine("4. Гринд");
                Console.WriteLine("5. Обмен валют");
                if (isAdmin)
                {
                    Console.ForegroundColor = ConsoleColor.DarkYellow;
                    Console.WriteLine("999. Привилегии");
                    Console.ResetColor();
                }
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
                    case "999":
                        if (isAdmin) { await RunAdminMenuAsync(playerName); }
                        else { Console.WriteLine("Неизвестная команда."); WaitAndClear(); }
                        break;
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

            try
            {
                var player = await _invService.GetPlayerWithInventoryAsync(playerName);


                if (player != null)
                {
                    Console.WriteLine($"{player.Name}");
                    Console.WriteLine($"Баланс: {player.Gold} золота | {player.Gems} брюлликов");
                    Console.Write("Инвентарь:");

                    if (player.Inventory.Count != 0)
                    {
                        Console.WriteLine();
                        foreach (var slot in player.Inventory)
                        {
                            Console.WriteLine($" [{slot.Amount} шт] {slot.Item.Name}");
                            if (!string.IsNullOrEmpty(slot.Item.Description))
                                Console.WriteLine($"  \"{slot.Item.Description}\"");
                        }
                    }
                    else
                        Console.WriteLine(" пуст.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка загрузки инвентаря: {ex.Message}");
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
                try
                {
                    var player = await _invService.GetPlayerWithInventoryAsync(playerName);
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
                            Console.WriteLine($"    \"{item.Description}\"");
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
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Ошибка покупки: {ex.Message}");
                }
                WaitAndClear();
            }
        }


        private async Task OpenLombardAsync(string playerName)
        {
            Console.Clear();
            bool exit = false;
            while (!exit)
            {
                try
                {
                    var player = await _invService.GetPlayerWithInventoryAsync(playerName);
                    if (player == null)
                    {
                        Console.WriteLine("Игрок не найден.");
                        exit = true;
                        return;
                    }

                    Console.WriteLine("--------------  СКУПОЙ РЫЦАРЬ  --------------");
                    Console.WriteLine($"Баланс: {player.Gold} золота | {player.Gems} брюлликов.");
                    Console.Write("Ваш инвентарь:");

                    if (player.Inventory.Count == 0)
                    {
                        Console.WriteLine(" пуст.");
                        Console.WriteLine("\nЗаходи, когда будет что продать.");
                        WaitAndClear();
                        return;
                    }

                    Console.WriteLine();
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
                        Console.WriteLine("\nНажмите любую клавишу для продолжения.");
                    }
                    else Console.WriteLine("Некорректный ввод. Нажмите любую клавишу.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Ошибка продажи: {ex.Message}");
                }

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
            }
            finally
            {
                Console.CursorVisible = true;
            }

            try
            {
                var (gold, gems) = await _invService.ProcessGrindAsync(playerName);

                if (gold == 0 && gems == 0)
                {
                    Console.WriteLine("\nКошелёк переполнен! Вы не смогли ничего унести.");
                }
                else
                {
                    Console.WriteLine($"\nЗалутано {gold} золота и {gems} брюлликов.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Не получилось погриндить: {ex.Message}");
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
                try
                {
                    var player = await _invService.GetPlayerWithInventoryAsync(playerName);

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
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Ошибка в обменнике: {ex.Message}");
                }

                WaitAndClear();
            }
        }


        private async Task TryExchangeAsync(string playerName, int rate, bool isBuying)
        {
            string actionText = isBuying ? "получить" : "обменять";
            Console.Write($"\nВведите количество Брюлликов, которое хотите {actionText}: ");

            if (!int.TryParse(ReadLineWithLimit(Player.MaxGems.ToString().Length), out int amount) || amount <= 0)
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
                    int gemsToExchange = isBuying ? amount : -amount;
                    string result = await _invService.ExchangeGemsAsync(playerName, gemsToExchange, rate);
                    Console.WriteLine(result);
                    break;
                }
                else if (key == ConsoleKey.Escape)
                {
                    Console.WriteLine("Операция отменена.");
                    break;
                }
            }
        }


        private async Task RunAdminMenuAsync(string playerName)
        {
            bool exit = false;
            while (!exit)
            {
                Console.Clear();
                Console.WriteLine("~~~~~~~~~~~~~~~  ПРИВИЛЕГИИ  ~~~~~~~~~~~~~~~");
                Console.WriteLine("\n1. Управлять состоянием");
                Console.WriteLine("2. Учредить предмет");
                Console.WriteLine("3. Наделить привилегиями");
                Console.WriteLine("0. Выход");
                Console.Write("> ");

                var input = Console.ReadLine();

                switch (input)
                {
                    case "1":
                        await ManageFinances(playerName); break;
                    case "2":
                        await EstablishItemAsync(playerName); break;
                    case "3":
                        await GrantPrivilegesAsync(playerName); break;
                    case "0":
                        exit = true; Console.Clear(); return;
                    default:
                        Console.WriteLine("Неизвестная команда."); WaitAndClear(); break;
                }
            }

            WaitAndClear();
        }


        private async Task GrantPrivilegesAsync(string playerName)
        {
            Console.Clear();
            Console.WriteLine("---------------  ПОСВЯЩЕНИЕ  ---------------");
            Console.Write("\nВведите имя посвящаемого: ");
            string newAdminName = ReadLineWithLimit(50);
            try
            {
                if (!(await _userService.CheckExistPlayerAsync(newAdminName)))
                {
                    Console.WriteLine("Какая-то ошибка... Такого игрока нет.");
                    WaitAndClear();
                    return;
                }

                Console.Write("\nВведите свой пароль для подтверждения: ");
                string passCheck = ReadLineWithLimit(20, true);

                await _userService.GrantAdminRightsAsync(newAdminName, playerName, passCheck);
                Console.WriteLine($"{newAdminName} теперь один из нас!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка посвящения: {ex.Message}");
            }

            WaitAndClear();
        }


        private async Task ManageFinances(string playerName)
        {
            Console.Clear();
            Console.WriteLine("-----------------  КОШЕЛЁК  -----------------");

            int goldLength = Player.MaxGold.ToString().Length;
            int gemsLength = Player.MaxGems.ToString().Length;

            string part1 = "\"Хочу, чтоб у меня было ";
            string part2 = " золота и ";
            string part3 = " брюлликов...\"";

            Console.Write(part1);
            Console.Write(new string('_', goldLength));
            Console.Write(part2);
            Console.Write(new string('_', gemsLength));
            Console.WriteLine(part3);

            int cursorTop = Console.CursorTop - 1;

            Console.SetCursorPosition(part1.Length, cursorTop);
            if (!int.TryParse(ReadLineWithLimit(goldLength, withNewLine: false), out int goldToSet) || goldToSet < 0)
            {
                Console.WriteLine("\n\nЖелание не сбылось (ошибка ввода золота).");
                WaitAndClear();
                return;
            }

            int gemsCursorLeft = part1.Length + goldLength + part2.Length;

            Console.SetCursorPosition(gemsCursorLeft, cursorTop);
            if (!int.TryParse(ReadLineWithLimit(gemsLength, withNewLine: false), out int gemsToSet) || gemsToSet < 0)
            {
                Console.WriteLine("\n\nЖелание не сбылось (ошибка ввода брюлликов).");
                WaitAndClear();
                return;
            }

            try
            {
                await _invService.SetBalanceAsync(playerName, goldToSet, gemsToSet);
                Console.SetCursorPosition(0, cursorTop + 2);
                Console.WriteLine("Да будет так!");
            }
            catch (Exception ex)
            {
                Console.SetCursorPosition(0, cursorTop + 2);
                Console.WriteLine($"Ошибка установки баланса: {ex.Message}");
            }

            WaitAndClear();
        }


        private async Task EstablishItemAsync(string playerName)
        {
            Console.Clear();
            Console.WriteLine("-----------  ДОБАВЛЕНИЕ В РЕЕСТР  -----------");
            Console.Write("\nНаименование: ");
            string itemName = ReadLineWithLimit(100);
            Console.Write("\nОписание (необязательно): ");
            string? description = Console.ReadLine();

            Currency currency;
            while (true)
            {
                Console.WriteLine("\nВалюта стоимости:");
                Console.WriteLine(" 1) Золото");
                Console.WriteLine(" 2) Брюллики");
                Console.Write("> ");
                string? input = Console.ReadLine();

                if (input == "1") { currency = Currency.Gold; break; }
                else if (input == "2") { currency = Currency.Gems; break; }

                Console.WriteLine("Неизвестная команда.\nНажмите любую клавишу, чтобы попробовать выбрать заново.");
                Console.ReadKey(true);
            }

            int price;
            while (true)
            {
                int maxDigits = (currency == Currency.Gold ? Player.MaxGold : Player.MaxGems).ToString().Length;
                Console.Write("\nСтоимость: ");
                if (!int.TryParse(ReadLineWithLimit(maxDigits), out price) || price < 0)
                {
                    Console.WriteLine("Цена должна быть неотрицательным числом. Попробуйте ещё раз.");
                    Console.ReadKey(true);
                }
                else
                    break;
            }

            try{
                string result = await _invService.CreateItemAsync(playerName, itemName, currency, price, description);
                Console.WriteLine("\n" + result);
            }
            catch(Exception ex)
            {
                Console.WriteLine($"Ошибка создания предмета: {ex.Message}");
            }

            WaitAndClear();
        }


        void WaitAndClear()
        {
            Console.ReadKey(true);
            Console.Clear();
        }
    }
}
