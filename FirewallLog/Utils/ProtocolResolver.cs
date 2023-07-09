using System.Globalization;
using System.Reflection;

namespace FirewallLog.Utils;

public class ProtocolResolver
{
    private Dictionary<int, string> protocolToName;

    public ProtocolResolver()
    {
        protocolToName = LoadProtocols();
    }

    private Dictionary<int, string> LoadProtocols()
    {
        var result = new Dictionary<int, string>();

        var assembly = Assembly.GetExecutingAssembly();
        using (var stream = assembly.GetManifestResourceStream("FirewallLog.protocol-numbers-1.csv")!)
        {
            using (var reader = new StreamReader(stream))
            {
                string? line;
                while ((line = reader.ReadLine()) != null)
                {
                    if (!line.StartsWith('#'))
                    {
                        var parts = line.Split(',');

                        var protocolNumberStr = parts[0];
                        var protocolName = parts[1];

                        if (protocolName != string.Empty)
                        {
                            result.Add(int.Parse(protocolNumberStr, CultureInfo.InvariantCulture), protocolName);
                        }
                    }
                }
            }
        }

        return result;
    }

    public string? ProtocolToDescription(int protocolNumber)
    {
        string? protocolName;
        protocolToName.TryGetValue(protocolNumber, out protocolName);

        return protocolName;
    }
}