using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MauiApp1.Models;
using System.Diagnostics;
using System.Timers;

namespace MauiApp1.ViewModels;

public partial class MainViewModel : ObservableObject
{

    [NotifyCanExecuteChangedFor(nameof(DecryptCommand), nameof(EncryptCommand))]
    [ObservableProperty] string? _pathFile, _key = string.Empty;

    [NotifyCanExecuteChangedFor(nameof(DecryptCommand), nameof(EncryptCommand))]
    [ObservableProperty] bool _isUsedNow;

    [ObservableProperty] string? _statusMessage;
    [ObservableProperty] double _progress, _timer;

    [RelayCommand]
    async Task OpenFileDialog()
    {
        FileResult? fileResult = await FilePicker.Default.PickAsync();
        PathFile = fileResult?.FullPath;
    }


    [RelayCommand(CanExecute = nameof(CanUseEncryption))]
    void Encrypt() => EncryptionHandler(FileEncryption.EncryptFile, "Saccessfully encrypt");

    [RelayCommand(CanExecute = nameof(CanUseEncryption))]
    void Decrypt() => EncryptionHandler(FileEncryption.DecryptFile, "Saccessfully decrypt");


    private void EncryptionHandler(Action<string, string, Action<double>> action, string successfulMessage)
    {
        IsUsedNow = true;
        Timer = 0;

        System.Timers.Timer timer = new(TimeSpan.FromSeconds(1));
        timer.Elapsed += (s, e) => Timer++;
        timer.Start();

        Task.Factory.StartNew(() =>
        {
            try
            {
                action.Invoke(PathFile!, Key!, progress => Progress = progress);
                StatusMessage = successfulMessage;
            }
            catch (Exception ex)
            {
                StatusMessage = ex.Message;
            }

            MainThread.BeginInvokeOnMainThread(() =>
            {            
                IsUsedNow = false;
                timer.Stop();
            });
        });
    }

    private bool CanUseEncryption() => File.Exists(PathFile) && !string.IsNullOrWhiteSpace(Key) && !IsUsedNow;
}
