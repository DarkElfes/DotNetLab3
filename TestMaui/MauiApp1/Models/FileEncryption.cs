using System.Security.Cryptography;
using System.Text;

namespace MauiApp1.Models;

public static class FileEncryption
{
    public static void EncryptFile(string inputFile, string key, Action<double>? progressCallback = null)
    {
        if (!File.Exists(inputFile))
            throw new ArgumentNullException(nameof(inputFile));

        using Aes aes = CreateAesInstance(key);

        ICryptoTransform encryptor = aes.CreateEncryptor(aes.Key, aes.IV);

        using FileStream inputStream = new(inputFile, FileMode.Open);
        string fileName = Path.GetFileNameWithoutExtension(inputFile);
        using FileStream outputStream = new(inputFile.Replace(fileName, "Encrypt_" + fileName), FileMode.Create);
        using CryptoStream cryptoStream = new(outputStream, encryptor, CryptoStreamMode.Write);

        if (progressCallback is not null)
            CopyWithProgress(inputFile, inputStream, cryptoStream, progressCallback);
        else
            inputStream.CopyTo(cryptoStream);
    }       

    public static void DecryptFile(string inputFile, string key, Action<double>? progressCallback = null)
    {
        if (!File.Exists(inputFile))
            throw new ArgumentNullException(nameof(inputFile));

        using Aes aes = CreateAesInstance(key);

        ICryptoTransform decryptor = aes.CreateDecryptor(aes.Key, aes.IV);

        using FileStream inputStream = new(inputFile, FileMode.Open);
        using FileStream outputStream = new(inputFile.Replace("Encrypt_", ""), FileMode.Create);
        using CryptoStream cryptoStream = new(inputStream, decryptor, CryptoStreamMode.Read);

        if (progressCallback is not null)
            CopyWithProgress(inputFile, cryptoStream, outputStream, progressCallback);
        else
            cryptoStream.CopyTo(outputStream);
    }

    private static void CopyWithProgress(string inputFile, Stream inputStream, Stream destination, Action<double> progressCallback)
    {
        long totalBytes = new FileInfo(inputFile).Length;
        long totalBytesRead = 0;
        byte[] buffer = new byte[8192];

        double onePercent = totalBytes / 100;
        double currentPercent = 0;

        while (inputStream.Read(buffer, 0, buffer.Length) is int bytesRead && bytesRead > 0)
        {
            destination.Write(buffer, 0, bytesRead);
            totalBytesRead += bytesRead;

            double progress = ((double)totalBytesRead / totalBytes);
            if (progress % onePercent > currentPercent)
            {
                currentPercent += 0.01;
                progressCallback?.Invoke(currentPercent);
            }
        }
        progressCallback?.Invoke(1);
    }

    private static Aes CreateAesInstance(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentNullException(nameof(key));

        Aes aes = Aes.Create();
        using SHA256 sha256 = SHA256.Create();
        aes.Key = sha256.ComputeHash(Encoding.UTF8.GetBytes(key));
        aes.IV = aes.Key.Take(16).ToArray();
        return aes;
    }

}
