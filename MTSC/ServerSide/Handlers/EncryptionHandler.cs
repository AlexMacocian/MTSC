﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace MTSC.ServerSide.Handlers
{
    /// <summary>
    /// Handler that encrypts the communication.
    /// </summary>
    public sealed class EncryptionHandler : IHandler
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
        /// <param name="server">Server object managed by the handler.</param>
        public EncryptionHandler(RSACryptoServiceProvider rsa)
        {
            this.rsa = rsa;
            this.privateKey = HelperFunctions.ToXmlString(rsa, true);
            this.publicKey = HelperFunctions.ToXmlString(rsa, false);
        }
        #endregion
        #region Public Methods

        #endregion
        #region Private Methods
        private byte[] EncryptBytes(byte[] clientKey, byte[] bytesToBeEncrypted)
        {
            byte[] encryptedBytes = null;

            // Set your salt here, change it to meet your flavor:
            // The salt bytes must be at least 8 bytes.
            var saltBytes = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 };

            using (var ms = new MemoryStream())
            {
                using var AES = Aes.Create();
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

            return encryptedBytes;
        }

        private byte[] DecryptBytes(byte[] clientKey, byte[] bytesToBeDecrypted)
        {
            byte[] decryptedBytes = null;

            // Set your salt here, change it to meet your flavor:
            // The salt bytes must be at least 8 bytes.
            var saltBytes = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 };

            using (var ms = new MemoryStream())
            {
                using var AES = Aes.Create();
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

            return decryptedBytes;
        }
        #endregion
        #region Handler Implementation
        /// <summary>
        /// Called when a client is being removed.
        /// </summary>
        /// <param name="client">Client to be removed.</param>
        void IHandler.ClientRemoved(Server server, ClientData client)
        {

        }
        /// <summary>
        /// Handle a new client connection.
        /// </summary>
        /// <param name="client">New client connection.</param>
        /// <returns>False if an error occurred.</returns>
        bool IHandler.HandleClient(Server server, ClientData client)
        {
            client.Resources.SetResource(new AdditionalData());
            return false;
        }
        /// <summary>
        /// Handle a received message.
        /// </summary>
        /// <param name="client">Client connection.</param>
        /// <param name="message">Received message.</param>
        /// <returns>True if no other handler should handle current message.</returns>
        bool IHandler.HandleReceivedMessage(Server server, ClientData client, Message message)
        {
            var additionalData = client.Resources.GetResource<AdditionalData>();
            if (additionalData.ClientState == ClientState.Initial || additionalData.ClientState == ClientState.Negotiating)
            {
                var asciiMessage = ASCIIEncoding.ASCII.GetString(message.MessageBytes);
                if (asciiMessage == CommunicationPrimitives.RequestPublicKey)
                {
                    server.QueueMessage(client, ASCIIEncoding.ASCII.GetBytes(CommunicationPrimitives.SendPublicKey + ":" + this.publicKey));
                    additionalData.ClientState = ClientState.Negotiating;
                    return true;
                }
                else if (asciiMessage.Contains(CommunicationPrimitives.SendEncryptionKey))
                {
                    var encryptedKey = new byte[message.MessageLength - CommunicationPrimitives.SendEncryptionKey.Length - 1];
                    Array.Copy(message.MessageBytes, CommunicationPrimitives.SendEncryptionKey.Length + 1, encryptedKey, 0, encryptedKey.Length);
                    var decryptedKey = this.rsa.Decrypt(encryptedKey, false);
                    additionalData.Key = decryptedKey;
                    server.QueueMessage(client, ASCIIEncoding.ASCII.GetBytes(CommunicationPrimitives.AcceptEncryptionKey));
                    additionalData.ClientState = ClientState.Encrypted;
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
        bool IHandler.PreHandleReceivedMessage(Server server, ClientData client, ref Message message)
        {
            var additionalData = client.Resources.GetResource<AdditionalData>();
            if (additionalData.ClientState == ClientState.Encrypted)
            {
                /*
                 * Decrypt message before returning.
                 */
                var encryptedBytes = message.MessageBytes;
                var decryptedBytes = this.DecryptBytes(additionalData.Key, encryptedBytes);
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
        void IHandler.Tick(Server server)
        {

        }
        /// <summary>
        /// Encrypts the message before sending.
        /// </summary>
        /// <param name="client">Client object.</param>
        /// <param name="message">Message to be processed.</param>
        /// <returns>True if no other handler should process this message.</returns>
        bool IHandler.HandleSendMessage(Server server, ClientData client, ref Message message)
        {
            var additionalData = client.Resources.GetResource<AdditionalData>();
            if (additionalData.ClientState == ClientState.Encrypted)
            {
                message = CommunicationPrimitives.BuildMessage(this.EncryptBytes(additionalData.Key, message.MessageBytes));
            }

            return false;
        }
        #endregion
    }
}
