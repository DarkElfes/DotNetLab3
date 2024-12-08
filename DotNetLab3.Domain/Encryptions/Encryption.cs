using FluentResults;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;

namespace DotNetLab3.Domain.Encryptions;

public static class Encryption
{
    public static async Task<Result<EncryptionInfo>> EncodeFileAsync(string filePath, string key, Action<int>? progressCallback = null, Action<TimeSpan>? timerCallback = null)
    {
        if (ValidateValues(filePath, key) is var result && result.IsFailed)
            return result;

        Stopwatch sw = Stopwatch.StartNew();
        System.Timers.Timer timer = new(TimeSpan.FromMilliseconds(1));
        timer.Elapsed += (_, e) => timerCallback?.Invoke(sw.Elapsed);
        timer.Start();

        EncryptionInfo info = new()
        {
            SrcFilePath = filePath,
            Key = key,
            EncryptionType = EncryptionType.Encode
        };

        try
        {
            using FileStream src = new(filePath, FileMode.Open);

            if (GetAccessibleFilePath(filePath.Replace(info.SrcFilePath, info.SrcFilePath + "_encoded")) is var newFileResult && newFileResult.IsFailed)
                return newFileResult.ToResult();
            info.DestFilePath = newFileResult.Value;
            using FileStream dest = new(newFileResult.Value, FileMode.Create);

            if (GetCryptoStream(key, info.EncryptionType, dest) is var cryptoStreamResult && cryptoStreamResult.IsFailed)
                return cryptoStreamResult.ToResult();
            using CryptoStream destCryptoStream = cryptoStreamResult.Value;

            if (progressCallback is not null)
                await CopyDataAsync(info, src, destCryptoStream, progressCallback);
            else
                await src.CopyToAsync(destCryptoStream);


            return Result.Ok(info)
                 .WithSuccess("File is successfully encoded");
        }
        catch (Exception ex)
        {
            return Result.Fail($"Encoding failed: {ex.Message}");
        }
        finally
        {
            timer.Stop();
            timer.Dispose();
            sw.Stop();
            info.TimeSpent = sw.Elapsed;
        }
    }
    public static async Task<Result<EncryptionInfo>> DecodeFileAsync(string filePath, string key, Action<int>? progressCallback = null, Action<TimeSpan>? timerCallback = null)
    {
        if (ValidateValues(filePath, key) is var validateResult && validateResult.IsFailed)
            return validateResult;

        Stopwatch sw = Stopwatch.StartNew();
        System.Timers.Timer timer = new(TimeSpan.FromMilliseconds(1));
        timer.Elapsed += (_, e) => timerCallback?.Invoke(sw.Elapsed);
        timer.Start();

        EncryptionInfo info = new()
        {
            SrcFilePath = filePath,
            Key = key,
            EncryptionType = EncryptionType.Decode
        };

        try
        {
            using FileStream src = new(filePath, FileMode.Open);

            if (GetAccessibleFilePath(filePath.Replace("_encoded", "")) is var newFileResult && newFileResult.IsFailed)
                return newFileResult.ToResult();
            info.DestFilePath = newFileResult.Value;
            using FileStream dest = new(newFileResult.Value, FileMode.Create);

            if (GetCryptoStream(key, EncryptionType.Decode, src) is var cryptoStreamResult && cryptoStreamResult.IsFailed)
                return cryptoStreamResult.ToResult();
            using CryptoStream srcCryptoStream = cryptoStreamResult.Value;


            if (progressCallback is not null)
                if (await CopyDataAsync(info, srcCryptoStream, dest, progressCallback) is var copyDataResult && copyDataResult.IsFailed)
                    return copyDataResult;
                else
                    await srcCryptoStream.CopyToAsync(dest);


            return Result.Ok(info)
                .WithSuccess("File is successfully decrypted");
        }
        catch (Exception ex)
        {
            return Result.Fail(ex.Message);
        }
        finally
        {
            timer.Stop();
            timer.Dispose();
            sw.Stop();
            info.TimeSpent = sw.Elapsed;
        }
    }


    private static Result ValidateValues(string filePath, string key)
    {
        if (!File.Exists(filePath))
            return Result.Fail("File not found");
        else if (string.IsNullOrWhiteSpace(key))
            return Result.Fail("Key cannot be empty");
        else
            return Result.Ok()
                .WithSuccess("Values was successfully validated");
    }
    private static Result<string> GetAccessibleFilePath(string filePath)
    {
        try
        {
            string? directory = Path.GetDirectoryName(filePath);
            string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(filePath);
            string extension = Path.GetExtension(filePath);

            int counter = 1;
            string newFilePath = filePath;
            while (File.Exists(newFilePath))
            {
                newFilePath = Path.Combine(directory, $"{fileNameWithoutExtension}_{counter}{extension}");
                counter++;
            }
            return Result.Ok(newFilePath)
                .WithSuccess("New path for file successufully found");
        }
        catch (Exception ex)
        {
            return Result.Fail(ex.Message);
        }
    }
    private static Result<CryptoStream> GetCryptoStream(string key, EncryptionType encryptionType, Stream stream)
    {
        try
        {
            using Aes aes = Aes.Create();
            aes.Key = SHA256.HashData(Encoding.UTF8.GetBytes(key));
            aes.IV = aes.Key.Take(16).ToArray();

            CryptoStream? cryptoStream = encryptionType switch
            {
                EncryptionType.Encode => new(stream, aes.CreateEncryptor(aes.Key, aes.IV), CryptoStreamMode.Write),
                EncryptionType.Decode => new(stream, aes.CreateDecryptor(aes.Key, aes.IV), CryptoStreamMode.Read),
                _ => null
            };

            if (cryptoStream is null)
                return Result.Fail("Ivalid encryption type");

            return Result.Ok(cryptoStream);
        }
        catch (Exception ex)
        {
            return Result.Fail(ex.Message);
        }
    }
    private static async Task<Result> CopyDataAsync(EncryptionInfo info, Stream src, Stream dest, Action<int> progressCallback)
    {
        try
        {
            long totalBytes = new FileInfo(info.SrcFilePath).Length;
            long totalBytesRead = 0;

            double onePercent = totalBytes / 100;
            double currentProcent = 0;

            byte[] buffer = new byte[8192];
            while (await src.ReadAsync(buffer.AsMemory(0, buffer.Length)) is int bytesRead && bytesRead > 0)
            {
                await dest.WriteAsync(buffer.AsMemory(0, bytesRead));
                totalBytesRead += bytesRead;

                double progress = (double)totalBytesRead / totalBytes;
                if (progress % onePercent > currentProcent)
                {
                    currentProcent += 0.01;
                    progressCallback.Invoke((int)(currentProcent * 100));
                }

            }
            progressCallback.Invoke(100);
            info.Size = totalBytes / 1024 / 1024;
            info.IsSuccess = true;

            return Result.Ok()
                .WithSuccess("Data was copied successfully");
        }
        catch (CryptographicException)
        {
            return Result.Fail($"Failed decode data: Invalid key");
        }
        catch (Exception ex)
        {
            return Result.Fail($"Failed to copy data: {ex.Message}");
        }
        finally
        {
            try
            {
                dest?.Close();
                dest?.Dispose();
                if (!info.IsSuccess)
                    File.Delete(info.DestFilePath);
            }
            catch { }
        }
    }
}