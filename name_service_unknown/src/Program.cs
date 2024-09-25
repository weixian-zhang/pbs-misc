using Serilog;
using DnsClient;
using System.Net;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .Filter.ByExcluding(logEvent => logEvent.Level == Serilog.Events.LogEventLevel.Warning )
    .WriteTo.Console()
    .CreateLogger();

using HttpClient http = new();


DotEnv.Load();

string host = Environment.GetEnvironmentVariable("Hostname") ?? "gateway.ocbc.com";
string http_address = $"https://{host}";
int.TryParse(Environment.GetEnvironmentVariable("MillisecondBetweenCalls"), out int temp);
int millisecondBetweenCalls = temp == 0 ? 2000 : temp;

string nameserverString = Environment.GetEnvironmentVariable("Nameserver") ?? "8.8.8.8";
var nameserver = IPAddress.Parse(nameserverString);
int.TryParse(Environment.GetEnvironmentVariable("NameserverPort"), out int tempPort);
int nameserverPort = tempPort == 0 ? 53 : tempPort;

var dns = new LookupClient(new IPEndPoint(nameserver, nameserverPort));


while (true)
{
    try 
    {
        var result = await dns.QueryAsync(host, QueryType.A);
        var record = result.Answers.ARecords().FirstOrDefault();
        var ip = record?.Address.ToString();

        Log.Logger.Information($"1. {host} resolved by nameserver {nameserver}:{nameserverPort.ToString()} is successful");
    }
    catch (DnsResponseException drex)
    {
        Log.Logger.Fatal($"1. nameserver {nameserver}:{nameserverPort.ToString()}, DNS-Error={drex.DnsError}, {drex.ToString()}");
    }
    catch(Exception ex) {
        Log.Logger.Fatal(ex.ToString());
    }

    Thread.Sleep(500);

    //make HTTP call
    try
    {
        await http.GetStringAsync(http_address);
    }
    catch(HttpRequestException httpex) {
        if (!httpex.Message.StartsWith("Response status code does not indicate success: 403")) {
            Log.Logger.Fatal($"2. {httpex.ToString()}");
        }
        else {
            Log.Logger.Information($"2. HTTP conection to {host} successfully");
        }
    }
    catch(OperationCanceledException ocex) {
        Log.Logger.Fatal($"2. {ocex.HResult.ToString()}, {ocex.ToString()}");   
    }
    catch (Exception ex)
    {
        Log.Logger.Information($"2. {ex.ToString()}");
    }

    Thread.Sleep(500);

    try 
    {
        var result = await dns.QueryAsync(host, QueryType.A);
        var record = result.Answers.ARecords().FirstOrDefault();
        var ip = record?.Address.ToString();

        Log.Logger.Information($"3. {host} resolved by nameserver {nameserver}:{nameserverPort.ToString()} is successful");
    }
    catch (DnsResponseException drex)
    {
        Log.Logger.Fatal($"3. nameserver {nameserver}:{nameserverPort.ToString()}, DNS-Error={drex.DnsError}, {drex.ToString()}");
    }
    catch(Exception ex) {
        Log.Logger.Fatal(ex.ToString());
    }
    
    Thread.Sleep(millisecondBetweenCalls);
}


