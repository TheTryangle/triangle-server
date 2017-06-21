using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading.Tasks;
using TriangleStreamingServer.Extensions;
using TriangleStreamingServer.WebSockets;

namespace TriangleStreamingServer.Models
{
	public class ChatStream : WebSockets.WebSocketHandler
	{
		private static string _name;

		public ChatStream(WebSocketConnectionManager webSocketConnectionManager, StreamQueueManager streamManager) : base(webSocketConnectionManager, streamManager)
		{
			Clients = new ConcurrentDictionary<string, List<string>>();
		}

		/// <summary>
		/// Own ID, List of streamerIds
		/// </summary>
		public ConcurrentDictionary<string, List<string>> Clients { get; private set; }

		public override async Task OnConnected(WebSocket socket)
		{
			await base.OnConnected(socket);
			Console.WriteLine("Someone Connected");
		}

		public override Task OnDisconnected(WebSocket socket)
		{
			string id = WebSocketConnectionManager.GetId(socket);
            Clients.TryRemove(id, out List<string> removedValue);
            Console.WriteLine("Someone disconnected");
            return base.OnDisconnected(socket);

		}

		public override async Task OnMessage(WebSocket socket, WebSocketReceiveResult result, WebSocketMessageType type, byte[] buffer)
		{
			if (type != WebSocketMessageType.Text)
			{
				throw new NotImplementedException();
			}
			string data = Encoding.UTF8.GetString(buffer).Trim('\0');
			string id = WebSocketConnectionManager.GetId(socket);
			//Console.WriteLine(data);

			ChatAction sendMessage = JsonConvert.DeserializeObject<ChatAction>(data);
            //Console.WriteLine(sendMessage);

			switch (sendMessage.ActionType)
			{
				case ChatAction.Type.JOIN:
					{
                        
						if (!Clients.ContainsKey(id))
						{
							// Not chatting
							Clients.TryAdd(id, new List<string> { sendMessage.StreamId });

						}
						else
						{

                            Console.WriteLine("Someone joined to chat");
                            Clients[id].Add(sendMessage.StreamId);
                        }

                        Console.WriteLine(data);
                        break;
					}
				case ChatAction.Type.LEAVE:
					{

                        Console.WriteLine("Someone left");
                        Clients[id].Remove(sendMessage.StreamId);
						break;
					}
				case ChatAction.Type.MESSAGE:
					{
						var _list = Clients.Where(p => p.Value.Contains(sendMessage.StreamId) && p.Key != id).Select(p => p.Key).ToArray();
						await SendToAll(data, _list);
                        Console.WriteLine(data);
                        break;
					}
				default:
					{
						throw new NotImplementedException("Type has not yet been implemented!");
					}
			}
		}
	}
}