using Clarity.Web;
using System;

namespace Clarity
{
    public class MyApplication : HttpApplication
    {
        protected void Application_Start()
        {
            var color = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"Application_Start fired in {GetType()}");

            Console.ForegroundColor = color;
        }
    }
}