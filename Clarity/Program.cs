using Clarity.HttpServer;
using Clarity.Web;
using Serilog;
using System;

namespace Clarity
{
    public class Program
    {
        private const string Host = "localhost";

        private const int Port = 9091;

        public static void Main(string[] args)
        {
            ConfigureLogging();

            try
            {
                var server = new TcpServer(Host, Port);
                var factory = new HttpApplicationFactory<MyApplication>();
                server.Start(factory);

                Log.Information("TCP server started at {Host}:{Port}.", Host, Port);
            }
            catch (Exception exception)
            {
                Log.Fatal("An unexpected error occurred while starting a server at {Host}:{Port}.\r\nError: {exception.Message}", Host, Port, exception.Message);
            }

            Console.ReadLine();
        }

        private static void ConfigureLogging()
        {
            Log.Logger = new LoggerConfiguration()
                .WriteTo.Console()
                .CreateLogger();
        }
    }
}