using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WebSocketSharp;
using WebSocketSharp.Server;

namespace Triangle_Streaming_Server
{
	class Program
	{
		static void Main(string[] args)
		{
			WebSocketServer webSocketServer = new WebSocketServer();
			webSocketServer.AddWebSocketService<VideoStream>("/stream");
			webSocketServer.Start();
			Console.ReadKey(true);
			webSocketServer.Stop();
		}
	}

	public class VideoStream : WebSocketBehavior
	{
		protected override void OnMessage(MessageEventArgs e)
		{
			// this.ID can be used for uniquely identifying sessions 

			// Here all the received data should be read
			if (e.IsBinary)
			{
				// Received probably camera bytes
				byte[] receivedBytes = e.RawData;

				// Do somehting with them
			}

			// Send can be used to send data to other devices.
			//Send(fileBytes);
		}
	}
}
