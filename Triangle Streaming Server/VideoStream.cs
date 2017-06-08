using System;
using System.IO;
using WebSocketSharp;
using WebSocketSharp.Server;

namespace Triangle_Streaming_Server
{
	public class VideoStream : WebSocketBehavior
	{
		protected override void OnOpen()
		{
			Console.WriteLine($"Someone connected to send, {this.Context.UserEndPoint.ToString()}");
		}
		protected override void OnMessage(MessageEventArgs e)
		{
			Console.WriteLine("Received data on server");
			// this.ID can be used for uniquely identifying sessions 

			// Here all the received data should be read
			if (e.IsBinary)
			{
				// Received probably camera bytes
				byte[] receivedBytes = e.RawData;

				// Do somehting with them
				StreamQueueManager.GetInstance().AddToQueue(receivedBytes);
			}
			else
			{
				Console.WriteLine("Ignoring received data");
			}
		}
	}
}
