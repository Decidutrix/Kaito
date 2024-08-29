using System;
using System.Threading.Tasks;

namespace Kaito

{
    class Program
    {
        static async Task Main(string[] args)
        {
            var bot = new Bot();
            await bot.RunAsynce();
        }
    }
}
