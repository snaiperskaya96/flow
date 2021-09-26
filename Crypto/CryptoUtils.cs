using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Org.BouncyCastle.Asn1.Pkcs;
using Org.BouncyCastle.Asn1.X509;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Encodings;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Paddings;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Crypto.Prng;
using Org.BouncyCastle.OpenSsl;
using Org.BouncyCastle.Pkcs;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.X509;

namespace flow.Crypto
{
    public class CryptoUtils
    {
        public static byte[] GenerateEncryptionKey(int length = 32)
        {
            var random = new SecureRandom();
            return random.GenerateSeed(length);
        }

        public static byte[] DecryptData(byte[] key, byte[] data)
        {
            return Cipher(false, key, data);
        }

        public static byte[] EncryptData(byte[] key, byte[] data, byte ivLen = 8)
        {
            return Cipher(true, key, data, ivLen);
        }

        public static string xor(string key, string input)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < input.Length; i++)
                sb.Append((char)(input[i] ^ key[(i % key.Length)]));
            string result = sb.ToString();

            return result;
        }

        // Return a raw pair of private and public rsa keys
        public static (byte[] privateKey, byte[] publicKey) GenerateRsaPair()
        {
            RsaKeyPairGenerator r = new RsaKeyPairGenerator();
            r.Init(new KeyGenerationParameters(new SecureRandom(), 2048));
            AsymmetricCipherKeyPair keys = r.GenerateKeyPair();
            SubjectPublicKeyInfo pubF = SubjectPublicKeyInfoFactory.CreateSubjectPublicKeyInfo(keys.Public);
            byte[] pubbytes = pubF.GetDerEncoded();
            PrivateKeyInfo privF = PrivateKeyInfoFactory.CreatePrivateKeyInfo(keys.Private);
            byte[] privBytes = privF.GetDerEncoded();

            return (privBytes, pubbytes);
        }

        public static byte[] RsaPublicEncrypt(byte[] key, byte[] data)
        {
            SubjectPublicKeyInfo pbInfo = SubjectPublicKeyInfo.GetInstance(key);
            AsymmetricKeyParameter pubKey = PublicKeyFactory.CreateKey(pbInfo);
            IAsymmetricBlockCipher encryptEngine = new RsaEngine();
            encryptEngine.Init(true, pubKey);
            return encryptEngine.ProcessBlock(data, 0, data.Length);
        }

        public static byte[] RsaPrivateEncrypt(byte[] key, byte[] data)
        {
            PrivateKeyInfo pkInfo = PrivateKeyInfo.GetInstance(key);
            AsymmetricKeyParameter privateKey = PrivateKeyFactory.CreateKey(pkInfo);
            IAsymmetricBlockCipher encryptEngine = new RsaEngine();
            encryptEngine.Init(true, privateKey);
            return encryptEngine.ProcessBlock(data, 0, data.Length);
        }

        public static byte[] RsaPrivateDecrypt(byte[] key, byte[] data)
        {
            PrivateKeyInfo pkInfo = PrivateKeyInfo.GetInstance(key);
            AsymmetricKeyParameter privateKey = PrivateKeyFactory.CreateKey(pkInfo);
            IAsymmetricBlockCipher decryptEngine = new RsaEngine();
            decryptEngine.Init(false, privateKey);
            return decryptEngine.ProcessBlock(data, 0, data.Length);
        }

        public static byte[] RsaPublicDecrypt(byte[] key, byte[] data)
        {
            SubjectPublicKeyInfo pbInfo = SubjectPublicKeyInfo.GetInstance(key);
            AsymmetricKeyParameter pubKey = PublicKeyFactory.CreateKey(pbInfo);
            IAsymmetricBlockCipher decryptEngine = new RsaEngine();
            decryptEngine.Init(false, pubKey);
            return decryptEngine.ProcessBlock(data, 0, data.Length);
        }

        public static string GetLicensePath()
        {
            string path = System.IO.Path.GetDirectoryName(
                System.Reflection.Assembly.GetExecutingAssembly().Location
            );
            string licensePath = System.IO.Path.Join(path, "license.fz");
            return licensePath;
        }

        public static string GetLicense()
        {
            if (System.IO.File.Exists(GetLicensePath()))
            {
                using (StreamReader licenseStream = new StreamReader(GetLicensePath()))
                {
                    string content = licenseStream.ReadToEnd();
                    return content;
                }
            }

            return null;
        }

        public static byte[] GetPublicKeyFromLicense()
        {
            string license = GetLicense();

            if (license != null)
            {
                return System.Convert.FromBase64String(license);
            }

            return null;
        }

        public static byte[] Cipher(bool forEncryption, byte[] key, byte[] data, byte ivLen = 8, string algorithm = "AES/CTR/NoPadding")
        {
            if (data.Length < 2) 
            {
                return null;
            }

            byte[] iv = null;

            if (forEncryption)
            {
                var random = new SecureRandom();
                iv = random.GenerateSeed(ivLen);
            }
            else
            {
                ivLen = data[0];
                iv = data.Skip(1).Take(ivLen).ToArray();
            }

            IBufferedCipher cipher = CipherUtilities.GetCipher(algorithm);
            KeyParameter keySpec = ParameterUtilities.CreateKeyParameter("AES", key);
            cipher.Init(forEncryption, new ParametersWithIV(keySpec, iv));
            byte[] result = cipher.DoFinal(forEncryption ? data : data.Skip(1 + ivLen).ToArray());

            if (forEncryption)
            {
                var encrypted = new byte[result.Length + ivLen + 1];
                encrypted[0] = ivLen;
                iv.CopyTo(encrypted, 1);
                result.CopyTo(encrypted, 1 + ivLen);
                return encrypted;
            }

            return result;
        }

    }
}