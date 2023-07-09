using System.Globalization;
using System.Reflection;

namespace FirewallLog.Utils;

public class PortResolver
{
    private record Key(string TransportProtocol, ushort PortNumber);

    private Dictionary<Key, string> portToServiceName;

    public PortResolver()
    {
        portToServiceName = LoadPorts();
    }

    private Dictionary<Key, string> LoadPorts()
    {
        var result = new Dictionary<Key, string>();

        var assembly = Assembly.GetExecutingAssembly();
        using (var stream = assembly.GetManifestResourceStream("FirewallLog.service-names-port-numbers.csv")!)
        {
            using (var reader = new StreamReader(stream))
            {
                string? line;
                while ((line = reader.ReadLine()) != null)
                {
                    if (!line.StartsWith('#'))
                    {
                        var parts = line.Split(',');

                        var serviceName = parts[0];
                        var portNumberStr = parts[1];
                        var transportProtocol = parts[2];

                        ushort portFrom;
                        ushort portTo;
                        if (portNumberStr.Contains('-'))
                        {
                            var portNumberParts = portNumberStr.Split('-');
                            if (portNumberParts.Length != 2)
                            {
                                throw new Exception("Invalid port number parts count: " + portNumberStr);
                            }
                            portFrom = ushort.Parse(portNumberParts[0], CultureInfo.InvariantCulture);
                            portTo = ushort.Parse(portNumberParts[1], CultureInfo.InvariantCulture);
                        }
                        else
                        {
                            portFrom = portTo = ushort.Parse(portNumberStr, CultureInfo.InvariantCulture);
                        }

                        if (serviceName != string.Empty)
                        {
                            for (int portNumber = portFrom; portNumber <= portTo; ++portNumber)
                            {
                                var key = new Key(transportProtocol.ToUpperInvariant(), (ushort)portNumber);

                                if (!result.ContainsKey(key))
                                {
                                    result.Add(key, serviceName);
                                }
                            }
                        }
                    }
                }
            }
        }

        return result;
    }

    public string? PortToDescription(string transportProtocol, ushort portNumber)
    {
        var key = new Key(transportProtocol.ToUpperInvariant(), (ushort)portNumber);

        string? serviceName;
        portToServiceName.TryGetValue(key, out serviceName);

        return serviceName;
    }
}