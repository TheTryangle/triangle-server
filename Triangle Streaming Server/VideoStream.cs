using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Encodings;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Security;
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using WebSocketSharp;
using WebSocketSharp.Server;

namespace Triangle_Streaming_Server
{
	public class VideoStream : WebSocketBehavior
	{
		private static SHA1CryptoServiceProvider _sha1;

		protected override void OnOpen()
		{
			Console.WriteLine($"Someone connected to send, {this.Context.UserEndPoint.ToString()}");
			Console.WriteLine("Their ID is {0}", this.ID);

			Send("PUBKEY");

			StreamQueueManager.GetInstance().Streams.Add(this.ID, new Stream(this.ID));
		}

		protected override void OnMessage(MessageEventArgs e)
		{
			Console.WriteLine("Received data on server");
			// this.ID can be used for uniquely identifying sessions 

			if (e.IsText)
			{
				if (e.Data.StartsWith("PUBKEY:"))
				{
					Console.WriteLine("Received public key");
					// probably public key
					string publicKey = e.Data.Replace("PUBKEY:", "");

					byte[] publicKeyBytes = Encoding.UTF8.GetBytes(publicKey);
					AsymmetricKeyParameter publicKeyParam = PublicKeyFactory.CreateKey(publicKeyBytes);

					StreamQueueManager.GetInstance().Streams[this.ID].PublicKey = publicKeyParam;
					return;
				}

				if (e.Data.StartsWith("HASH:"))
				{
					Console.WriteLine("Received hash");

					// probably public key
					string signature = e.Data.Replace("HASH:", "");

					var publicKey = StreamQueueManager.GetInstance().Streams[this.ID].PublicKey;
					var encryptEngine = new Pkcs1Encoding(new RsaEngine());
					encryptEngine.Init(false, publicKey);

					byte[] signatureBytes = Encoding.UTF8.GetBytes(signature);

					byte[] decryptedHash = encryptEngine.ProcessBlock(signatureBytes, 0, signatureBytes.Length);

					StreamQueueManager.GetInstance().Streams[this.ID].LatestHash = decryptedHash;
					return;
				}

			}

			// Here all the received data should be read
			if (e.IsBinary)
			{
				// Received probably camera bytes
				byte[] receivedBytes = e.RawData;
				byte[] computedHash = _sha1.ComputeHash(receivedBytes);
				byte[] latestHash = StreamQueueManager.GetInstance().Streams[this.ID].LatestHash;


				if (computedHash == latestHash)
				{
					Console.WriteLine("Valid file");
					// Valid file
					StreamQueueManager.GetInstance().AddToQueue(this.ID, receivedBytes);
				}
				else
				{
					Console.WriteLine("Invalid file received!");
				}
			}
			else
			{
				Console.WriteLine("Ignoring received data");
			}
		}

		protected override void OnClose(CloseEventArgs e)
		{
			StreamQueueManager.GetInstance().Streams.Remove(this.ID);
		}
	}
}
