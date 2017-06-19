using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WebSocketSharp.Server;

namespace Triangle_Streaming_Server
{
	class Program
	{
		static void Main(string[] args)
		{
            string baseAddressWebApi = "http://145.49.44.137:9000/";
            WebSocketManager webManager = WebSocketManager.GetInstance();
            WebApp.Start<WebApiStart>(baseAddressWebApi);
            Console.WriteLine("Press any key to close server.");
            Console.ReadKey();                  
        }
	}
}
