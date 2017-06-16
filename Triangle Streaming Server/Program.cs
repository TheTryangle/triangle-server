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
			using (WebSocketManager webManager = WebSocketManager.GetInstance())
			{
				Console.ReadKey();
			}
		}
	}
}
