using System;
using System.Buffers.Binary;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Encrypto.CryptoEngine
{
    /// <summary>
    /// RSA key pair generation and password-protected private key storage.
    /// Format for private.enc:
    ///  MAGIC(6) | VERSION(1) | salt(16) | iterations(4 BE) | iv(12) | ciphertext | tag(16)
    /// Public PEM saved in provided path.
    /// </summary>
    public static class RsaKeyManager
    {
        private static readonly byte[] Magic = Encoding.ASCII.GetBytes("MMDREZ");
        private const byte Version = 1;
        private const int SaltSize = 16;
        private const int IvSize = 12;
        private const int TagSize = 16;              // 128-bit GCM tag
        private const int DefaultIterations = 200_000;

        public static void GenerateKeys(
            string password,
            string publicPath = "public.pem",
            string protectedPrivatePath = "private.enc",
            int rsaKeySize = 4096,
            int pbkdf2Iterations = DefaultIterations)
        {
            if (string.IsNullOrWhiteSpace(password))
                throw new ArgumentNullException(nameof(password));

            using var rsa = RSA.Create(rsaKeySize);
            string publicPem = rsa.ExportSubjectPublicKeyInfoPem();
            string privatePem = rsa.ExportPkcs8PrivateKeyPem();

            // derive AES key
            byte[] salt = RandomNumberGenerator.GetBytes(SaltSize);
            using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, pbkdf2Iterations, HashAlgorithmName.SHA256);
            byte[] aesKey = pbkdf2.GetBytes(32);
            byte[] iv = RandomNumberGenerator.GetBytes(IvSize);

            // encrypt privatePem with AES-GCM
            byte[] plainBytes = Encoding.UTF8.GetBytes(privatePem);
            byte[] cipher = new byte[plainBytes.Length];
            byte[] tag = new byte[TagSize];

            using (var aes = new AesGcm(aesKey, TagSize))
            {
                // AAD
                aes.Encrypt(iv, plainBytes, cipher, tag, associatedData: null);
            }

            // write public & protected private
            File.WriteAllText(publicPath, publicPem);

            using (var fs = new FileStream(protectedPrivatePath, FileMode.Create, FileAccess.Write, FileShare.None))
            using (var bw = new BinaryWriter(fs, Encoding.UTF8, leaveOpen: false))
            {
                bw.Write(Magic);
                bw.Write(Version);

                // salt
                bw.Write(salt);

                // iterations (4 BE)
                Span<byte> tmp4 = stackalloc byte[4];
                BinaryPrimitives.WriteUInt32BigEndian(tmp4, (uint)pbkdf2Iterations);
                bw.Write(tmp4);

                // iv
                bw.Write(iv);

                // ciphertext
                bw.Write(cipher);

                // tag
                bw.Write(tag);
            }

            // cleanup
            CryptographicOperations.ZeroMemory(aesKey);
            Array.Clear(plainBytes, 0, plainBytes.Length);
            Array.Clear(tag, 0, tag.Length);
        }

        /// <summary>
        /// Decrypts the password-protected private key file and returns the PEM string.
        /// </summary>
        public static string DecryptPrivateKey(string password, string protectedPrivatePath = "private.enc")
        {
            if (string.IsNullOrWhiteSpace(password))
                throw new ArgumentNullException(nameof(password));
            if (!File.Exists(protectedPrivatePath))
                throw new FileNotFoundException("Protected private key file not found", protectedPrivatePath);

            byte[] data = File.ReadAllBytes(protectedPrivatePath);
            int pos = 0;

            if (data.Length < Magic.Length + 1 + SaltSize + 4 + IvSize + TagSize)
                throw new InvalidDataException("Protected key file is truncated or invalid.");

            ReadOnlySpan<byte> span = data;

            // MAGIC
            if (!span.Slice(pos, Magic.Length).SequenceEqual(Magic))
                throw new InvalidDataException("Invalid protected key file (magic mismatch).");
            pos += Magic.Length;

            // VERSION
            byte ver = data[pos++];
            if (ver != Version)
                throw new InvalidDataException($"Unsupported protected key version {ver}.");

            // SALT
            byte[] salt = span.Slice(pos, SaltSize).ToArray();
            pos += SaltSize;

            // ITERATIONS
            uint iterations = BinaryPrimitives.ReadUInt32BigEndian(span.Slice(pos, 4));
            pos += 4;

            // IV
            byte[] iv = span.Slice(pos, IvSize).ToArray();
            pos += IvSize;

            // CIPHERTEXT + TAG
            int cipherLen = data.Length - pos - TagSize;
            if (cipherLen <= 0)
                throw new InvalidDataException("No ciphertext present.");
            byte[] cipher = span.Slice(pos, cipherLen).ToArray();
            pos += cipherLen;
            byte[] tag = span.Slice(pos, TagSize).ToArray();
            pos += TagSize;

            using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, (int)iterations, HashAlgorithmName.SHA256);
            byte[] aesKey = pbkdf2.GetBytes(32);

            byte[] plain = new byte[cipher.Length];
            try
            {
                using var aes = new AesGcm(aesKey, TagSize);
                aes.Decrypt(iv, cipher, tag, plain, associatedData: null);
                string result = Encoding.UTF8.GetString(plain);
                return result;
            }
            catch (CryptographicException ex)
            {
                throw new UnauthorizedAccessException("Incorrect password or corrupted protected key file.", ex);
            }
            finally
            {
                CryptographicOperations.ZeroMemory(aesKey);
                Array.Clear(plain, 0, plain.Length);
                Array.Clear(tag, 0, tag.Length);
            }
        }
    }
}