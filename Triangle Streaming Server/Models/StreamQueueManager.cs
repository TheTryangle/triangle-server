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
using TriangleStreamingServer.Extensions;
using TriangleStreamingServer.WebSockets;

namespace TriangleStreamingServer.Models
{
	public class StreamQueueManager
	{
		//Default Windows exit code for a file not being found.
		private const int FILE_NOT_FOUND = 2;

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

			try
			{
				using (var reader = File.OpenText(path))
				{
					_keyPair = (AsymmetricCipherKeyPair)new PemReader(reader).ReadObject();
				}
			}
			catch (FileNotFoundException e)
			{
				Console.WriteLine("Private key file not found at {0}! Please check application config and private key file.", path);
				Environment.Exit(FILE_NOT_FOUND);
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

			stream.LatestReceivedTime = DateTime.Now;
			stream.VideoQueue.Enqueue(item);
		}

		public void RunCheckQueue()
		{
			Observable.Interval(TimeSpan.FromMilliseconds(250))
				.Select(i => Observable.FromAsync(CheckStreamQueue))
				.Concat()
				.Subscribe();

			Observable.Interval(TimeSpan.FromSeconds(30))
				.Subscribe(i => CheckStoppedStreamers());
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

						//Sign video fragment.
						string base64 = Convert.ToBase64String(nextVideo);
						string encryptedHash = base64.Sign(_keyPair.Private);

						await ReceivingWebSocket.Send($"SIGN: {encryptedHash}", receivingClients.ToArray());
					}
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex.ToString());
			}
		}

		private void CheckStoppedStreamers()
		{
			DateTime now = DateTime.Now;

			foreach (Stream stream in Streams.Values.ToList())
			{
				TimeSpan difference = now - stream.LatestReceivedTime;
				if(difference.TotalSeconds > 30.0)
				{
					Streams.TryRemove(stream.ClientID, out Stream value);
				}
			}
		}
	}
}