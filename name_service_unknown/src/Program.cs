using Serilog;
using DnsClient;
using System.Net;
using System.Collections.Concurrent;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .Filter.ByExcluding(logEvent => logEvent.Level == Serilog.Events.LogEventLevel.Warning )
    .WriteTo.Console()
    .CreateLogger();


DotEnv.Load();

const string httpErrorPrefix = "http_error";
const string dnsErrorPrefix = "dns_error";


string url = Environment.GetEnvironmentVariable("Url") ?? "http://httpbin.org/delay/2";
string host = new Uri(url).Host;
int.TryParse(Environment.GetEnvironmentVariable("MillisecondBetweenCalls"), out int temp);
int millisecondBetweenCalls = temp == 0 ? 2000 : temp;

string nameserverString = Environment.GetEnvironmentVariable("Nameserver") ?? "8.8.8.8";
var nameserver = IPAddress.Parse(nameserverString);
int.TryParse(Environment.GetEnvironmentVariable("NameserverPort"), out int tempPort);
int nameserverPort = tempPort == 0 ? 53 : tempPort;

var dns = new LookupClient(new IPEndPoint(nameserver, nameserverPort));

int.TryParse(Environment.GetEnvironmentVariable("NumberOfConcurrentHTTPCall"), out int tempCalls);
int numberOfConcurrentHTTPCall = tempCalls == 0 ? 30 : tempCalls;

while (true)
{

    try 
    {
        var result = await dns.QueryAsync(host, QueryType.A);
        var record = result.Answers.ARecords().FirstOrDefault();
        var ip = record?.Address.ToString();

        Log.Logger.Information($"Step-1. {host} resolved by nameserver {nameserver}:{nameserverPort.ToString()} is successful");
    }
    catch (DnsResponseException drex)
    {
        Log.Logger.Fatal($"Step-1. {dnsErrorPrefix} - nameserver {nameserver}:{nameserverPort.ToString()}, DNS-Error={drex.DnsError}, {drex.ToString()}");
    }
    catch(Exception ex) {
        Log.Logger.Fatal($"Step-1 - {dnsErrorPrefix} - {ex.ToString()}");
    }


    // http call
    int degreeOfParallelism = numberOfConcurrentHTTPCall;
    using var semaphoreSlim  = new SemaphoreSlim(degreeOfParallelism, degreeOfParallelism);
    var tasks = Enumerable.Range(0, numberOfConcurrentHTTPCall).Select(async i =>
    {
        await semaphoreSlim.WaitAsync();
        try
        {
            await httpCall();
        }
        finally
        {
            semaphoreSlim.Release();
        }
    });

    // Wait for all tasks to complete
    await Task.WhenAll(tasks);

    try 
    {
        var result = await dns.QueryAsync(host, QueryType.A);
        var record = result.Answers.ARecords().FirstOrDefault();
        var ip = record?.Address.ToString();

        Log.Logger.Information($"Step-3 - {host} resolved by nameserver {nameserver}:{nameserverPort.ToString()} is successful");
    }
    catch (DnsResponseException drex)
    {
        Log.Logger.Fatal($"Step-3 - {dnsErrorPrefix} - nameserver {nameserver}:{nameserverPort.ToString()}, DNS-Error={drex.DnsError}, {drex.ToString()}");
    }
    catch(Exception ex) {
        Log.Logger.Fatal($"Step-3 - {dnsErrorPrefix} - {ex.ToString()}");
    }

    await Task.Delay(millisecondBetweenCalls);

}


async Task httpCall() {
    try
    {
        var http = new HttpClient();
        var resp = await http.GetStringAsync(url);

        Log.Logger.Information($"Step-2 - HTTP connection to {url} successfully");

        //var resp = await http.PostAsync(http_address, null);
        // if (resp.StatusCode == HttpStatusCode.Forbidden) {
        //     Log.Logger.Information($"Step-2 - HTTP conection to {http_address} successfully");
        // }
    }
    catch(HttpRequestException httpex) {
        if (!httpex.Message.StartsWith("Response status code does not indicate success: 403")) {
            Log.Logger.Fatal($"Step-2 - {httpErrorPrefix} - {httpex.ToString()}");
        }
        else {
            Log.Logger.Information($"Step-2 - {httpErrorPrefix} - HTTP connection to {url} successfully");
        }
    }
    catch(OperationCanceledException ocex) {
        Log.Logger.Fatal($"Step-2 - {httpErrorPrefix} - {ocex.HResult.ToString()}, {ocex.ToString()}");   
    }
    catch (Exception ex)
    {
        Log.Logger.Information($"Step-2 - {httpErrorPrefix} - {ex.ToString()}");
    }
}




