using System;
using System.IO;
using System.Security.Cryptography;

namespace Encrypto.CryptoEngine
{
    /// <summary>
    /// Best-effort secure deletion:
    /// - Rename original file to random temp name (avoid preserving original name in FS)
    /// - Overwrite with random data in chunked fashion for N passes
    /// - Flush and delete
    /// Notes:
    /// - On SSDs and modern filesystems this is NOT a cryptographic guarantee.
    /// - If true 'secure deletion' required, rely on full-disk encryption and key destruction.
    /// </summary>
    public static class SecureDelete
    {
        public static void WipeFile(string path, int passes = 3, int bufferSize = 64 * 1024)
        {
            if (string.IsNullOrEmpty(path)) throw new ArgumentNullException(nameof(path));
            if (!File.Exists(path)) return;
            if (passes < 1) passes = 1;
            if (bufferSize < 4096) bufferSize = 4096;

            string dir = Path.GetDirectoryName(path) ?? ".";
            string tempPath = Path.Combine(dir, Path.GetRandomFileName());

            // rename to temp
            File.Move(path, tempPath);

            var fi = new FileInfo(tempPath);
            long length = fi.Length;

            byte[] buffer = new byte[bufferSize];

            try
            {
                using var fs = new FileStream(tempPath, FileMode.Open, FileAccess.Write, FileShare.None);
                for (int p = 0; p < passes; p++)
                {
                    fs.Seek(0, SeekOrigin.Begin);
                    long remaining = length;
                    while (remaining > 0)
                    {
                        int toWrite = (int)Math.Min(bufferSize, remaining);
                        RandomNumberGenerator.Fill(buffer.AsSpan(0, toWrite));
                        fs.Write(buffer, 0, toWrite);
                        remaining -= toWrite;
                    }
                    fs.Flush(true);
                }
                fs.Close();
            }
            catch
            {
                // best-effort: ignore and attempt delete
            }
            finally
            {
                // zero buffer
                CryptographicOperations.ZeroMemory(buffer);
                try { File.Delete(tempPath); } catch { /* swallow */ }
            }
        }
    }
}