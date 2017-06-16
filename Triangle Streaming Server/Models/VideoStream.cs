using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Threading.Tasks;
using TriangleStreamingServer.WebSockets;

namespace TriangleStreamingServer.Models
{
	public class VideoStream : WebSockets.WebSocketHandler
	{
		public VideoStream(WebSocketConnectionManager webSocketConnectionManager) : base(webSocketConnectionManager)
		{
		}

		public override async Task OnConnected(WebSocket socket)
		{
			await base.OnConnected(socket);
			string socketId = WebSocketConnectionManager.GetId(socket);

			Console.WriteLine("Their ID is {0}", socketId);
			StreamQueueManager.GetInstance().Streams.TryAdd(socketId, new Stream(socketId));
		}

		public override async Task OnMessage(WebSocket socket, WebSocketReceiveResult result, WebSocketMessageType type, byte[] buffer)
		{
			Console.WriteLine("Received data on server");
			switch (type)
			{
				case WebSocketMessageType.Binary:
					{
						// binary data
						string socketId = WebSocketConnectionManager.GetId(socket);

						StreamQueueManager.GetInstance().AddToQueue(socketId, buffer);
						break;
					}
				default:
					{
						// text data
						Console.WriteLine("Ignoring received data");
						break;
					}
			}
		}

		public override Task OnDisconnected(WebSocket socket)
		{
			string socketId = WebSocketConnectionManager.GetId(socket);
			StreamQueueManager.GetInstance().Streams.TryRemove(socketId, out Stream stream);

			return base.OnDisconnected(socket);
		}
	}
}
