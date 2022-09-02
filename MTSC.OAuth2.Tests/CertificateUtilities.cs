using System;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace MTSC.OAuth2.Tests;

public static class CertificateUtilities
{
    public static X509Certificate2 CreateNewSelfSignedCertificate()
    {
        using var parent = RSA.Create(4096);
        using var rsa = RSA.Create(2048);
        var parentReq = new CertificateRequest(
            "CN=Experimental Issuing Authority",
            parent,
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1);

        parentReq.CertificateExtensions.Add(
            new X509BasicConstraintsExtension(true, false, 0, true));

        parentReq.CertificateExtensions.Add(
            new X509SubjectKeyIdentifierExtension(parentReq.PublicKey, false));

        return parentReq.CreateSelfSigned(
            DateTimeOffset.UtcNow.AddDays(-45),
            DateTimeOffset.UtcNow.AddDays(365));
    }
}
