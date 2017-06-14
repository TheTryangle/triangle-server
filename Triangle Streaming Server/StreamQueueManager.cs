using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using Org.BouncyCastle;
using Org.BouncyCastle.Crypto;
using System.IO;
using Org.BouncyCastle.OpenSsl;
using Org.BouncyCastle.Crypto.Encodings;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Math;
using System.Security.Cryptography;
using Org.BouncyCastle.Security;
using System.Linq;
using System.Configuration;

namespace Triangle_Streaming_Server
{
	public class StreamQueueManager
	{
		private static StreamQueueManager _instance;
		private static AsymmetricCipherKeyPair _keyPair;
		private static SHA1CryptoServiceProvider _sha1;

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

			_sha1 = new SHA1CryptoServiceProvider();

			Streams = new Dictionary<string, Stream>();
			RunCheckQueue();
		}

		public void AddToQueue(string ID, byte[] item)
		{
			var stream = Streams[ID];
			if(stream == null)
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
			foreach(Stream stream in Streams.Values.ToList()) //The ToList() call is to prevent modification from throwing exceptions.
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
						string encryptedHash = SignBytes(Convert.ToBase64String(nextVideo));
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
