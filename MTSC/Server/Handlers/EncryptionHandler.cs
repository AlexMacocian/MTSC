using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using MTSC;

namespace MTSC.Server.Handlers
{
    /// <summary>
    /// Handler that encrypts the communication.
    /// </summary>
    public class EncryptionHandler : IHandler
    {
        private enum ClientState
        {
            Initial,
            Negotiating,
            Encrypted
        }
        private struct AdditionalData
        {
            public byte[] Key;
            public ClientState ClientState;
        }
        private Dictionary<ClientStruct, AdditionalData> additionalData;
        private RSA rsa;
        private string privateKey, publicKey;
        /// <summary>
        /// Creates an instance of EncryptionHandler.
        /// </summary>
        /// <param name="rsa">Symmetrical algorithm to be used for end-to-end encryption.</param>
        public EncryptionHandler(RSA rsa)
        {
            additionalData = new Dictionary<ClientStruct, AdditionalData>();
            privateKey = rsa.ToXmlString(true);
            publicKey = rsa.ToXmlString(false);
        }
        
        public void ClientRemoved(ClientStruct client)
        {
            throw new NotImplementedException();
        }

        public bool HandleClient(ClientStruct client)
        {
            throw new NotImplementedException();
        }

        public bool HandleMessage(ClientStruct client, Message message)
        {
            throw new NotImplementedException();
        }

        public bool PreHandleMessage(ClientStruct client, ref Message message)
        {
            if(additionalData[client].ClientState == ClientState.Encrypted)
            {
                /*
                 * Decrypt message before returning.
                 */
                byte[] encryptedBytes = message.MessageBytes;
                byte[] decryptedBytes = DecryptBytes(additionalData[client].Key, encryptedBytes);
                message = new Message((uint)decryptedBytes.Length, decryptedBytes);
                return false;
            }
            else
            {
                /*
                 * If the state of the client is not encrypted, there's nothing to decrypt.
                 */
                return false;
            }
        }


        private byte[] EncryptBytes(byte[] clientKey, byte[] bytesToBeEncrypted)
        {
            byte[] encryptedBytes = null;

            // Set your salt here, change it to meet your flavor:
            // The salt bytes must be at least 8 bytes.
            byte[] saltBytes = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 };

            using (MemoryStream ms = new MemoryStream())
            {
                using (RijndaelManaged AES = new RijndaelManaged())
                {
                    AES.KeySize = 256;
                    AES.BlockSize = 128;

                    AES.Mode = CipherMode.CBC;

                    var key = new Rfc2898DeriveBytes(clientKey, saltBytes, 1000);
                    AES.Key = key.GetBytes(AES.KeySize / 8);
                    AES.IV = key.GetBytes(AES.BlockSize / 8);



                    using (var cs = new CryptoStream(ms, AES.CreateEncryptor(), CryptoStreamMode.Write))
                    {
                        cs.Write(bytesToBeEncrypted, 0, bytesToBeEncrypted.Length);
                        cs.Close();
                    }
                    encryptedBytes = ms.ToArray();
                }
            }

            return encryptedBytes;
        }

        private byte[] DecryptBytes(byte[] clientKey, byte[] bytesToBeDecrypted)
        {
            byte[] decryptedBytes = null;

            // Set your salt here, change it to meet your flavor:
            // The salt bytes must be at least 8 bytes.
            byte[] saltBytes = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 };

            using (MemoryStream ms = new MemoryStream())
            {
                using (RijndaelManaged AES = new RijndaelManaged())
                {
                    AES.KeySize = 256;
                    AES.BlockSize = 128;

                    AES.Mode = CipherMode.CBC;

                    var key = new Rfc2898DeriveBytes(clientKey, saltBytes, 1000);
                    AES.Key = key.GetBytes(AES.KeySize / 8);
                    AES.IV = key.GetBytes(AES.BlockSize / 8);



                    using (var cs = new CryptoStream(ms, AES.CreateDecryptor(), CryptoStreamMode.Write))
                    {
                        cs.Write(bytesToBeDecrypted, 0, bytesToBeDecrypted.Length);
                        cs.Close();
                    }
                    decryptedBytes = ms.ToArray();
                }
            }

            return decryptedBytes;
        }
    }
}
