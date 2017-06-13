using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WebSocketSharp.Server;

namespace Triangle_Streaming_Server
{
	public class WebSocketManager : IDisposable
	{
		private static WebSocketManager mInstance;

		public static WebSocketManager GetInstance()
		{
			if (mInstance == null)
				mInstance = new WebSocketManager();

			return mInstance;
		}

		public WebSocketServer Server { get; set; }

		private WebSocketManager()
		{
			InitializeServer();
		}

		private void InitializeServer()
		{
			Server = new WebSocketServer(1234);
			Server.AddWebSocketService<VideoStream>("/send");
			Server.AddWebSocketService<ReceiveStream>("/receive");

			ReceiveStream.Initialize();

			Server.Start();
		}

		public void Dispose()
		{
			Server.Stop();
		}
	}
}
