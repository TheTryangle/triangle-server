﻿using Newtonsoft.Json;
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

			await Send(socket, "PUBKEY");
		}

		public override async Task OnMessage(WebSocket socket, WebSocketReceiveResult result, WebSocketMessageType type, byte[] buffer)
		{
			Console.WriteLine("Received data on server");
			string socketId = WebSocketConnectionManager.GetId(socket);
			switch (type)
			{
				case WebSocketMessageType.Binary:
					{
						// binary data

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

							StreamManager.Streams[socketId].PublicKey = publicKeyParam;
							return;
						}
						else if (data.StartsWith("HASH:"))
						{
							Console.WriteLine($"{socketId}: Received hash");

							// probably public key
							string signature = data.Replace("HASH:", "");

							byte[] decodedSignature = Convert.FromBase64String(signature);

							StreamManager.Streams[socketId].LatestSignature = decodedSignature;
							return;
						}
                        else if (data.StartsWith("USERLIST"))
                        {
                            //Send a list of user of the stream
                            await this.Send(JsonConvert.SerializeObject(ReceiveStream.GetUserListPerStreamer(socketId)));
                        }

                        else if (data.StartsWith("VIEWERCOUNT"))
                        {
                            //Get the amount of viewers for this stream
                            await this.Send(String.Format("VIEWERCOUNT: {0}", ReceiveStream.GetViewerAmountByStream(socketId)));

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
