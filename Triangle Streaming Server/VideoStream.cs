using Org.BouncyCastle.Crypto;
using System;
using System.IO;
using Triangle_Streaming_Server.Extensions;
using WebSocketSharp;
using WebSocketSharp.Server;

namespace Triangle_Streaming_Server
{
	public class VideoStream : WebSocketBehavior
	{
		protected override void OnOpen()
		{
			Console.WriteLine($"Someone connected to send, {this.Context.UserEndPoint.ToString()}");
			Console.WriteLine($"Their ID is {this.ID}");

			Send("PUBKEY");

			StreamQueueManager.GetInstance().Streams.Add(this.ID, new Stream(this.ID));
		}

		protected override void OnMessage(MessageEventArgs e)
		{
			if (e.IsText)
			{
				if (e.Data.StartsWith("PUBKEY:"))
				{
					Console.WriteLine($"{ID}: Received public key");
					// probably public key
					string publicKey = e.Data.Replace("PUBKEY:", "");

					TextReader textReader = new StringReader(publicKey);
					Org.BouncyCastle.OpenSsl.PemReader pemReader = new Org.BouncyCastle.OpenSsl.PemReader(textReader);
					AsymmetricKeyParameter publicKeyParam = (AsymmetricKeyParameter)pemReader.ReadObject();

					StreamQueueManager.GetInstance().Streams[this.ID].PublicKey = publicKeyParam;
					return;
				}

				if (e.Data.StartsWith("HASH:"))
				{
					Console.WriteLine($"{ID}: Received hash");

					// probably public key
					string signature = e.Data.Replace("HASH:", "");

					byte[] decodedSignature = Convert.FromBase64String(signature);

					StreamQueueManager.GetInstance().Streams[this.ID].LatestSignature = decodedSignature;
					return;
				}

			}

			// Here all the received data should be read
			if (e.IsBinary)
			{
				// Received probably camera bytes
				byte[] receivedBytes = e.RawData;
				byte[] latestSignature = StreamQueueManager.GetInstance().Streams[this.ID].LatestSignature;
				AsymmetricKeyParameter publicKey = StreamQueueManager.GetInstance().Streams[this.ID].PublicKey;

				bool validData = receivedBytes.Validate(latestSignature, publicKey);

				if (validData)
				{
					Console.WriteLine($"{ID}: Data is valid.");
					// Valid file
					StreamQueueManager.GetInstance().AddToQueue(this.ID, receivedBytes);
				}
				else
				{
					Console.WriteLine($"{ID}: Data has been tampered with!");
				}
			}
			else
			{
				Console.WriteLine($"{ID}: Ignoring the received data");
			}
		}

		protected override void OnClose(CloseEventArgs e)
		{
			StreamQueueManager.GetInstance().Streams.Remove(this.ID);
		}
	}
}
