using static BCrypt.Net.BCrypt;
using InventoryApp.Entities;
using System.Text.Json;

namespace InventoryApp.Data
{
    public class DbSeeder
    {
        public static void Seed(ApplicationContext context)
        {
            if (!context.Items.Any())
            {
                var filePath = "default_items.json";

                if (File.Exists(filePath))
                {
                    var jsonString = File.ReadAllText(filePath);

                    var items = JsonSerializer.Deserialize<List<Item>>(jsonString);

                    if (items != null && items.Any())
                    {
                        context.Items.AddRange(items);
                        context.SaveChanges();
                        Console.WriteLine($"Загружено {items.Count} предметов по умолчанию.");
                        Thread.Sleep(1500);
                    }
                }
                else
                {
                    Console.WriteLine("Предметы по умолчанию не найдены.");
                    Thread.Sleep(1500);
                }
            }

            if (!context.Players.Any(p => p.IsAdmin))
            {
                context.Players.Add(new Player
                {
                    Name = "admin",
                    PasswordHash = HashPassword("admin"),
                    Gold = 99999,
                    Gems = 99999,
                    IsAdmin = true
                });
            }

            context.SaveChanges();
        }
    }
}
