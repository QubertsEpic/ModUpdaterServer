
/// <summary>
/// Program written as server software for the minecraft mod updater latest version.
/// 
/// Program written by Matthew Findlay
/// Project Start: 31/10/2023
/// </summary>
internal class Program
{
    public static Server.Server? ModServer;
    private static void Main(string[] args)
    {
        Console.CancelKeyPress += Console_CancelKeyPress;
        AppDomain.CurrentDomain.ProcessExit += CurrentDomain_ProcessExit;

        ModServer = new Server.Server();
        ModServer.Start();
    }

    private static void CurrentDomain_ProcessExit(object? sender, EventArgs e)
    {
        ModServer?.CloseServer();
    }

    private static void Console_CancelKeyPress(object? sender, ConsoleCancelEventArgs e)
    {
        ModServer?.CloseServer();
    }


}
