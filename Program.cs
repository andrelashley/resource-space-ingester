using CsvHelper;
using Microsoft.Extensions.Configuration;
using ResourceSpace.Ingester.Models;
using System.Globalization;

var config = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .Build();

string BaseUrl = config["ResourceSpace:BaseUrl"]!;
string User = config["ResourceSpace:User"]!;
string PrivateKey = config["ResourceSpace:PrivateKey"]!;

int MaxParallel = int.Parse(config["Upload:MaxParallel"]!);
string InboundPath = config["Upload:InboundPath"]!;
string MetadataPath = config["Upload:MetadataCsvPath"]!;

List<MetadataRow> rows;
using var reader = new StreamReader(MetadataPath);
using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
rows = csv.GetRecords<MetadataRow>().ToList();


foreach (var row in rows)
{
    Console.WriteLine(row.Title);
}