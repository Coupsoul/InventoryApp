using InventoryApp.Entities;
using InventoryApp.Enums;

namespace InventoryApp.Data
{
    public class DbSeeder
    {
        public static void Seed(ApplicationContext context)
        {
            if(context.Items.Any())
                return;

            var items = new List<Item>
            {
                new Item("Ржавая вилка", Currency.Gold, 3, "Есть такой не стоит, но пару раз куда-нибудь воткнуть - сойдёт."),
                new Item("Шнур", Currency.Gold, 1, "Верёвка длиной не больше локтя."),
                new Item("Случайное зелье", Currency.Gems, 4, "Пока не выпьешь - не узнаешь."),
                new Item("Палка", Currency.Gold, 0, "Что-то там про раз в год...")
            };

            context.Items.AddRange(items);

            if (!context.Players.Any())
            {
                context.Players.Add(new Player
                {
                    Name = "Player_01",
                    Gold = 10,
                    Gems = 4
                });
            }

            context.SaveChanges();
        }
    }
}
