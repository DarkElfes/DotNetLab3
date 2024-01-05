
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace MauiApp1.ViewModels;

public partial class ProcessManagerViewModel : ObservableObject
{
    [ObservableProperty]
    ObservableCollection<Process> _processes = new();

    [RelayCommand]
    void StartProcess(string processName)
    {
        Process newProcess = new()
        {
            StartInfo = new()
            {
                FileName = processName
            },
            EnableRaisingEvents = true,
        };
        newProcess.Exited += RemoveProcess;
        newProcess.Start();
        Processes.Add(newProcess);
    }

    private void RemoveProcess(object? s, EventArgs e)
    {
        if (s is Process process
                && Processes.FirstOrDefault(p => p?.Id == process.Id) is Process addedProcess
                    && addedProcess.ProcessName != "calc")
            MainThread.BeginInvokeOnMainThread(() => Processes.Remove(addedProcess));
    }

    [RelayCommand]
    void CloseProcess(int id)
    {
        if (Processes.FirstOrDefault(x => x.Id == id) is Process process)
        {
            if (!process.HasExited)
                process.Kill();
            else if (process.ProcessName == "calc" && Process.GetProcessesByName("calculatorapp")[0] is Process calcAppProcess)
            {
                calcAppProcess.Kill();
                for (int i = 0 ; i < Processes.Count; i++)
                    if (Processes[i].ProcessName == "calc")
                        Processes.RemoveAt(i);
            }
        }
    }       

}
