namespace FirewallLog.Utils;

using System.Management;
using System.Runtime.Versioning;

public class ServiceNameResolver
{
    public record Item(string Name, string DisplayName, string PathName);

    private Dictionary<uint, Item> pidToServiceInfo;

    private const string class_service = "Win32_Service";

    private const string property_processId = "ProcessId";
    private const string property_name = "Name";
    private const string property_displayName = "DisplayName";
    private const string property_pathName = "PathName";

    [SupportedOSPlatform("windows")]
    public ServiceNameResolver()
    {
        pidToServiceInfo = LoadServices();
    }

    [SupportedOSPlatform("windows")]
    private static Dictionary<uint, Item> LoadServices()
    {
        var result = new Dictionary<uint, Item>();

        string propertyList = string.Join(", ", property_processId, property_name, property_displayName, property_pathName);
        using (var searcher = new ManagementObjectSearcher($"SELECT {propertyList} FROM {class_service}"))
        {
            using var objects = searcher.Get();

            foreach (var obj in objects)
            {
                var pid = (uint)obj[property_processId];
                if (!result.ContainsKey(pid))
                {
                    result.Add(pid, new Item((string)obj[property_name], (string)obj[property_displayName], (string)obj[property_pathName]));
                }
            }
        }
        return result;
    }

    public Item? GetServiceInfo(uint pid)
    {
        if (pidToServiceInfo.TryGetValue(pid, out Item? svcInfo))
        {
            return svcInfo;
        }
        else
        {
            return null;
        }
    }
}


