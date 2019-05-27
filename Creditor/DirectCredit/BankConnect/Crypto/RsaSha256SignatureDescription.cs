using System.Security.Cryptography;

namespace SecuredClient.Crypto
{
    public class RsaSha256SignatureDescription : SignatureDescription
    {
        public const string Name = "http://www.w3.org/2001/04/xmldsig-more#rsa-sha256";

        public RsaSha256SignatureDescription()
        {
            KeyAlgorithm = "System.Security.Cryptography.RSACryptoServiceProvider";
            DigestAlgorithm = "System.Security.Cryptography.SHA256Managed";
            FormatterAlgorithm = "System.Security.Cryptography.RSAPKCS1SignatureFormatter";
            DeformatterAlgorithm = "System.Security.Cryptography.RSAPKCS1SignatureDeformatter";
        }

        public override AsymmetricSignatureDeformatter CreateDeformatter(AsymmetricAlgorithm key)
        {
            var asymmetricSignatureDeformatter = (AsymmetricSignatureDeformatter)CryptoConfig.CreateFromName(DeformatterAlgorithm);
            asymmetricSignatureDeformatter.SetKey(key);
            asymmetricSignatureDeformatter.SetHashAlgorithm("SHA256");
            return asymmetricSignatureDeformatter;
        }
    }
}
