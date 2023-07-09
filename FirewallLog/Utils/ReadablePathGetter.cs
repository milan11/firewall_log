namespace FirewallLog.Utils;

using System.Runtime.InteropServices;
using System.Text;

public class ReadablePathGetter
{
    private Dictionary<string, string> deviceNameToDriveName;

    [DllImport("kernel32.dll")]
    static extern uint QueryDosDevice(string lpDeviceName, StringBuilder lpTargetPath, int ucchMax);

    private const int MAX_PATH = 260;

    private const char pathSeparator = '\\';

    public ReadablePathGetter()
    {
        deviceNameToDriveName = LoadDrives();
    }

    private static Dictionary<string, string> LoadDrives()
    {
        var result = new Dictionary<string, string>();

        var logicalDrives = Directory.GetLogicalDrives();

        var bufferSize = MAX_PATH + 1;
        var deviceName = new StringBuilder(bufferSize);
        foreach (var logicalDrive in logicalDrives)
        {
            string driveName = NormalizeDriveString(logicalDrive);
            if (QueryDosDevice(driveName, deviceName, bufferSize) == 0)
            {
                throw new Exception("Getting device name failed");
            }
            result.Add(NormalizeDriveString(deviceName.ToString()), driveName);
        }

        return result;
    }

    public string? GetReadablePath(string path)
    {
        if (path == "System")
        {
            return path;
        }

        var pathParts = path.Split(pathSeparator, 4);

        if (pathParts[0] != "")
        {
            throw new Exception("Unexpected path: " + path);
        }

        var pathDeviceName = pathParts[1] + pathSeparator + pathParts[2];
        var pathSubpath = pathParts.Length == 4 ? pathParts[3] : "";

        if (deviceNameToDriveName.TryGetValue(NormalizeDriveString(pathDeviceName), out string? driveName))
        {
            string resolvedPath = driveName + pathSeparator + pathSubpath;
            return GetPathWithExactCasing(resolvedPath);
        }
        else
        {
            return null;
        }
    }

    private static string GetPathWithExactCasing(string path)
    {
        if (!File.Exists(path) && !Directory.Exists(path))
        {
            return path;
        }

        var currentDirectoryInfo = new DirectoryInfo(path);

        var parts = new List<string>();

        while (currentDirectoryInfo != null)
        {
            bool isDriveName = (currentDirectoryInfo.Parent == null);
            if (isDriveName)
            {
                parts.Add(currentDirectoryInfo.Name);
            }
            else
            {
                parts.Add(currentDirectoryInfo.Parent!.GetFileSystemInfos(currentDirectoryInfo.Name).Single().Name);
            }

            currentDirectoryInfo = currentDirectoryInfo.Parent;
        }

        parts.Reverse();

        return Path.Combine(parts.ToArray());
    }

    private static string NormalizeDriveString(string driveString)
    {
        return driveString
            .Trim(pathSeparator)
            .ToUpperInvariant()
            ;
    }
}
