namespace DotNetLab3.Domain.Encryptions;

public class EncryptionInfo
{
    public string SrcFilePath { get; set; } = string.Empty;
    public string DestFilePath { get; set; } = string.Empty;
    public string Key { get; set; } = string.Empty;
    public string SrcFileName => Path.GetFileName(SrcFilePath);
    public string DestFileName => Path.GetFileName(DestFilePath);
    public long Size { get; set; }
    public EncryptionType EncryptionType { get; set; }
    public TimeSpan TimeSpent { get; set; }
    public bool IsSuccess { get; set; }
}
