# ResourceSpace.Ingester

A lightweight C# command-line ingester for uploading files and metadata into a **ResourceSpace** DAM instance using the official API.

This tool handles:

- Batch ingestion of images
- Full metadata application on resource creation
- Parallel uploads for high throughput
- SHA-256–signed API requests
- Automatic matching between filenames and metadata rows
- Configurable settings via `appsettings.json`

---

## ✨ Features

### ✔ Parallel Uploads  
Process large batches of files efficiently using a configurable degree of parallelism.

### ✔ Metadata-Based Resource Creation  
Reads a CSV file and maps metadata fields to ResourceSpace field IDs.  
Metadata is assigned **during resource creation**, before the file upload.

### ✔ Automatic File-to-Metadata Matching  
Extracts a numeric ID from filenames such as:

```
RS12345_SomeImage.jpg
RS987654_example.png
```

Matches that ID to `ResourceId` from the metadata CSV.

### ✔ Robust Upload Logic  
- Tries both `userfile` and `file` multipart field names  
- Hashes exact query strings to match ResourceSpace API expectations  
- Encodes metadata JSON correctly  
- Handles malformed UTF-8 in CSV input

---

## 📁 Project Structure

```
ResourceSpace.Ingester/
├── Program.cs
├── Models/
│   └── MetadataRow.cs
├── Utils/
│   └── Helpers.cs
├── appsettings.json          # your local config (ignored)
└── appsettings.example.json  # template for others
```

---

## ⚙ Configuration

Copy:

```
appsettings.example.json → appsettings.json
```

Then fill in:

```json
{
  "ResourceSpace": {
    "BaseUrl": "http://your-resourcespace/api/?",
    "User": "admin",
    "PrivateKey": "your_private_key_here"
  },
  "Upload": {
    "MaxParallel": 4,
    "InboundPath": "C:\InboundPhotos",
    "MetadataCsvPath": "C:\InboundPhotos\metadata.csv"
  }
}
```

---

## ▶ Running

```bash
dotnet run
```

Files are processed in parallel, and progress is logged to the console.

---

## 🛑 Notes

- Field IDs used in metadata mapping are *example values* and should be updated to match your ResourceSpace metadata schema.
- This tool is not intended as a generic ResourceSpace SDK, but rather as a practical ingestion utility.
- Requires .NET 8 or later.

---

## 📜 License

MIT License — see `LICENSE` file if provided.

---

## 🤝 Contributing

Feel free to fork and customize for your own ResourceSpace instance or metadata schema.
