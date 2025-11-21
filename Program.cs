using CsvHelper;
using Microsoft.Extensions.Configuration;
using ResourceSpace.Ingester.Models;
using System.Diagnostics;
using System.Globalization;
using System.Net.Http.Headers;
using System.Text.Json;
using H = ResourceSpace.Ingester.Utils.Helpers;


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


async Task ProcessDirectoryParallel(string folderPath, int maxParallel = 4)
{

    var files = Directory.GetFiles(folderPath);

    var csvLookup = rows.ToDictionary(r => r.ResourceId, r => r);

    var jobs =
        from file in Directory.GetFiles(folderPath)
        let id = H.ExtractRsId(Path.GetFileNameWithoutExtension(file))
        where csvLookup.ContainsKey(id)
        select new { file, meta = csvLookup[id] };

    using var throttler = new SemaphoreSlim(maxParallel);

    var tasks = jobs.Select(async job =>
    {
        await throttler.WaitAsync();
        try
        {
            await ProcessSingleFile(job.file, job.meta);
        }
        finally
        {
            throttler.Release();
        }
    });

    await Task.WhenAll(tasks);
}

async Task ProcessSingleFile(string file, MetadataRow meta)
{
    Console.WriteLine($"\n=== Processing {file} ===");

    int refId = await CreateResourceWithMeta(meta);
    if (refId <= 0)
    {
        Console.WriteLine($"❌ Failed to create resource for {file}");
        return;
    }

    Console.WriteLine($"Resource {refId} created.");

    var sw = Stopwatch.StartNew();
    bool ok = await UploadFile(refId, file);
    sw.Stop();

    Console.WriteLine(ok
        ? $"✔ Upload completed in {sw.Elapsed.TotalSeconds:F2} sec"
        : $"❌ Upload failed after {sw.Elapsed.TotalSeconds:F2} sec");

    if (!ok)
        return;

    Console.WriteLine($"File uploaded: {file}");
}

async Task<int> CreateResourceWithMeta(MetadataRow meta)
{   
    var dict = new Dictionary<string, string>();
    dict["8"] = meta.Title;
    dict["12"] = meta.Date;
    dict["89"] = meta.Description;
    dict["90"] = meta.KeywordsMinistry;
    dict["91"] = meta.KeywordsResourceType;
    dict["1"] = meta.KeywordsOther;
    dict["10"] = meta.Credit;
    dict["29"] = meta.NamedPersons;
    dict["3"] = meta.Country;
    dict["93"] = meta.TermsOfUse;
    dict["95"] = meta.ContactInformation;
    dict["96"] = meta.RelatedLinks;
    dict["52"] = meta.CameraMakeModel;
    dict["54"] = meta.Source;

    string metadataJson = JsonSerializer.Serialize(dict);
    string metadataEncoded = Uri.EscapeDataString(metadataJson);

    // Build query entirely from encoded values
    string query =
        $"user={User}" +
        $"&function=create_resource" +
        $"&resource_type=1" +
        $"&metadata={metadataEncoded}" +
        $"&archive=0";

    // Hash the EXACT string sent
    string sign = H.Sha256(PrivateKey + query);

    string url = BaseUrl + query + "&sign=" + sign;

    Console.WriteLine("RAW QUERY FOR HASH:");
    Console.WriteLine(query);
    Console.WriteLine("SIGNATURE:");
    Console.WriteLine(sign);
    Console.WriteLine("URL:");
    Console.WriteLine(url);

    using var client = new HttpClient();
    string response = await client.GetStringAsync(url);
    Console.WriteLine("RESPONSE:");
    Console.WriteLine(response);

    // parse the returned ref
    if (int.TryParse(response, out int idOnly))
        return idOnly;

    // JSON parse fallback
    int idx = response.IndexOf("\"ref\":");
    if (idx > 0)
    {
        int start = idx + 6;
        int end = response.IndexOfAny(new[] { ',', '}' }, start);
        if (end > start && int.TryParse(response.Substring(start, end - start), out int refId))
            return refId;
    }

    return -1;
}

async Task<bool> UploadFile(int refId, string filePath, bool tryAltFieldName = false)
{
    if (!File.Exists(filePath))
    {
        Console.WriteLine($"[Upload] ❌ File missing: {filePath}");
        return false;
    }

    string query = $"user={User}&function=upload_file&ref={refId}";
    string sign = H.Sha256(PrivateKey + query);
    string url = $"{BaseUrl}{query}&sign={sign}";

    Console.WriteLine("[Upload] Raw hashed: " + query);
    Console.WriteLine("[Upload] Signature : " + sign);
    Console.WriteLine("[Upload] URL       : " + url);

    using var http = new HttpClient();
    using var form = new MultipartFormDataContent();
    using var fs = File.OpenRead(filePath);
    var content = new StreamContent(fs);
    content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/octet-stream");

    string fieldName = tryAltFieldName ? "file" : "userfile";
    form.Add(content, fieldName, Path.GetFileName(filePath));

    var resp = await http.PostAsync(url, form);
    var body = (await resp.Content.ReadAsStringAsync())?.Trim();

    Console.WriteLine($"[Upload] HTTP {(int)resp.StatusCode} {resp.ReasonPhrase}");
    Console.WriteLine(body);

    // Try to interpret body as a numeric ref ("1039" or 1039)
    string trimmed = body?.Trim('"', ' ', '\n', '\r', '\t') ?? "";
    bool isNumeric = int.TryParse(trimmed, out _);

    // ✅ SUCCESS: 200 + numeric ref returned
    if (resp.IsSuccessStatusCode && isNumeric)
    {
        Console.WriteLine("[Upload] ✅ Upload succeeded. Ref: " + trimmed);
        return true;
    }

    // Handle classic "No file provided" and retry with alternate field name
    bool noFile = body?.IndexOf("No file", StringComparison.OrdinalIgnoreCase) >= 0;

    if (noFile && !tryAltFieldName)
    {
        Console.WriteLine("[Upload] Retrying with field name 'file'…");
        return await UploadFile(refId, filePath, tryAltFieldName: true);
    }

    Console.WriteLine("[Upload] ❌ Upload failed.");
    return false;
}

var batch = Stopwatch.StartNew();
await ProcessDirectoryParallel(InboundPath, maxParallel: 4);
batch.Stop();

Console.WriteLine($"\n=== Batch completed in {batch.Elapsed.TotalSeconds:F2} sec ===");