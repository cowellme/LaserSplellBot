using JobBe;

namespace LaserSplellBot
{
    internal class Program
    {
        static void Main(string[] args)
        {
            // 6241876779:AAHFNktkfY2P6IWDxmGkrD-HNoZqSyPpaz0
            Testing();
            Console.WriteLine("Hello, World!");
            _ = new TBot(@"6241876779:AAHFNktkfY2P6IWDxmGkrD-HNoZqSyPpaz0");
            while (true)
            {
                var key = Console.ReadKey().Key;
                if (key == ConsoleKey.E) return;

            }
        }

        private static void Testing()
        {
            using var db = new ApplicationContext(true);
        }
    }
}
