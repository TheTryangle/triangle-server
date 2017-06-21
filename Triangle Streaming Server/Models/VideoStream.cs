using Org.BouncyCastle.Crypto;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading.Tasks;
using TriangleStreamingServer.Extensions;
using TriangleStreamingServer.WebSockets;
using Newtonsoft.Json;

namespace TriangleStreamingServer.Models
{
	public class VideoStream : WebSockets.WebSocketHandler
	{
		public VideoStream(WebSocketConnectionManager webSocketConnectionManager, StreamQueueManager streamManager) : base(webSocketConnectionManager, streamManager)
		{
		}

		public override async Task OnConnected(WebSocket socket)
		{
			await base.OnConnected(socket);
			string socketId = WebSocketConnectionManager.GetId(socket);

			Console.WriteLine("Their ID is {0}", socketId);
			StreamManager.Streams.TryAdd(socketId, new Stream(socketId));

			await Send(socket, $"ID: {socketId}");
			await Send(socket, "PUBKEY");
		}

		public override async Task OnMessage(WebSocket socket, WebSocketReceiveResult result, WebSocketMessageType type, byte[] buffer)
		{
			Console.WriteLine("Received data on server");
			string socketId = string.Empty;
			try
			{
				socketId = WebSocketConnectionManager.GetId(socket);
			}
			catch (Exception ex)
			{
				// Unkoown socketId
				Console.WriteLine("unknown id");
				return;
			}
			switch (type)
			{
				case WebSocketMessageType.Binary:
					{
						// binary data
						if (StreamManager.Streams.ContainsKey(socketId))
						{
							Stream stream;
							bool couldGetValue = StreamManager.Streams.TryGetValue(socketId, out stream);

							if (couldGetValue)
							{
								byte[] latestSignature = StreamManager.Streams[socketId].LatestSignature;
								AsymmetricKeyParameter publicKey = StreamManager.Streams[socketId].PublicKey;

								bool validData = buffer.Validate(latestSignature, publicKey);
								if (validData)
								{
									// Valid file
									StreamManager.AddToQueue(socketId, buffer);
								}
								else
								{
									Console.WriteLine($"{socketId}: Data has been tampered with!");
								}
								return;
							}

							Console.WriteLine("Unable to add to queue");
						}
						break;
					}
				case WebSocketMessageType.Text:
					{
						string data = Encoding.UTF8.GetString(buffer).Trim('\0');
						if (data.StartsWith("PUBKEY:"))
						{
							Console.WriteLine($"{socketId}: Received public key");
							// probably public key
							string publicKey = data.Replace("PUBKEY:", "");

							TextReader textReader = new StringReader(publicKey);
							Org.BouncyCastle.OpenSsl.PemReader pemReader = new Org.BouncyCastle.OpenSsl.PemReader(textReader);
							AsymmetricKeyParameter publicKeyParam = (AsymmetricKeyParameter)pemReader.ReadObject();

							if (StreamManager.Streams.ContainsKey(socketId))
							{
								Stream stream;
								bool couldGetValue = StreamManager.Streams.TryGetValue(socketId, out stream);

								if (couldGetValue)
								{
									Stream updatedStream = stream;
									updatedStream.PublicKey = publicKeyParam;
									StreamManager.Streams.TryUpdate(socketId, updatedStream, stream);
								}

								return;
							}
							Console.WriteLine("Unable to set public key");
							return;
						}
						else if (data.StartsWith("HASH:"))
						{
							Console.WriteLine($"{socketId}: Received hash");

							// probably public key
							string signature = data.Replace("HASH:", "");

							byte[] decodedSignature = Convert.FromBase64String(signature);

							if (StreamManager.Streams.ContainsKey(socketId))
							{
								Stream stream;
								bool couldGetValue = StreamManager.Streams.TryGetValue(socketId, out stream);

								if (couldGetValue)
								{
									Stream updatedStream = stream;
									updatedStream.LatestSignature = decodedSignature;
									StreamManager.Streams.TryUpdate(socketId, updatedStream, stream);
								}

								return;
							}
							Console.WriteLine("Unable to set latest signature");
							return;
						}
						else if (data.StartsWith("{") && data.EndsWith("}"))
						{
							//The definition for the expected JSON object.
							var streamInfoDefinition = new { StreamerName = "" };

							try
							{
								//Extract streamer name from JSON, and put it into the streamer's Stream object.
								var streamInfo = JsonConvert.DeserializeAnonymousType(data, streamInfoDefinition);
								StreamManager.Streams[socketId].StreamerName = streamInfo.StreamerName;
							}
							catch (JsonReaderException e)
							{
								Console.WriteLine("Received invalid JSON: {0}", data);
							}
						}
						else if (data.StartsWith("VIEWERCOUNT"))
						{
							int count = StreamManager.ReceivingWebSocket.Clients.Where(x => x.Value.Equals(socketId.ToString())).Count();
							await Send(socket, $"VIEWERCOUNT: {count.ToString()}");
						}

						break;
					}
				default:
					{
						// text data
						Console.WriteLine($"{socketId}: Ignoring received data");
						break;
					}
			}
		}

		public override Task OnDisconnected(WebSocket socket)
		{
			string socketId = WebSocketConnectionManager.GetId(socket);
			StreamManager.Streams.TryRemove(socketId, out Stream stream);

			return base.OnDisconnected(socket);
		}
	}
}
