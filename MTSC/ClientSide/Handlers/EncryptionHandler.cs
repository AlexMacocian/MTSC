using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Xml;

namespace MTSC.Client.Handlers
{
    public sealed class EncryptionHandler : IHandler
    {
        private enum ConnectionState
        {
            Initial,
            RequestingPublicKey,
            NegotiatingSymKey,
            Encrypted
        }
        #region Fields
        private ConnectionState connectionState = ConnectionState.Initial;
        private byte[] aesKey;
        #endregion
        #region Constructors
        /// <summary>
        /// Creates an instance of EncryptionHandler.
        /// </summary>
        /// <param name="client">Client object that this handler manages.</param>
        public EncryptionHandler()
        {
            
        }

        #endregion
        #region Private Methods

        private byte[] GetUniqueByteKey(int size)
        {
            var data = new byte[size];
            using (var crypto = RandomNumberGenerator.Create())
            {
                crypto.GetBytes(data);
            }

            return data;
        }

        private byte[] EncryptBytes(byte[] bytesToBeEncrypted)
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
                var key = new Rfc2898DeriveBytes(this.aesKey, saltBytes, 1000);
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

        private byte[] DecryptBytes(byte[] bytesToBeDecrypted)
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

                var key = new Rfc2898DeriveBytes(this.aesKey, saltBytes, 1000);
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
        #region Interface Implementation
        /// <summary>
        /// Tries to initialize an encrypted connection.
        /// </summary>
        /// <param name="client">TcpClient connection.</param>
        /// <returns>True if connection is successful. False otherwise.</returns>
        bool IHandler.InitializeConnection(Client client)
        {
            this.connectionState = ConnectionState.Initial;
            return true;
        }
        /// <summary>
        /// Handles the operations done after client disconnected.
        /// </summary>
        /// <param name="client"></param>
        void IHandler.Disconnected(Client client)
        {

        }
        /// <summary>
        /// Performs operations on the message before sending it.
        /// </summary>
        /// <param name="client">Client connection.</param>
        /// <param name="message">Message to be processed.</param>
        /// <returns>True if no other handler should process the message further.</returns>
        bool IHandler.HandleSendMessage(Client client, ref Message message)
        {
            if (this.connectionState == ConnectionState.Encrypted)
            {
                var decryptedBytes = message.MessageBytes;
                var encryptedBytes = this.EncryptBytes(decryptedBytes);
                message = CommunicationPrimitives.BuildMessage(encryptedBytes);
                return true;
            }

            return false;
        }
        /// <summary>
        /// Performs operations on the message buffer, modifying it.
        /// </summary>
        /// <param name="client">Client connection.</param>
        /// <param name="message">Message to be processed.</param>
        /// <returns>True if no other handler should process it further.</returns>
        bool IHandler.PreHandleReceivedMessage(Client client, ref Message message)
        {
            if (this.connectionState == ConnectionState.Encrypted)
            {
                var encryptedBytes = message.MessageBytes;
                var decryptedBytes = this.DecryptBytes(encryptedBytes);
                message = CommunicationPrimitives.BuildMessage(decryptedBytes);
                return false;
            }

            return false;
        }
        /// <summary>
        /// Handle the received message.
        /// </summary>
        /// <param name="client">Client connection.</param>
        /// <param name="message">Message.</param>
        /// <returns>True if no other handler should handle the message further.</returns>
        bool IHandler.HandleReceivedMessage(Client client, Message message)
        {
            if (this.connectionState == ConnectionState.RequestingPublicKey)
            {
                var ascii = ASCIIEncoding.ASCII.GetString(message.MessageBytes);
                if (ascii.Contains(CommunicationPrimitives.SendPublicKey))
                {
                    var publicKeyBytes = new byte[message.MessageLength - CommunicationPrimitives.SendPublicKey.Length - 1];
                    Array.Copy(message.MessageBytes, CommunicationPrimitives.SendPublicKey.Length + 1, publicKeyBytes, 0, publicKeyBytes.Length);
                    var publicKey = ASCIIEncoding.ASCII.GetString(publicKeyBytes);
                    var rsa = new RSACryptoServiceProvider(1024);
                    var parameters = new RSAParameters();
                    var xmlDoc = new XmlDocument();
                    xmlDoc.LoadXml(publicKey);
                    if (xmlDoc.DocumentElement.Name.Equals("RSAKeyValue"))
                    {
                        foreach (XmlNode node in xmlDoc.DocumentElement.ChildNodes)
                        {
                            switch (node.Name)
                            {
                                case "Modulus": parameters.Modulus = string.IsNullOrEmpty(node.InnerText) ? null : Convert.FromBase64String(node.InnerText); break;
                                case "Exponent": parameters.Exponent = string.IsNullOrEmpty(node.InnerText) ? null : Convert.FromBase64String(node.InnerText); break;
                                case "P": parameters.P = string.IsNullOrEmpty(node.InnerText) ? null : Convert.FromBase64String(node.InnerText); break;
                                case "Q": parameters.Q = string.IsNullOrEmpty(node.InnerText) ? null : Convert.FromBase64String(node.InnerText); break;
                                case "DP": parameters.DP = string.IsNullOrEmpty(node.InnerText) ? null : Convert.FromBase64String(node.InnerText); break;
                                case "DQ": parameters.DQ = string.IsNullOrEmpty(node.InnerText) ? null : Convert.FromBase64String(node.InnerText); break;
                                case "InverseQ": parameters.InverseQ = string.IsNullOrEmpty(node.InnerText) ? null : Convert.FromBase64String(node.InnerText); break;
                                case "D": parameters.D = string.IsNullOrEmpty(node.InnerText) ? null : Convert.FromBase64String(node.InnerText); break;
                            }
                        }
                    }

                    rsa.ImportParameters(parameters);
                    var symkeyBytes = this.GetUniqueByteKey(32);
                    var encryptedSymKey = rsa.Encrypt(symkeyBytes, false);
                    this.aesKey = symkeyBytes;
                    var messageHeader = ASCIIEncoding.ASCII.GetBytes(CommunicationPrimitives.SendEncryptionKey + ":");
                    var messageBytes = new byte[encryptedSymKey.Length + messageHeader.Length];
                    Array.Copy(messageHeader, 0, messageBytes, 0, messageHeader.Length);
                    Array.Copy(encryptedSymKey, 0, messageBytes, messageHeader.Length, encryptedSymKey.Length);
                    client.QueueMessage(messageBytes);
                    this.connectionState = ConnectionState.NegotiatingSymKey;
                    return true;
                }
            }
            else if (this.connectionState == ConnectionState.NegotiatingSymKey)
            {
                var ascii = ASCIIEncoding.ASCII.GetString(this.DecryptBytes(message.MessageBytes));
                if (ascii == CommunicationPrimitives.AcceptEncryptionKey)
                {
                    this.connectionState = ConnectionState.Encrypted;
                    return true;
                }
                else
                {
                    this.connectionState = ConnectionState.Initial;
                    return false;
                }
            }

            return false;
        }
        /// <summary>
        /// Called on every tick by the client object.
        /// Performs regular operations.
        /// </summary>
        /// <param name="tcpClient">Client connection.</param>
        void IHandler.Tick(Client client)
        {
            if (this.connectionState == ConnectionState.Initial)
            {

                client.QueueMessage(ASCIIEncoding.ASCII.GetBytes(CommunicationPrimitives.RequestPublicKey));
                this.connectionState = ConnectionState.RequestingPublicKey;
            }
        }
        #endregion
    }
}
