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
        private class AdditionalData
        {
            public byte[] Key;
            public ClientState ClientState = ClientState.Initial;
        }
        #region Fields
        private Dictionary<ClientStruct, AdditionalData> additionalData;
        private RSACryptoServiceProvider rsa;
        private string privateKey, publicKey;
        #endregion
        #region Properties
        #endregion
        #region Constructors
        /// <summary>
        /// Creates an instance of EncryptionHandler.
        /// </summary>
        /// <param name="rsa">Symmetrical algorithm to be used for end-to-end encryption.</param>
        public EncryptionHandler(RSACryptoServiceProvider rsa)
        {
            additionalData = new Dictionary<ClientStruct, AdditionalData>();
            privateKey = HelperFunctions.ToXmlString(rsa, true);
            publicKey = HelperFunctions.ToXmlString(rsa, false);
        }
        #endregion
        #region Public Methods
        /// <summary>
        /// Called when a client is being removed.
        /// </summary>
        /// <param name="client">Client to be removed.</param>
        public void ClientRemoved(ClientStruct client)
        {
            additionalData.Remove(client);
        }
        /// <summary>
        /// Handle a new client connection.
        /// </summary>
        /// <param name="client">New client connection.</param>
        /// <returns>False if an error occurred.</returns>
        public bool HandleClient(ClientStruct client)
        {
            additionalData.Add(client, new AdditionalData());
            return false;
        }
        /// <summary>
        /// Handle a received message.
        /// </summary>
        /// <param name="client">Client connection.</param>
        /// <param name="message">Received message.</param>
        /// <returns>True if no other handler should handle current message.</returns>
        public bool HandleMessage(ClientStruct client, Message message)
        {
            if (additionalData[client].ClientState == ClientState.Initial || additionalData[client].ClientState == ClientState.Negotiating)
            {
                string asciiMessage = ASCIIEncoding.ASCII.GetString(message.MessageBytes);
                if (asciiMessage == CommunicationPrimitives.RequestPublicKey)
                {
                    Message sendMessage = CommunicationPrimitives.BuildMessage(ASCIIEncoding.ASCII.GetBytes(CommunicationPrimitives.SendPublicKey + ":" + publicKey));
                    CommunicationPrimitives.SendMessage(client.TcpClient, sendMessage);
                    additionalData[client].ClientState = ClientState.Negotiating;
                    return true;
                }
                else if (asciiMessage.Contains(CommunicationPrimitives.SendEncryptionKey))
                {
                    byte[] encryptedKey = new byte[message.MessageLength - CommunicationPrimitives.SendEncryptionKey.Length - 1];
                    Array.Copy(message.MessageBytes, CommunicationPrimitives.SendEncryptionKey.Length + 1, encryptedKey, 0, encryptedKey.Length);
                    byte[] decryptedKey = rsa.Decrypt(encryptedKey, false);
                    additionalData[client].Key = decryptedKey;
                    Message sendMessage = CommunicationPrimitives.BuildMessage(ASCIIEncoding.ASCII.GetBytes(CommunicationPrimitives.AcceptEncryptionKey));
                    CommunicationPrimitives.SendMessage(client.TcpClient, sendMessage);
                    additionalData[client].ClientState = ClientState.Encrypted;
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }
        /// <summary>
        /// Perform transformative operations on the message.
        /// </summary>
        /// <param name="client">Client connection.</param>
        /// <param name="message">Message to be processed.</param>
        /// <returns>True if no other handlers should perform operations on current message.</returns>
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
        /// <summary>
        /// Performs periodic operations on the server.
        /// </summary>
        public void Tick()
        {
            
        }
        #endregion
        #region Private Methods
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
        #endregion
    }
}
