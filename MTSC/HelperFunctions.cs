using MTSC.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Xml;

[assembly: InternalsVisibleTo("MTSC.UnitTests")]
namespace MTSC
{
    internal static class HelperFunctions
    {
        private const string Prefix = "<";
        private const string Suffix = ">k__BackingField";

        #region XML

        public static void FromXmlString(this RSA rsa, string xmlString)
        {
            var parameters = new RSAParameters();

            var xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(xmlString);

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
            else
            {
                throw new Exception("Invalid XML RSA key.");
            }

            rsa.ImportParameters(parameters);
        }

        public static string ToXmlString(this RSA rsa, bool includePrivateParameters)
        {
            var parameters = rsa.ExportParameters(includePrivateParameters);

            return string.Format("<RSAKeyValue><Modulus>{0}</Modulus><Exponent>{1}</Exponent><P>{2}</P><Q>{3}</Q><DP>{4}</DP><DQ>{5}</DQ><InverseQ>{6}</InverseQ><D>{7}</D></RSAKeyValue>",
                  parameters.Modulus != null ? Convert.ToBase64String(parameters.Modulus) : null,
                  parameters.Exponent != null ? Convert.ToBase64String(parameters.Exponent) : null,
                  parameters.P != null ? Convert.ToBase64String(parameters.P) : null,
                  parameters.Q != null ? Convert.ToBase64String(parameters.Q) : null,
                  parameters.DP != null ? Convert.ToBase64String(parameters.DP) : null,
                  parameters.DQ != null ? Convert.ToBase64String(parameters.DQ) : null,
                  parameters.InverseQ != null ? Convert.ToBase64String(parameters.InverseQ) : null,
                  parameters.D != null ? Convert.ToBase64String(parameters.D) : null);
        }
        #endregion

        /// <summary>
        /// Returns true if <paramref name="path"/> starts with the path <paramref name="baseDirPath"/>.
        /// The comparison is case-insensitive, handles / and \ slashes as folder separators and
        /// only matches if the base dir folder name is matched exactly ("c:\foobar\file.txt" is not a sub path of "c:\foo").
        /// </summary>
        public static bool IsSubPathOf(this string path, string baseDirPath)
        {
            var normalizedPath = Path.GetFullPath(path.Replace('/', '\\')
                .WithEnding("\\"));

            var normalizedBaseDirPath = Path.GetFullPath(baseDirPath.Replace('/', '\\')
                .WithEnding("\\"));

            return normalizedPath.StartsWith(normalizedBaseDirPath, StringComparison.OrdinalIgnoreCase);
        }
        
        /// <summary>
        /// Returns <paramref name="str"/> with the minimal concatenation of <paramref name="ending"/> (starting from end) that
        /// results in satisfying .EndsWith(ending).
        /// </summary>
        /// <example>"hel".WithEnding("llo") returns "hello", which is the result of "hel" + "lo".</example>
        public static string WithEnding(this string str, string ending)
        {
            if (str == null)
            {
                return ending;
            }

            var result = str;

            // Right() is 1-indexed, so include these cases
            // * Append no characters
            // * Append up to N characters, where N is ending length
            for (var i = 0; i <= ending.Length; i++)
            {
                var tmp = result + ending.Right(i);
                if (tmp.EndsWith(ending))
                {
                    return tmp;
                }
            }

            return result;
        }
        
        /// <summary>Gets the rightmost <paramref name="length" /> characters from a string.</summary>
        /// <param name="value">The string to retrieve the substring from.</param>
        /// <param name="length">The number of characters to retrieve.</param>
        /// <returns>The substring.</returns>
        public static string Right(this string value, int length)
        {
            if (value == null)
            {
                throw new ArgumentNullException("value");
            }

            if (length < 0)
            {
                throw new ArgumentOutOfRangeException("length", length, "Length is less than zero");
            }

            return (length < value.Length) ? value.Substring(value.Length - length) : value;
        }
        
        public static string RelativePath(this string absPath, string relTo)
        {
            var absDirs = absPath.Split('\\');
            var relDirs = relTo.Split('\\');
            // Get the shortest of the two paths 
            var len = absDirs.Length < relDirs.Length ? absDirs.Length : relDirs.Length;
            // Use to determine where in the loop we exited 
            var lastCommonRoot = -1; int index;
            // Find common root 
            for (index = 0; index < len; index++)
            {
                if (absDirs[index] == relDirs[index])
                {
                    lastCommonRoot = index;
                }
                else
                {
                    break;
                }
            }
            // If we didn't find a common prefix then throw 
            if (lastCommonRoot == -1)
            {
                throw new ArgumentException("Paths do not have a common base");
            }
            // Build up the relative path 
            var relativePath = new StringBuilder();
            // Add on the .. 
            for (index = lastCommonRoot + 1; index < absDirs.Length; index++)
            {
                if (absDirs[index].Length > 0)
                {
                    relativePath.Append("..\\");
                }
            }
            // Add on the folders 
            for (index = lastCommonRoot + 1; index < relDirs.Length - 1; index++)
            {
                relativePath.Append(relDirs[index] + "\\");
            }

            relativePath.Append(relDirs[relDirs.Length - 1]);
            return relativePath.ToString();
        }

        public static byte[] ReadRemainingBytes(this MemoryStream ms)
        {
            var buffer = new byte[ms.Length - ms.Position];
            ms.Read(buffer, 0, buffer.Length);
            return buffer;
        }

        public static byte[] TrimTrailingNullBytes(this byte[] bytes)
        {
            var trimSize = 0;
            for(var i = bytes.Length - 1; i >= 0; i--)
            {
                if(bytes[i] == 0)
                {
                    trimSize++;
                }
                else
                {
                    break;
                }
            }

            var newBytes = new byte[bytes.Length - trimSize];
            Array.Copy(bytes, 0, newBytes, 0, newBytes.Length);
            return newBytes;
        }

        /// <summary>
        /// Returns the backing field of a property.
        /// Source: https://gist.github.com/NickStrupat/39e659e53a7aa000b737
        /// </summary>
        /// <param name="propertyInfo"></param>
        /// <returns></returns>
        public static FieldInfo GetBackingField(this PropertyInfo propertyInfo)
        {
            if (propertyInfo == null)
            {
                throw new ArgumentNullException(nameof(propertyInfo));
            }

            if (!propertyInfo.CanRead || !propertyInfo.GetGetMethod(nonPublic: true).IsDefined(typeof(CompilerGeneratedAttribute), inherit: true))
            {
                return null;
            }

            var backingFieldName = GetBackingFieldName(propertyInfo.Name);
            var backingField = propertyInfo.DeclaringType?.GetField(backingFieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            if (backingField == null)
            {
                return null;
            }

            return !backingField.IsDefined(typeof(CompilerGeneratedAttribute), inherit: true) ? null : backingField;
        }

        private static string GetBackingFieldName(string propertyName) => $"{Prefix}{propertyName}{Suffix}";
    }
}
