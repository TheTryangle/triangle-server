using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using TriangleStreamingServer.WebSockets;

namespace TriangleStreamingServer.WebSockets
{
	public class WebSocketMiddleware
	{
		private readonly RequestDelegate _next;
		private WebSocketHandler _webSocketHandler { get; set; }

		public WebSocketMiddleware(RequestDelegate next, WebSocketHandler webSocketHandler)
		{
			_next = next;
			_webSocketHandler = webSocketHandler;
		}

		public async Task Invoke(HttpContext context)
		{
			if (!context.WebSockets.IsWebSocketRequest)
				return;

			var socket = await context.WebSockets.AcceptWebSocketAsync();
			await _webSocketHandler.OnConnected(socket);

			try
			{
				await Receive(socket, async (result, buffer) =>
				{
					if (socket.State == WebSocketState.Aborted || socket.State == WebSocketState.Closed || socket.State == WebSocketState.CloseReceived)
					{
						await _webSocketHandler.OnDisconnected(socket);
						return;
					}

					if (result.MessageType == WebSocketMessageType.Text || result.MessageType == WebSocketMessageType.Binary)
					{
						await _webSocketHandler.OnMessage(socket, result, result.MessageType, buffer);
						return;
					}

					else if (result.MessageType == WebSocketMessageType.Close)
					{
						await _webSocketHandler.OnDisconnected(socket);
						return;
					}

				});
			}
			catch(IOException e)
			{
				await _webSocketHandler.OnDisconnected(socket);
			}
		}

		private async Task Receive(WebSocket socket, Action<WebSocketReceiveResult, byte[]> handleMessage)
		{
			var buffer = new ArraySegment<byte>(new byte[1024 * 4]);

			while (socket.State == WebSocketState.Open)
			{
				using (var ms = new MemoryStream())
				{
					WebSocketReceiveResult result;
					do
					{
						result = await socket.ReceiveAsync(buffer, CancellationToken.None);

						ms.Write(buffer.Array, buffer.Offset, result.Count);
					} while (!result.EndOfMessage);

					ms.Seek(0, SeekOrigin.Begin);

					handleMessage(result, ms.ToArray());
				}
			}
		}
	}
}
