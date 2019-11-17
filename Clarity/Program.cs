using Clarity.HttpServer;
using Clarity.Web;
using System;

namespace Clarity
{
    public class Program
    {
        private const string Host = "localhost";

        private const int Port = 9091;

        public static void Main(string[] args)
        {
            try
            {
                var server = new TcpServer(Host, Port);
                var factory = new HttpApplicationFactory<MyApplication>();
                server.Start(factory);
            }
            catch (Exception exception)
            {
                Console.WriteLine($"An unexpected error occurred while starting a server at {Host}:{Port}.\r\nError: {exception.Message}");
            }

            Console.ReadLine();
        }
    }
}