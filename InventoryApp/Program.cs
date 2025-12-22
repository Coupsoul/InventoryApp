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


            Console.WriteLine("----- ИНВЕНТАРЬ -----");
            var player = await service.GetPlayerAsync(playerName);
            if (player != null)
            {
                Console.WriteLine($"{player.Name}: Gold={player.Gold}, Gems={player.Gems}");
                Console.WriteLine("Инвентарь:");
                foreach (var slot in player.Inventory)
                {
                    Console.WriteLine($"  [{slot.Amount} шт] {slot.Item.Name}");
                }
            }

            Console.WriteLine("\n");

            Console.WriteLine("----------------  СЕМЁРОЧКА  ----------------" + "\nСписок товаров:");
            var items = await service.GetShopItemsAsync();
            foreach (var i in items)
            {
                string currencyName = i.PriceCurrency switch
                {
                    Currency.Gold => "золота",
                    Currency.Gems => "брюлликов",
                    _ => i.PriceCurrency.ToString() // На случай добавления новых типов
                };
                var price = i.Price == 0 ? "бесплатно" : $"{i.Price} {currencyName}";
                Console.WriteLine($" ~ {i.Name} - {price}");
                if (!string.IsNullOrEmpty(i.Description))
                    Console.WriteLine($" \"{i.Description}\"\n");
            }

            //Console.WriteLine("\n--------  ПРОВЕРКА ПОКУПАТЕЛЬНЫХ ВОЗМОЖНОСТЕЙ  --------");
            //Console.WriteLine("Берём вилку...");
            //var result1 = await service.BuyItemAsync(playerName, "Ржавая вилка");
            //Console.WriteLine(result1);

            //Console.WriteLine("Берём зелье...");
            //var result2 = await service.BuyItemAsync(playerName, "Случайное зелье");
            //Console.WriteLine(result2);

            //Console.WriteLine("Берём палку...");
            //var result3 = await service.BuyItemAsync(playerName, "Палка");
            //Console.WriteLine(result3);

            //Console.WriteLine("Берём вторую вилку...");
            //var result4 = await service.BuyItemAsync(playerName, "Ржавая вилка");
            //Console.WriteLine(result4);

            //Console.WriteLine("Пытаемся взять второе зелье...");
            //var result5 = await service.BuyItemAsync(playerName, "Случайное зелье");
            //Console.WriteLine(result5);

            Console.ReadKey();
        }
    }
}