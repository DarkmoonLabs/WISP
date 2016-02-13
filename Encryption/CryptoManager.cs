using System;
using System.Security.Cryptography;
using System.Collections.Generic;
using System.IO;
using System.Diagnostics;
using System.Text;

namespace Shared
{
	/// <summary>
	/// Cryptography helper class which generates 1024 bit Rijndael keys, handles SHA512 hashes and creates and formats RSA keys and exchanges.
    /// This object is implemented as a singleton.  Along with other data, each CryptoManager retains a public/private RSA key pair which is used for all 
    /// key exchanges by this object.
	/// </summary>
	public class CryptoManager
	{
        private static int                      m_KeySize = 1024;
        private static SHA256Managed            m_SHA;
        private RSAPKCS1KeyExchangeDeformatter  m_RSA_Exch_Def;
        private RSAPKCS1KeyExchangeFormatter    m_RSA_Exch_For;
        private RSACryptoServiceProvider        m_RSA;
        private RijndaelManaged                 m_RIJ;
        private RSAParameters                   m_KeyPair; // Silverlight doesn't use UDP, which is the only thing this is used for - UDP packet signing.
        private byte[]                          m_PublicKey;
        private byte[]                          m_PrivateKey;
        
		private void Construct()
		{            
		}

        static CryptoManager()
        {

            m_SHA = new SHA256Managed();
        }

		public CryptoManager()
		{            
            m_RSA_Exch_Def           = new RSAPKCS1KeyExchangeDeformatter();
            m_RSA_Exch_For           = new RSAPKCS1KeyExchangeFormatter();
            m_RSA                    = new RSACryptoServiceProvider();
            m_RSA.KeySize            = m_KeySize;
            //m_RSA.PersistKeyInCsp    = true;	
            
            m_SHA                    = new SHA256Managed();
           
            m_RIJ                    = new RijndaelManaged();
            m_RIJ.Padding            = PaddingMode.PKCS7;
            m_RIJ.Mode               = CipherMode.CBC;
			m_RIJ.IV                 = new byte[16];			
            m_RIJ.BlockSize          = 128;
            m_RIJ.KeySize            = 256;
            m_RIJ.GenerateKey();

            //m_PublicKey              = m_RSA.ExportCspBlob(false);
            //m_PrivateKey             = m_RSA.ExportCspBlob(true);
            m_PublicKey              = Encoding.UTF8.GetBytes(m_RSA.ToXmlString(false));
            m_PrivateKey             = Encoding.UTF8.GetBytes(m_RSA.ToXmlString(true));
            m_KeyPair                = m_RSA.ExportParameters(true);

		}        	 		

        /// <summary>
        /// Generates a random 1024 bit Rijndael key with PKCS7 padding mode and a 16 byte initiatlization vector
        /// </summary>
        /// <returns></returns>
        public byte[] GetRandomRijndaelKey()
        {
            RijndaelManaged RIJ = new RijndaelManaged();
            RIJ.Padding = PaddingMode.PKCS7;
            RIJ.IV = new byte[16];            
            RIJ.GenerateKey();            
            return RIJ.Key;
        }

		/// <summary>
		/// Encrypts the target @rijKey with the given RSA public key.  Generally, the public RSA key is
        /// transmitted via plaintext and then used to encrypt the Rijndael key.  The encrypted Rijndael key can then
        /// only be decrypted by the holder of the private RSA key that was generated along with the public key.
        /// This is the core of the encryption key exchange at the beginning of all Kronus network communication.
		/// </summary>
		/// <param name="publicKey">the public RSA key used to encrpt @rijKey</param>
		/// <param name="rijKey">the Rijndael key to encrypt</param>
		/// <returns></returns>
		public byte[] EncryptRijndaelKey(byte[] publicKey, byte[] rijKey)
		{
			byte[] key = new byte[0];
			
			lock(this)
			{
				//m_RSA.ImportCspBlob(publicKey);
                m_RSA = new RSACryptoServiceProvider();
                m_RSA.KeySize = m_KeySize;
                m_RSA.FromXmlString(Encoding.UTF8.GetString(publicKey));
				m_RSA_Exch_For.SetKey(m_RSA);
                key = m_RSA_Exch_For.CreateKeyExchange(rijKey);
            }

			return key;
		}

        /// <summary>
        /// Decrypts the target key with the CryptoManager's private RSA key.  It is assumbed that the
        /// @rijKey was previously encrypted with the CryptoManager's public key.
        /// </summary>
        /// <param name="rijKey"></param>
        /// <returns></returns>
		public byte[] DecryptRijndaelKey(byte[] rijKey)
		{
			byte[] key = new byte[0];
			lock(this)
			{
                //m_RSA.ImportCspBlob(m_PrivateKey);
                m_RSA.FromXmlString(Encoding.UTF8.GetString(m_PrivateKey));
                m_RSA_Exch_Def.SetKey(m_RSA);
				key = m_RSA_Exch_Def.DecryptKeyExchange(rijKey);
			}

			return key;
		}


        /// <summary>
        /// Hashes data using the local private/public key pair.  Data must be verified with the matching public key.
        /// </summary>
        public byte[] HashAndSignBytesSHA256(byte[] DataToSign, int Index, int Length)
        {
            try
            {
                // Hash and sign the data. Pass a new instance of SHA1CryptoServiceProvider
                // to specify the use of SHA1 for hashing.                
                m_RSA.ImportParameters(m_KeyPair);
                return m_RSA.SignData(DataToSign, Index, Length, m_SHA);
            }
            catch (CryptographicException e)
            {
                return new byte[0];
            }
        }

        /// <summary>
        /// Verifies the signature that was previously signed using the @publicKey's matching public key
        /// </summary>
        public bool VerifySignedHashSHA256(string publicKey, byte[] DataToVerify, byte[] SignedData)
        {
            try
            {
                // Verify the data using the signature.  Pass a new instance of SHA1CryptoServiceProvider
                // to specify the use of SHA1 for hashing.
                m_RSA.FromXmlString(publicKey);
                return m_RSA.VerifyData(DataToVerify, m_SHA, SignedData);

            }
            catch (CryptographicException e)
            {
                Console.WriteLine(e.Message);

                return false;
            }
        }

        /// <summary>
        /// The public RSA key associated with this CryptoManager
        /// </summary>
		public byte[] PublicRSAKey
		{
			get
			{
                return m_PublicKey;
			}
		}

        /// <summary>
        /// The private RSA key associated with this CryptoManager
        /// </summary>
		public byte[] PrivateRSAKey
		{
			get
			{
                return m_PrivateKey;
			}
		}
 
        /// <summary>
        /// Encrypts an arbitrary sequence of @source bytes with an arbitrary Rijndael @key
        /// </summary>
        /// <param name="source">the data to encrypt</param>
        /// <param name="key">the Rijndael key to use for encryption</param>
        /// <returns>the encrypted byte sequence, or byte[0] on failure</returns>
		public byte[] RijEncrypt(byte[] source, int sourceStart, int sourceLen, byte[] key)
		{
            try
            {
                lock(this)
                {
                    m_RIJ.Key = key;
                    // create an Encryptor from the Provider Service instance
                    ICryptoTransform encrypto = m_RIJ.CreateEncryptor();
                    return encrypto.TransformFinalBlock(source, sourceStart, sourceLen);
                }
            }
            catch(Exception RijEnExcB)
            {
            }

			// convert into Base64 so that the result can be used in xml
			return new byte[0];

		}

        /// <summary>
        /// Decrypts an arbitrary sequence of @source bytes with an arbitrary Rijndael @key
        /// </summary>
        /// <param name="source">the data to encrypt</param>
        /// <param name="key">the Rijndael key to use for decryption</param>
        /// <returns>the decrypted byte sequence, or byte[0] on failure</returns>
		public byte[] RijDecrypt(byte[] source, byte[] key)
		{
            return RijDecrypt(source, 0, source.Length, key);
		}

        /// <summary>
        /// Decrypts an arbitrary sequence of @source bytes with an arbitrary Rijndael @key
        /// </summary>
        /// <param name="source">the data to encrypt</param>
        /// <param name="key">the Rijndael key to use for decryption</param>
        /// <returns>the decrypted byte sequence, or byte[0] on failure</returns>
        public byte[] RijDecrypt(byte[] source, int dataStart, int dataLen, byte[] key)
        {
            try
            {
                lock (this)
                {
                    m_RIJ.Key = key;
                    // create a Decryptor from the Provider Service instance
                    ICryptoTransform encrypto = m_RIJ.CreateDecryptor();
                    return encrypto.TransformFinalBlock(source, dataStart, dataLen);
                }
            }
            catch (Exception RijDecExc)
            {
            }

            return new byte[0];
        }

        /// <summary>
        /// Generates a SHA256 hash, given a string of text.  No two strings should generate the same hash.
        /// </summary>
        /// <param name="inputString">the string upon which to generate the hash</param>
        /// <returns>the Base64 string that resulted from the hash - could be used in XML</returns>
		public static string GetSHA256Hash(string inputString)
		{
            string rslt = "";
			try
			{
				byte[] input = System.Text.UTF8Encoding.UTF8.GetBytes(inputString);
                byte[] output = GetSHA256Hash(input);
                
                // convert into Base64 so that the result can be used in xml
                rslt = System.Convert.ToBase64String(output, 0, output.Length);
			}
			catch(Exception MD5Exc)
			{				
				return string.Empty;
			}
			
			return rslt;
		}

        /// <summary>
        /// Generates a SHA256 hash, given a sequence of bytes.  No two byte sequences should generate the same hash.
        /// </summary>
        /// <param name="input">the byte sequence upon which to generate the hash</param>
        /// <returns>the byte sequence that resulted from the hash</returns>
        public static byte[] GetSHA256Hash(byte[] input)
        {
            byte[] output = null;
            try
            {
                output = m_SHA.ComputeHash(input, 0, input.Length);
            }
            catch (Exception MD5Exc)
            {
                return new byte[0];
            }

            return output;
        }


        public void Init()
        {
            //throw new NotImplementedException();
        }
    }
}