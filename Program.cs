using Microsoft.Extensions.Configuration;

var config = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .Build();

string BaseUrl = config["ResourceSpace:BaseUrl"]!;
string User = config["ResourceSpace:User"]!;
string PrivateKey = config["ResourceSpace:PrivateKey"]!;

int MaxParallel = int.Parse(config["Upload:MaxParallel"]!);
string InboundPath = config["Upload:InboundPath"]!;

Console.WriteLine(BaseUrl);
