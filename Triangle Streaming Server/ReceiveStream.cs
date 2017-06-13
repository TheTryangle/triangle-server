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

        private string _pubKey;

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
                //Read privatekey.pem from executable directory
                using (var reader = File.OpenText(AppDomain.CurrentDomain.BaseDirectory + @"\privatekey.pem"))
                {
                    _keyPair = (AsymmetricCipherKeyPair)new PemReader(reader).ReadObject();
                }
            }

            //Get public key string
            if(_pubKey == null)
            {
                TextWriter textWriter = new StringWriter();
                PemWriter pemWriter = new PemWriter(textWriter);
                pemWriter.WriteObject(_keyPair.Public);
                pemWriter.Writer.Flush();
                _pubKey = textWriter.ToString();
                textWriter.Close();
            }

            //Send public key to client
            Send(_pubKey);
        }
    }
}
