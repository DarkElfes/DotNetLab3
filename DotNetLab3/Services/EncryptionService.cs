using FluentResults;
using DotNetLab3.Domain.Encryptions;
using System.ComponentModel;

namespace DotNetLab3.Services;

public class EncryptionService
{
    public List<EncryptionInfo> Infos { get; private set; } = [];

    public bool IsWork { get; private set; }
    public int Progress { get; private set; }
    public TimeSpan TimeSpent { get; private set; }

    public event Action? OnPropertyChanged;


    public async Task<Result> EncodeFileAsync(string filePath, string key)
        => await HandleResultAsync(Encryption.EncodeFileAsync(filePath, key, UpdateProgress, UpdateTimer));
    public async Task<Result> DecodeFileAsync(string filePath, string key)
        => await HandleResultAsync(Encryption.DecodeFileAsync(filePath, key, UpdateProgress, UpdateTimer));

    private async Task<Result> HandleResultAsync(Task<Result<EncryptionInfo>> task)
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
        OnPropertyChanged?.Invoke();
    }
    private void UpdateTimer(TimeSpan timeSpent)
    {
        TimeSpent = timeSpent;
        OnPropertyChanged?.Invoke();
    }
}
