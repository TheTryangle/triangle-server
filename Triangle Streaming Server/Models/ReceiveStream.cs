﻿using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Encodings;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.OpenSsl;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.WebSockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using TriangleStreamingServer.WebSockets;

namespace TriangleStreamingServer.Models
{
	public class ReceiveStream : WebSockets.WebSocketHandler
	{
		private static AsymmetricCipherKeyPair _keyPair;
		private static SHA1 _sha1;

		private static string _pubKey;

		public ReceiveStream(WebSocketConnectionManager webSocketConnectionManager) : base(webSocketConnectionManager)
		{
			_sha1 = SHA1.Create();

			var builder = new ConfigurationBuilder()
				.SetBasePath(Directory.GetCurrentDirectory())
				.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
				.AddEnvironmentVariables();
			var config = builder.Build();
			var path = config["path:privateKey"];
			using (var reader = File.OpenText(path))
			{
				_keyPair = (AsymmetricCipherKeyPair)new PemReader(reader).ReadObject();
			}

			//Get public key
			TextWriter textWriter = new StringWriter();
			PemWriter pemWriter = new PemWriter(textWriter);
			pemWriter.WriteObject(_keyPair.Public);
			pemWriter.Writer.Flush();
			_pubKey = textWriter.ToString();
			textWriter.Flush();

			Clients = new Dictionary<string, string>();
		}

		//A dictionary which keeps track of which clients are watching which streams.
		//Key is client session ID, value is stream ID.
		//TODO: optimize. Searching on value is O(n).
		public Dictionary<string, string> Clients { get; private set; }

		public override async Task OnConnected(WebSocket socket)
		{
			await base.OnConnected(socket);
			Console.WriteLine("Someone connected to receive");
			string socketId = WebSocketConnectionManager.GetId(socket);

			Console.WriteLine("Their ID is {0}", socketId);
		}


		public override async Task OnMessage(WebSocket socket, WebSocketReceiveResult result, WebSocketMessageType type, byte[] buffer)
		{
			if (type != WebSocketMessageType.Text)
			{
				throw new NotImplementedException();
			}

			string id = WebSocketConnectionManager.GetId(socket);
			string data = Encoding.UTF8.GetString(buffer).Trim('\0');

			if (data.StartsWith("WATCH "))
			{
				Console.WriteLine($"{id}: Decided to watch");
				//Strip the "watch" part from the string, leaving just the ID.
				//Example: "WATCH {ID}" becomes "{ID}".
				string streamToWatch = data.Replace("WATCH ", "");

				//If the client is already watching a stream, remove client from stream first.
				if (Clients.ContainsKey(id))
				{
					Clients.Remove(id);
				}

				Clients.Add(id, streamToWatch);
			}
			else if (data.StartsWith("CHALLENGE: "))
			{
				string challengeString = data.Replace("CHALLENGE: ", "");

				//Convert base64 string back to encrypted bytes
				byte[] challengeBytes = Convert.FromBase64String(challengeString);

				//Decrypt bytes into string
				IAsymmetricBlockCipher eng = new Pkcs1Encoding(new RsaEngine());
				eng.Init(false, _keyPair.Private);
				string responseString = Encoding.UTF8.GetString(eng.ProcessBlock(challengeBytes, 0, challengeBytes.Length));

				//Send original message back to client
				await this.Send(socket, $"CHALLENGERESPONSE: {responseString}");
			}
			else
			{
				switch (data)
				{
					case "PUBKEY":
						//Send public key to client
						await this.Send(socket, _pubKey);
						break;
					case "LIST":
						//Send a list of streams to the client.
						string streamsJson = JsonConvert.SerializeObject(StreamQueueManager.GetInstance().Streams.Values.ToList());

						await this.Send(socket, streamsJson);
						break;
					default:
						Console.WriteLine("Unknown command: {0}", data);
						break;
				}
			}
		}

		public override Task OnDisconnected(WebSocket socket)
		{
			string id = WebSocketConnectionManager.GetId(socket);

			Clients.Remove(id);
			return base.OnDisconnected(socket);
		}
	}
}
