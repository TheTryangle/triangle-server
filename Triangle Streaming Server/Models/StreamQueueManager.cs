using Microsoft.Extensions.Configuration;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Encodings;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.OpenSsl;
using Org.BouncyCastle.Security;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TriangleStreamingServer.WebSockets;

namespace TriangleStreamingServer.Models
{
	public class StreamQueueManager
	{
		private static StreamQueueManager _instance;
		private AsymmetricCipherKeyPair _keyPair;
		private SHA1 _sha1;

		public ConcurrentDictionary<string, Stream> Streams { get; private set; }
		public ReceiveStream ReceivingWebSocket { get; set; }

		public StreamQueueManager()
		{
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

			_sha1 = SHA1.Create();

			Streams = new ConcurrentDictionary<string, Stream>();
			RunCheckQueue();
		}

		public void AddToQueue(string ID, byte[] item)
		{
			var stream = Streams[ID];
			if (stream == null)
			{
				stream = new Stream(ID);
				Streams.TryAdd(ID, stream);
			}

			stream.VideoQueue.Enqueue(item);
		}

		public void RunCheckQueue()
		{
			Observable.Interval(TimeSpan.FromMilliseconds(250))
				.Select(i => Observable.FromAsync(CheckStreamQueue))
				.Concat()
				.Subscribe();
		}

		private async Task CheckStreamQueue()
		{
			try
			{
				foreach (Stream stream in Streams.Values.ToList()) //The ToList() call is to prevent modification from throwing exceptions.
				{
					if (stream.VideoQueue.Count > 0)
					{
						byte[] nextVideo = stream.VideoQueue.Dequeue();

						//Get the receiving clients
						//Quite slow. Needs improvements.
						var receivingClients = ReceivingWebSocket.Clients
							.Where(pair => pair.Value == stream.ClientID)
							.Select(pair => pair.Key);

						await ReceivingWebSocket.Send(nextVideo, receivingClients.ToArray());

						//Every 5 video fragments, sign a fragment.
						if (stream.FragmentCount >= 5)
						{
							string encryptedHash = SignBytes(Convert.ToBase64String(nextVideo));

							await ReceivingWebSocket.Send($"SIGN: {encryptedHash}", receivingClients.ToArray());
							stream.FragmentCount = 0;
						}
						stream.FragmentCount += 1;
					}
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex.ToString());
			}
		}

		private string SignBytes(byte[] bytesToSign)
		{
			ISigner signer = SignerUtilities.GetSigner("SHA1withRSA");
			signer.Init(true, _keyPair.Private);
			signer.BlockUpdate(bytesToSign, 0, bytesToSign.Length);
			byte[] signBytes = signer.GenerateSignature();

			return ByteArrayToString(signBytes);
		}

		private string SignBytes(string stringToSign)
		{
			return SignBytes(Encoding.ASCII.GetBytes(stringToSign));
		}

		private string EncryptHashBytes(byte[] bytesToHash)
		{
			var hash = _sha1.ComputeHash(bytesToHash);

			var encryptEngine = new Pkcs1Encoding(new RsaEngine());

			encryptEngine.Init(true, _keyPair.Private);

			return Convert.ToBase64String(encryptEngine.ProcessBlock(hash, 0, hash.Length));
		}

		private string ByteArrayToString(byte[] ba)
		{
			StringBuilder hex = new StringBuilder(ba.Length * 2);
			foreach (byte b in ba)
				hex.AppendFormat("{0:x2}", b);
			return hex.ToString();
		}
	}
}