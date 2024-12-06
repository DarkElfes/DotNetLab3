using FluentResults;
using MauiApp2.Domain;
using static MauiApp2.Domain.Encryption;

namespace MauiApp2.Services;

public class EncryptionService
{
    public List<Info> Infos { get; private set; } = [];

    public bool IsWork { get; private set; }
    public int Progress { get; private set; }
    public TimeSpan TimeSpent { get; private set; }

    public event Action? ValuesChanged;


    public async Task<Result> EncodeFileAsync(string filePath, string key)
        => await HandleResultAsync(Encryption.EncodeFileAsync(filePath, key, UpdateProgress, UpdateTimer));
    public async Task<Result> DecodeFileAsync(string filePath, string key)
        => await HandleResultAsync(Encryption.DecodeFileAsync(filePath, key, UpdateProgress, UpdateTimer));

    private async Task<Result> HandleResultAsync(Task<Result<Info>> task)
    {
        IsWork = true;

        var result = await task;
        if(result.IsSuccess)
            Infos.Add(result.Value);

        (IsWork, Progress, TimeSpent) = (false, 0, TimeSpan.Zero);

        return result.ToResult();
    }

    private void UpdateProgress(int progress)
    {
        Progress = progress;
        ValuesChanged?.Invoke();
    }
    private void UpdateTimer(TimeSpan timeSpent)
    {
        TimeSpent = timeSpent;
        ValuesChanged?.Invoke();
    }
}
