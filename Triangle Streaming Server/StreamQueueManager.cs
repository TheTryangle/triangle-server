using System;
using System.Collections.Generic;
using System.Timers;
using Org.BouncyCastle.Crypto;
using System.IO;
using Org.BouncyCastle.OpenSsl;
using System.Linq;
using System.Configuration;
using Triangle_Streaming_Server.Extensions;

namespace Triangle_Streaming_Server
{
	public class StreamQueueManager
	{
		private static StreamQueueManager _instance;
		private static AsymmetricCipherKeyPair _keyPair;

		public static StreamQueueManager GetInstance()
		{
			if (_instance == null)
				_instance = new StreamQueueManager();

			return _instance;
		}

		public Dictionary<string, Stream> Streams { get; private set; }

		private StreamQueueManager()
		{
			var path = ConfigurationManager.AppSettings["privateKeyPath"];
			using (var reader = File.OpenText(path))
			{
				_keyPair = (AsymmetricCipherKeyPair)new PemReader(reader).ReadObject();
			}

			Streams = new Dictionary<string, Stream>();
			RunCheckQueue();
		}

		public void AddToQueue(string ID, byte[] item)
		{
			var stream = Streams[ID];
			if (stream == null)
			{
				stream = new Stream(ID);
				Streams.Add(ID, stream);
			}

			stream.VideoQueue.Enqueue(item);
		}

		public void RunCheckQueue()
		{
			Timer t = new Timer(250);
			t.Elapsed += T_Elapsed;
			t.Start();
		}

		private void T_Elapsed(object sender, ElapsedEventArgs e)
		{
			foreach (Stream stream in Streams.Values.ToList()) //The ToList() call is to prevent modification from throwing exceptions.
			{
				if (stream.VideoQueue.Count > 0)
				{
					byte[] nextVideo = stream.VideoQueue.Dequeue();

					//Get the receiving clients
					//Quite slow. Needs improvements.
					var receivingClients = ReceiveStream.Clients
						.Where(pair => pair.Value == stream.ClientID)
						.Select(pair => pair.Key);

					foreach (string client in receivingClients)
					{
						WebSocketManager.GetInstance().Server.WebSocketServices["/receive"].Sessions.SendTo(nextVideo, client);
					}

					//Every 5 video fragments, sign a fragment.
					if (stream.FragmentCount >= 5)
					{
						string base64 = Convert.ToBase64String(nextVideo);
						string encryptedHash = base64.Sign(_keyPair.Private);
						foreach (string client in receivingClients)
						{
							WebSocketManager.GetInstance().Server.WebSocketServices["/receive"].Sessions.SendTo("SIGN: " + encryptedHash, client);
						}

						stream.FragmentCount = 0;
					}
					stream.FragmentCount += 1;
				}
			}
		}
	}
}
