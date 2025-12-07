namespace Kopi.Core.Models.Common;

public class TargetDbPortRange(int minPort, int maxPort)
{
    public int MinPort { get; set; } = minPort;
    public int MaxPort { get; set; } = maxPort;
}