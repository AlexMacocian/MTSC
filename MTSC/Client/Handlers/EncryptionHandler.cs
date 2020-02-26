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
        #region Public Methods

        #endregion
        #region Private Methods
        private string GetUniqueKey(int size)
        {
            return Convert.ToBase64String(GetUniqueByteKey(size));
        }

        private byte[] GetUniqueByteKey(int size)
        {
            byte[] data = new byte[size];
            using (RNGCryptoServiceProvider crypto = new RNGCryptoServiceProvider())
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
            byte[] saltBytes = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 };

            using (MemoryStream ms = new MemoryStream())
            {
                using (RijndaelManaged AES = new RijndaelManaged())
                {
                    AES.KeySize = 256;
                    AES.BlockSize = 128;
                    AES.Mode = CipherMode.CBC;
                    var key = new Rfc2898DeriveBytes(aesKey, saltBytes, 1000);
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

        private byte[] DecryptBytes(byte[] bytesToBeDecrypted)
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

                    var key = new Rfc2898DeriveBytes(aesKey, saltBytes, 1000);
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

        private string EncryptText(string input)
        {
            // Get the bytes of the string
            byte[] bytesToBeEncrypted = Encoding.UTF8.GetBytes(input);

            byte[] bytesEncrypted = EncryptBytes(bytesToBeEncrypted);

            string result = Encoding.UTF8.GetString(bytesEncrypted);

            return result;
        }

        private string DecryptText(string input)
        {
            // Get the bytes of the string
            byte[] bytesToBeDecrypted = Encoding.UTF8.GetBytes(input);

            byte[] bytesDecrypted = DecryptBytes(bytesToBeDecrypted);

            string result = Encoding.UTF8.GetString(bytesDecrypted);

            return result;
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
            if (connectionState == ConnectionState.Encrypted)
            {
                byte[] decryptedBytes = message.MessageBytes;
                byte[] encryptedBytes = EncryptBytes(decryptedBytes);
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
            if (connectionState == ConnectionState.Encrypted)
            {
                byte[] encryptedBytes = message.MessageBytes;
                byte[] decryptedBytes = DecryptBytes(encryptedBytes);
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
            if (connectionState == ConnectionState.RequestingPublicKey)
            {
                string ascii = ASCIIEncoding.ASCII.GetString(message.MessageBytes);
                if (ascii.Contains(CommunicationPrimitives.SendPublicKey))
                {
                    byte[] publicKeyBytes = new byte[message.MessageLength - CommunicationPrimitives.SendPublicKey.Length - 1];
                    Array.Copy(message.MessageBytes, CommunicationPrimitives.SendPublicKey.Length + 1, publicKeyBytes, 0, publicKeyBytes.Length);
                    string publicKey = ASCIIEncoding.ASCII.GetString(publicKeyBytes);
                    RSACryptoServiceProvider rsa = new RSACryptoServiceProvider(1024);
                    RSAParameters parameters = new RSAParameters();
                    XmlDocument xmlDoc = new XmlDocument();
                    xmlDoc.LoadXml(publicKey);
                    if (xmlDoc.DocumentElement.Name.Equals("RSAKeyValue"))
                    {
                        foreach (XmlNode node in xmlDoc.DocumentElement.ChildNodes)
                        {
                            switch (node.Name)
                            {
                                case "Modulus": parameters.Modulus = (string.IsNullOrEmpty(node.InnerText) ? null : Convert.FromBase64String(node.InnerText)); break;
                                case "Exponent": parameters.Exponent = (string.IsNullOrEmpty(node.InnerText) ? null : Convert.FromBase64String(node.InnerText)); break;
                                case "P": parameters.P = (string.IsNullOrEmpty(node.InnerText) ? null : Convert.FromBase64String(node.InnerText)); break;
                                case "Q": parameters.Q = (string.IsNullOrEmpty(node.InnerText) ? null : Convert.FromBase64String(node.InnerText)); break;
                                case "DP": parameters.DP = (string.IsNullOrEmpty(node.InnerText) ? null : Convert.FromBase64String(node.InnerText)); break;
                                case "DQ": parameters.DQ = (string.IsNullOrEmpty(node.InnerText) ? null : Convert.FromBase64String(node.InnerText)); break;
                                case "InverseQ": parameters.InverseQ = (string.IsNullOrEmpty(node.InnerText) ? null : Convert.FromBase64String(node.InnerText)); break;
                                case "D": parameters.D = (string.IsNullOrEmpty(node.InnerText) ? null : Convert.FromBase64String(node.InnerText)); break;
                            }
                        }
                    }
                    rsa.ImportParameters(parameters);
                    byte[] symkeyBytes = GetUniqueByteKey(32);
                    byte[] encryptedSymKey = rsa.Encrypt(symkeyBytes, false);
                    aesKey = symkeyBytes;
                    byte[] messageHeader = ASCIIEncoding.ASCII.GetBytes(CommunicationPrimitives.SendEncryptionKey + ":");
                    byte[] messageBytes = new byte[encryptedSymKey.Length + messageHeader.Length];
                    Array.Copy(messageHeader, 0, messageBytes, 0, messageHeader.Length);
                    Array.Copy(encryptedSymKey, 0, messageBytes, messageHeader.Length, encryptedSymKey.Length);
                    client.QueueMessage(messageBytes);
                    connectionState = ConnectionState.NegotiatingSymKey;
                    return true;
                }
            }
            else if (connectionState == ConnectionState.NegotiatingSymKey)
            {
                string ascii = ASCIIEncoding.ASCII.GetString(DecryptBytes(message.MessageBytes));
                if (ascii == CommunicationPrimitives.AcceptEncryptionKey)
                {
                    connectionState = ConnectionState.Encrypted;
                    return true;
                }
                else
                {
                    connectionState = ConnectionState.Initial;
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
            if (connectionState == ConnectionState.Initial)
            {

                client.QueueMessage(ASCIIEncoding.ASCII.GetBytes(CommunicationPrimitives.RequestPublicKey));
                this.connectionState = ConnectionState.RequestingPublicKey;
            }
        }
        #endregion
    }
}
