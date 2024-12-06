
using System.Diagnostics;

namespace MauiApp2.Processes;

public class ProcessInfo(
    Process process)
{
    public Process Process => process;
    public int ProcessId { get; init; } = process.Id;
    public string ProcessName { get; init; } = process.ProcessName;
    public DateTime StartTime { get; init; } = process.StartTime;

    //State of process
    public string? State { get; set; }


    //Check if process was terminated
    public bool IsExited
    {
        get
        {
            try
            {
                return process.HasExited;
            }
            catch
            {
                return true;
            }
        }
    }


    public List<ProcessInfo> Children = [];
}
