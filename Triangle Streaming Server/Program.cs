using System;
using Microsoft.Owin.Hosting;
using Triangle_Streaming_Server.WebApi;

namespace Triangle_Streaming_Server
{
	class Program
	{
		static void Main(string[] args)
		{
            string baseAddressWebApi = "http://localhost:9000/";
            WebSocketManager webManager = WebSocketManager.GetInstance();
            WebApp.Start<WebApiStart>(baseAddressWebApi);
            Console.WriteLine("Press any key to close server.");
            Console.ReadKey();                  
        }
	}
}
