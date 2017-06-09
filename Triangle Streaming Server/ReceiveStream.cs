using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.OpenSsl;
using System;
using System.IO;
using WebSocketSharp;
using WebSocketSharp.Server;

namespace Triangle_Streaming_Server
{
	public class ReceiveStream : WebSocketBehavior
	{
        private static AsymmetricCipherKeyPair _keyPair;

        protected override void OnOpen()
		{
			Console.WriteLine($"Someone connected to receive, {this.Context.UserEndPoint.ToString()}");
		}

        protected override void OnMessage(MessageEventArgs e)
        {
            if(!e.Data.Equals("PUBKEY"))
            {
                return;
            }

            if (_keyPair == null)
            {
                using (var reader = File.OpenText(AppDomain.CurrentDomain.BaseDirectory + @"\privatekey.pem"))
                {
                    _keyPair = (AsymmetricCipherKeyPair)new PemReader(reader).ReadObject();
                }
            }

            Send("PUBKEY: " + _keyPair.Public.ToString());
        }
    }
}
