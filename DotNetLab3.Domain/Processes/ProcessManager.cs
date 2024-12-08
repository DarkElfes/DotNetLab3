using FluentResults;
using System.Diagnostics;
using System.Management;
using System.Timers;

namespace DotNetLab3.Domain.Processes;

public class ProcessManager : IDisposable
{
    public List<ProcessInfo> ProcessesInfo { get; set; } = [];
    public event Action? ProcessesChanged;

    //Timer for updating information about running processes
    private readonly System.Timers.Timer _timer = new(TimeSpan.FromSeconds(1));


    public ProcessManager()
        => _timer.Elapsed += UpdateProcessesInfo;


    #region Public methods
    public Result StartProcess(string processName)
    {
        try
        {
            Process process = new()
            {
                StartInfo = new()
                {
                    FileName = processName,
                    UseShellExecute = true,
                },
                EnableRaisingEvents = true,
            };

            process.Exited += ExitProcessHandler;
            process.Start();

            if (process is null)
                return Result.Fail("Process not was started");

            lock (ProcessesInfo)
                ProcessesInfo.Add(new ProcessInfo(process));

            ProcessesChanged?.Invoke();

            if (!_timer.Enabled)
                _timer.Start();

            return Result.Ok()
                .WithSuccess($"Process with name {processName} has been successfully launched");
        }
        catch (Exception ex)
        {
            return Result.Fail($"Failed to launch process with name {processName} because: {ex.Message}");
        }
    }
    public Result StopProcess(int processId)
    {
        try
        {
            Process process = Process.GetProcessById(processId);
            process.Kill();

            ProcessesChanged?.Invoke();

            return Result.Ok()
                .WithSuccess($"Process: {process.ProcessName} with id: {processId} is start stopping");
        }
        catch
        {
            return Result.Fail($"An error occurred while trying to stop process");
        }

    }
    #endregion

    #region Private methods
    private void UpdateProcessesInfo(object? s, ElapsedEventArgs e)
    {
        List<ProcessInfo> exitedProcessesInfo = [];

        lock (ProcessesInfo)
        {
            try
            {
                ProcessesInfo.ForEach(pI =>
                {
                    if (!pI.IsExited)
                        UpdateProcessInfo(pI);
                    else
                        exitedProcessesInfo.Add(pI);
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }

            try
            {
                exitedProcessesInfo.ForEach(exitedProcesInfo =>
                {
                    ProcessesInfo.Remove(exitedProcesInfo);
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }
        ProcessesChanged?.Invoke();
    }
    private void UpdateProcessInfo(ProcessInfo processInfo)
    {
        processInfo.Process.Refresh();

        string queryString = $"SELECT * FROM Win32_Process WHERE ParentProcessId = {processInfo.ProcessId}";
        ManagementObjectSearcher searcher = new(queryString);

        var curChildProcesses = searcher
            .Get()
            .Cast<ManagementObject>()
            .ToList();

        curChildProcesses.ForEach(childProcess =>
        {
            int childProcessId = Convert.ToInt32(childProcess["ProcessId"]);
            var childProcessInfo = processInfo.Children.FirstOrDefault(c => c.ProcessId.Equals(childProcessId));

            if (childProcessInfo is null)
            {
                childProcessInfo = new(Process.GetProcessById(childProcessId));
                processInfo.Children.Add(childProcessInfo);
            }

            ProcessesChanged?.Invoke(); 
            UpdateProcessInfo(childProcessInfo);
        });

        var processToRemove = processInfo.Children.Where(pI => pI.IsExited).ToList();
        processToRemove.ForEach(p => processInfo.Children.Remove(p));
    }
    private void ExitProcessHandler(object? s, EventArgs e)
    {
        if (s is not Process process)
            return;

        process.Exited -= ExitProcessHandler;
        ProcessesChanged?.Invoke();
    }
    #endregion


    public void Dispose()
    {
        if (_timer.Enabled)
            _timer.Stop();

        _timer.Elapsed -= UpdateProcessesInfo;
        _timer.Dispose();

        GC.SuppressFinalize(this);
    }
}
