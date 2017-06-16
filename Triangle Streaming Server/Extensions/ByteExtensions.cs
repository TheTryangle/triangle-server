using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Encodings;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Security;
using System;
using System.Security.Cryptography;
using System.Text;

namespace Triangle_Streaming_Server.Extensions
{
	public static class IntegrityExtensions
	{
		public const string ALGORITHM = "SHA1withRSA";

		/// <summary>
		/// Signes the data with the <paramref name="privateKey"/>
		/// </summary>
		/// <param name="stringToSign">The string to sign</param>
		/// <param name="privateKey">The private key to use for signing</param>
		/// <returns></returns>
		public static string Sign(this string stringToSign, AsymmetricKeyParameter privateKey)
		{
			return Sign(Encoding.ASCII.GetBytes(stringToSign), privateKey);
		}

		/// <summary>
		/// Signes the data with the <paramref name="privateKey"/>
		/// </summary>
		/// <param name="bytesToSign">The data to sign</param>
		/// <param name="privateKey">The private key to use for signing</param>
		/// <returns></returns>
		public static string Sign(this byte[] bytesToSign, AsymmetricKeyParameter privateKey)
		{
			ISigner signer = SignerUtilities.GetSigner(ALGORITHM);
			signer.Init(true, privateKey);
			signer.BlockUpdate(bytesToSign, 0, bytesToSign.Length);
			byte[] signBytes = signer.GenerateSignature();

			return ToHexString(signBytes);
		}

		/// <summary>
		/// Validates the signed data with the <paramref name="signature"/> and the <paramref name="publicKey"/>
		/// </summary>
		/// <param name="bytesToValidate">The data to validate</param>
		/// <param name="signature">The correct signature of the data</param>
		/// <param name="publicKey">The public key use for validating</param>
		/// <returns></returns>
		public static bool Validate(this byte[] bytesToValidate, byte[] signature, AsymmetricKeyParameter publicKey)
		{
			ISigner signer = SignerUtilities.GetSigner(ALGORITHM);
			signer.Init(false, publicKey);
			signer.BlockUpdate(bytesToValidate, 0, bytesToValidate.Length);

			return signer.VerifySignature(signature);
		}

		/// <summary>
		/// Encrypts and generates a hash.
		/// </summary>
		/// <param name="bytesToHash">The data to hash</param>
		/// <param name="privateKey">The private key to use for hashing</param>
		/// <returns>Base64 string with encrypted hash</returns>
		public static string EncryptHash(this byte[] bytesToHash, AsymmetricKeyParameter privateKey)
		{
			SHA1CryptoServiceProvider sha1 = new SHA1CryptoServiceProvider();
			var hash = sha1.ComputeHash(bytesToHash);

			var encryptEngine = new Pkcs1Encoding(new RsaEngine());

			encryptEngine.Init(true, privateKey);

			return Convert.ToBase64String(encryptEngine.ProcessBlock(hash, 0, hash.Length));
		}

		/// <summary>
		/// Converts a byte array to a hex string
		/// </summary>
		/// <param name="ba">The byte array to convert</param>
		/// <returns>Hex string from the byte array.</returns>
		public static string ToHexString(this byte[] ba)
		{
			StringBuilder hex = new StringBuilder(ba.Length * 2);
			foreach (byte b in ba)
				hex.AppendFormat("{0:x2}", b);
			return hex.ToString();
		}
	}
}
