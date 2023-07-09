using System.Diagnostics;
using System.Globalization;
using System.Security;
using FirewallLog.Utils;

if (!OperatingSystem.IsWindows())
{
    await Console.Error.WriteLineAsync("This program is intended to be run only on Windows.");
    return -1;
}

// var firewallEventInstanceIds_allowed = new long[] { 5154, 5156 };
var firewallEventInstanceIds_blocked = new long[] { 5150, 5151, 5152, 5155, 5157 };

var protocolResolver = new ProtocolResolver();
var portResolver = new PortResolver();
var readablePathGetter = new ReadablePathGetter();
var serviceNameResolver = new ServiceNameResolver();

var eventLog = new EventLog("security");

var since = DateTime.Now.AddMinutes(-1);

try
{
    if (eventLog.Entries.Count == 0)
    {
        Console.Error.WriteLine("No recent entries. Make sure the auditing is enabled:");
        Console.Error.WriteLine(@"C:\windows\system32\auditpol.exe /set ""/subcategory:{0CCE9226-69AE-11D9-BED3-505054503030}"" /failure:enable");
    }
}
catch (SecurityException)
{
    Console.Error.WriteLine("Unable to get Event Log entries. Make sure to run this program as Administrator");
    throw;
}

foreach (EventLogEntry entry in eventLog.Entries)
{
    if (firewallEventInstanceIds_blocked.Contains(entry.InstanceId) && entry.TimeGenerated >= since)
    {
        var pid = uint.Parse(GetEntryReplacementString(entry, 0), CultureInfo.InvariantCulture);
        var direction = GetDirectionLabel(GetEntryReplacementString(entry, 2));
        var protocol = int.Parse(GetEntryReplacementString(entry, 7), CultureInfo.InvariantCulture);

        var path = ResolveOrDefault(GetEntryReplacementString(entry, 1), readablePathGetter.GetReadablePath, value => value);

        var fileName = System.IO.Path.GetFileName(path);
        var serviceInfo = serviceNameResolver.GetServiceInfo(pid);
        string protocolStr = protocolResolver.ProtocolToDescription(protocol) ?? throw new Exception("Protocol not found: " + protocol);

        string sourceIP = GetEntryReplacementString(entry, 3);
        string sourcePort = GetEntryReplacementString(entry, 4);
        string targetIP = GetEntryReplacementString(entry, 5);
        string targetPort = GetEntryReplacementString(entry, 6);

        string portDescription = ResolveOrDefault(ushort.Parse(targetPort, CultureInfo.InvariantCulture), (targetPort) => portResolver.PortToDescription(protocolStr, targetPort), _ => "?");

        ConsoleUtils.WriteWithColor(ConsoleColor.Blue, entry.TimeGenerated.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture));
        Console.Write(' ');

        ConsoleUtils.WriteWithColor(ConsoleColor.Gray, direction);
        Console.Write(' ');

        ConsoleUtils.WriteWithColor(ConsoleColor.Blue, protocolStr);
        Console.Write(' ');

        ConsoleUtils.WriteWithColor(ConsoleColor.Magenta, sourceIP);
        Console.Write(' ');

        ConsoleUtils.WriteWithColor(ConsoleColor.Red, sourcePort);

        Console.Write(" > ");

        ConsoleUtils.WriteWithColor(ConsoleColor.Magenta, targetIP);
        Console.Write(' ');

        ConsoleUtils.WriteWithColor(ConsoleColor.Red, targetPort);
        Console.Write(' ');

        ConsoleUtils.WriteWithColor(ConsoleColor.DarkGray, portDescription);
        Console.Write(' ');

        ConsoleUtils.WriteWithColor(ConsoleColor.White, fileName);
        Console.Write(' ');

        ConsoleUtils.WriteWithColor(ConsoleColor.DarkGreen, pid.ToString(CultureInfo.InvariantCulture));
        Console.Write(' ');

        ConsoleUtils.WriteWithColor(ConsoleColor.DarkGray, path);

        if (serviceInfo != null)
        {
            Console.Write(' ');
            Console.Write('(');

            ConsoleUtils.WriteWithColor(ConsoleColor.White, serviceInfo.Name);
            Console.Write(' ');

            ConsoleUtils.WriteWithColor(ConsoleColor.DarkGreen, serviceInfo.DisplayName);
            Console.Write(' ');

            ConsoleUtils.WriteWithColor(ConsoleColor.DarkGray, serviceInfo.PathName);
            Console.Write(')');
        }

        Console.WriteLine();
    }
}

Console.WriteLine("[DONE]");
Console.ReadLine();

return 0;

string GetEntryReplacementString(EventLogEntry entry, int i)
{
    return entry.ReplacementStrings.ElementAt(i);
}

string GetDirectionLabel(string direction)
{
    if (direction == "%%14593")
    {
        return "OUT";
    }

    if (direction == "%%14592")
    {
        return "IN ";
    }

    throw new Exception($"Invalid direction: {direction}");
}

string ResolveOrDefault<T>(T toResolve, Func<T, string?> resolver, Func<T, string> defaultValueCreator)
{
    var resolved = resolver(toResolve);

    if (resolved != null)
    {
        return resolved;
    }
    else
    {
        return defaultValueCreator(toResolve);
    }
}
