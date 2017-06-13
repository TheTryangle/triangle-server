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

namespace Triangle_Streaming_Server
{
	public class StreamQueueManager
	{
		private static StreamQueueManager _instance;
        private static AsymmetricCipherKeyPair _keyPair;
        private static SHA1CryptoServiceProvider _sha1;
        private int _queueCount = 0; //This is incremented by one every time a fragment is broadcast.

		public static StreamQueueManager GetInstance()
		{
			if (_instance == null)
				_instance = new StreamQueueManager();

			return _instance;
		}

		public Queue<byte[]> StreamQueue { get; set; }

		private StreamQueueManager()
		{
            using (var reader = File.OpenText(AppDomain.CurrentDomain.BaseDirectory + @"\privatekey.pem"))
            {
                _keyPair = (AsymmetricCipherKeyPair)new PemReader(reader).ReadObject();
            }

            _sha1 = new SHA1CryptoServiceProvider();

            StreamQueue = new Queue<byte[]>();
			RunCheckQueue();
		}

		public void AddToQueue(byte[] item)
		{
			StreamQueue.Enqueue(item);
		}

		public void RunCheckQueue()
		{
			Timer t = new Timer(250);
			t.Elapsed += T_Elapsed;
			t.Start();
		}

		private void T_Elapsed(object sender, ElapsedEventArgs e)
		{
			if (StreamQueue.Count > 0)
			{
				byte[] nextVideo = StreamQueue.Dequeue();
				WebSocketManager.GetInstance().Server.WebSocketServices["/receive"].Sessions.Broadcast(nextVideo);

                //Every 5 video fragments, sign a fragment.
                if (_queueCount >= 5)
                {
                    string encryptedHash = SignBytes(Convert.ToBase64String(nextVideo));
                    WebSocketManager.GetInstance().Server.WebSocketServices["/receive"].Sessions.Broadcast(String.Format("SIGN: {0}", encryptedHash));
                    _queueCount = 0;
                }

                _queueCount++;
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
