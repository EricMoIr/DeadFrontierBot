namespace DFBot
{
    class Run
    {
        static void Main(string[] args)
        {
            BotService.RunAsync().GetAwaiter().GetResult();
        }
    }
}
