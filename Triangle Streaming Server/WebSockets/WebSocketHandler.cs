using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TriangleStreamingServer.WebSockets
{
	public abstract class WebSocketHandler
	{
		protected WebSocketConnectionManager WebSocketConnectionManager { get; set; }

		public WebSocketHandler(WebSocketConnectionManager webSocketConnectionManager)
		{
			WebSocketConnectionManager = webSocketConnectionManager;
		}

		public virtual async Task OnConnected(WebSocket socket)
		{
			WebSocketConnectionManager.AddSocket(socket);
		}

		public virtual async Task OnDisconnected(WebSocket socket)
		{
			await WebSocketConnectionManager.RemoveSocket(WebSocketConnectionManager.GetId(socket));
		}


		public async Task Send(byte[] data, params string[] clients)
		{
			foreach (string client in clients)
			{
				await Send(client, data);
			}
		}

		public async Task Send(string socketId, byte[] data)
		{
			await Send(WebSocketConnectionManager.GetSocketById(socketId), data);
		}

		public async Task Send(WebSocket socket, byte[] data)
		{
			await SendData(socket, data, WebSocketMessageType.Binary);
		}

		public async Task Send(string data, params string[] clients)
		{
			foreach (string client in clients)
			{
				await Send(client, data);
			}
		}

		public async Task Send(string socketId, string message)
		{
			await Send(WebSocketConnectionManager.GetSocketById(socketId), message);
		}

		public async Task Send(WebSocket socket, string message)
		{
			await SendData(socket, Encoding.UTF8.GetBytes(message), WebSocketMessageType.Text);
		}

		private async Task SendData(WebSocket socket, byte[] data, WebSocketMessageType type)
		{
			if (socket.State != WebSocketState.Open)
				return;

			await socket.SendAsync(buffer: new ArraySegment<byte>(array: data,
																  offset: 0,
																  count: data.Length),
								   messageType: type,
								   endOfMessage: true,
								   cancellationToken: CancellationToken.None);
		}

		public abstract Task OnMessage(WebSocket socket, WebSocketReceiveResult result, WebSocketMessageType type, byte[] buffer);
	}
}
