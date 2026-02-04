using System;
using System.Buffers.Binary;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Encrypto.CryptoEngine
{
    /// <summary>
    /// Hybrid file encryption:
    /// - CEK: AES-256-GCM (per-file key)
    /// - CEK wrapped with RSA-OAEP-SHA256
    /// - File format (big-endian fields):
    ///   MAGIC(6) | VERSION(1) | EncKeyLen(2) | EncKey | BaseNonce(8) |
    ///   NameLen(2) | Name(UTF8) | ChunkSize(4) |
    ///   [ chunkCount times: ChunkLen(4) | Ciphertext | Tag(16) ]
    /// </summary>
    public static class HybridCrypto
    {
        private static readonly byte[] Magic = Encoding.ASCII.GetBytes("MMDREZ");
        private const byte Version = 1;
        private const int AesKeySizeBytes = 32;
        private const int BaseNonceSize = 8;
        private const int GcmTagSize = 16;
        private const int DefaultChunkSize = 64 * 1024;

        public static void EncryptFile(
            string inputPath,
            string publicKeyPemPath,
            string? outPath = null,
            int chunkSize = DefaultChunkSize)
        {
            if (!File.Exists(inputPath)) throw new FileNotFoundException(inputPath);
            if (!File.Exists(publicKeyPemPath)) throw new FileNotFoundException(publicKeyPemPath);

            outPath ??= Path.ChangeExtension(inputPath, ".mmdrez");

            string pubPem = File.ReadAllText(publicKeyPemPath);
            string originalName = Path.GetFileName(inputPath);
            byte[] nameBytes = Encoding.UTF8.GetBytes(originalName);

            byte[] cek = RandomNumberGenerator.GetBytes(AesKeySizeBytes);
            byte[] baseNonce = RandomNumberGenerator.GetBytes(BaseNonceSize);

            byte[] encCek;
            using (var rsa = RSA.Create())
            {
                rsa.ImportFromPem(pubPem);
                encCek = rsa.Encrypt(cek, RSAEncryptionPadding.OaepSHA256);
            }

            using var inFs = new FileStream(inputPath, FileMode.Open, FileAccess.Read, FileShare.Read);
            using var outFs = new FileStream(outPath, FileMode.Create, FileAccess.Write, FileShare.None);
            using var bw = new BinaryWriter(outFs, Encoding.UTF8, true);

            bw.Write(Magic);
            bw.Write(Version);

            Span<byte> tmp2 = stackalloc byte[2];
            BinaryPrimitives.WriteUInt16BigEndian(tmp2, (ushort)encCek.Length);
            bw.Write(tmp2);
            bw.Write(encCek);

            bw.Write(baseNonce);

            BinaryPrimitives.WriteUInt16BigEndian(tmp2, (ushort)nameBytes.Length);
            bw.Write(tmp2);
            bw.Write(nameBytes);

            Span<byte> tmp4 = stackalloc byte[4];
            BinaryPrimitives.WriteUInt32BigEndian(tmp4, (uint)chunkSize);
            bw.Write(tmp4);

            using var aes = new AesGcm(cek, GcmTagSize);

            byte[] plainBuffer = new byte[chunkSize];
            byte[] cipherBuffer = new byte[chunkSize];
            byte[] tag = new byte[GcmTagSize];

            uint chunkIndex = 0;
            int read;

            while ((read = inFs.Read(plainBuffer, 0, chunkSize)) > 0)
            {
                byte[] nonce = new byte[12];
                Buffer.BlockCopy(baseNonce, 0, nonce, 0, BaseNonceSize);
                BinaryPrimitives.WriteUInt32BigEndian(nonce.AsSpan(BaseNonceSize), chunkIndex);

                byte[] aad = new byte[nameBytes.Length + 4];
                Buffer.BlockCopy(nameBytes, 0, aad, 0, nameBytes.Length);
                BinaryPrimitives.WriteUInt32BigEndian(aad.AsSpan(nameBytes.Length), chunkIndex);

                aes.Encrypt(nonce, plainBuffer.AsSpan(0, read), cipherBuffer.AsSpan(0, read), tag, aad);

                BinaryPrimitives.WriteUInt32BigEndian(tmp4, (uint)read);
                bw.Write(tmp4);
                bw.Write(cipherBuffer, 0, read);
                bw.Write(tag);

                CryptographicOperations.ZeroMemory(nonce);
                CryptographicOperations.ZeroMemory(aad);
                CryptographicOperations.ZeroMemory(tag);

                Array.Clear(plainBuffer, 0, read);
                Array.Clear(cipherBuffer, 0, read);

                chunkIndex++;
            }

            bw.Flush();
            outFs.Flush();

            CryptographicOperations.ZeroMemory(cek);
        }

        public static void DecryptFile(
            string inputPath,
            string privateKeyPem,
            string? outDirectory = null)
        {
            outDirectory ??= Path.GetDirectoryName(inputPath) ?? ".";

            using var inFs = new FileStream(inputPath, FileMode.Open, FileAccess.Read, FileShare.Read);
            using var br = new BinaryReader(inFs, Encoding.UTF8, true);

            if (!ByteArrayEquals(br.ReadBytes(Magic.Length), Magic))
                throw new InvalidDataException("Invalid file");

            if (br.ReadByte() != Version)
                throw new InvalidDataException("Unsupported version");

            Span<byte> tmp2 = stackalloc byte[2];
            tmp2[0] = br.ReadByte();
            tmp2[1] = br.ReadByte();
            int encKeyLen = BinaryPrimitives.ReadUInt16BigEndian(tmp2);
            byte[] encKey = br.ReadBytes(encKeyLen);

            byte[] baseNonce = br.ReadBytes(BaseNonceSize);

            tmp2[0] = br.ReadByte();
            tmp2[1] = br.ReadByte();
            int nameLen = BinaryPrimitives.ReadUInt16BigEndian(tmp2);
            byte[] nameBytes = br.ReadBytes(nameLen);
            string fileName = Path.GetFileName(Encoding.UTF8.GetString(nameBytes));

            Span<byte> tmp4 = stackalloc byte[4];
            br.Read(tmp4);
            int chunkSize = (int)BinaryPrimitives.ReadUInt32BigEndian(tmp4);

            byte[] cek;
            using (var rsa = RSA.Create())
            {
                rsa.ImportFromPem(privateKeyPem);
                cek = rsa.Decrypt(encKey, RSAEncryptionPadding.OaepSHA256);
            }

            string tempPath = Path.Combine(outDirectory, Guid.NewGuid().ToString("N") + ".tmp");
            string finalPath = Path.Combine(outDirectory, fileName);

            using (var outFs = new FileStream(tempPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            using (var aes = new AesGcm(cek, GcmTagSize))
            {
                uint chunkIndex = 0;

                while (inFs.Position < inFs.Length)
                {
                    br.Read(tmp4);
                    int len = (int)BinaryPrimitives.ReadUInt32BigEndian(tmp4);

                    byte[] cipher = br.ReadBytes(len);
                    byte[] tag = br.ReadBytes(GcmTagSize);

                    byte[] nonce = new byte[12];
                    Buffer.BlockCopy(baseNonce, 0, nonce, 0, BaseNonceSize);
                    BinaryPrimitives.WriteUInt32BigEndian(nonce.AsSpan(BaseNonceSize), chunkIndex);

                    byte[] aad = new byte[nameBytes.Length + 4];
                    Buffer.BlockCopy(nameBytes, 0, aad, 0, nameBytes.Length);
                    BinaryPrimitives.WriteUInt32BigEndian(aad.AsSpan(nameBytes.Length), chunkIndex);

                    byte[] plain = new byte[len];
                    aes.Decrypt(nonce, cipher, tag, plain, aad);

                    outFs.Write(plain, 0, len);

                    CryptographicOperations.ZeroMemory(nonce);
                    CryptographicOperations.ZeroMemory(aad);
                    Array.Clear(cipher, 0, cipher.Length);
                    Array.Clear(plain, 0, plain.Length);

                    chunkIndex++;
                }

                outFs.Flush();
            }

            if (File.Exists(finalPath)) File.Delete(finalPath);
            File.Move(tempPath, finalPath);

            CryptographicOperations.ZeroMemory(cek);
        }

        private static bool ByteArrayEquals(byte[] a, byte[] b)
        {
            if (a.Length != b.Length) return false;
            int diff = 0;
            for (int i = 0; i < a.Length; i++)
                diff |= a[i] ^ b[i];
            return diff == 0;
        }
    }
}