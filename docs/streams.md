# Media Entities & Streams

OData V4 supports media entities (entities with binary stream content) and named stream properties. This is useful for documents, images, files, and other binary data.

## Table of Contents

- [Overview](#overview)
- [Media Entities](#media-entities)
- [Named Stream Properties](#named-stream-properties)
- [Working with Files](#working-with-files)

## Overview

OData supports two types of binary content:

1. **Media Entities** - The entity itself represents a binary resource, accessed via `$value`
2. **Named Stream Properties** - Entities with one or more binary properties

## Media Entities

Media entities store their binary content separately from their structural properties.

### Getting Stream Content

```csharp
// Get the binary stream for a media entity
using var stream = await client.GetStreamAsync<int>("Documents", 123);

// Save to file
using var fileStream = File.Create("document.pdf");
await stream.CopyToAsync(fileStream);
```

### Setting Stream Content

```csharp
// Upload binary content
using var fileStream = File.OpenRead("photo.jpg");
await client.SetStreamAsync<int>(
    "Photos", 
    123, 
    fileStream, 
    "image/jpeg"
);
```

### Common Workflow

```csharp
// 1. Create the entity metadata first
var doc = await client.CreateAsync("Documents", new Document
{
    Name = "Report.pdf",
    Description = "Q4 Financial Report"
});

// 2. Upload the binary content
using var pdfStream = File.OpenRead("Report.pdf");
await client.SetStreamAsync<int>("Documents", doc.Id, pdfStream, "application/pdf");

// 3. Later, download it
using var downloadStream = await client.GetStreamAsync<int>("Documents", doc.Id);
using var output = File.Create("Downloaded_Report.pdf");
await downloadStream.CopyToAsync(output);
```

## Named Stream Properties

Entities can have multiple named stream properties for storing different binary data.

### Getting a Named Stream

```csharp
// Get a specific stream property
using var thumbnail = await client.GetStreamPropertyAsync<int>(
    "Products", 
    123, 
    "Thumbnail"
);

// Save the thumbnail
using var file = File.Create("thumbnail.png");
await thumbnail.CopyToAsync(file);
```

### Setting a Named Stream

```csharp
// Upload a thumbnail image
using var imageStream = File.OpenRead("thumbnail.png");
await client.SetStreamPropertyAsync<int>(
    "Products",
    123,
    "Thumbnail",
    imageStream,
    "image/png"
);
```

### Multiple Stream Properties

```csharp
// Entity with multiple binary properties
public class Product
{
    public int Id { get; set; }
    public string Name { get; set; }
    // Thumbnail and FullImage are stream properties
}

// Upload different images
using var thumbStream = File.OpenRead("thumb.png");
await client.SetStreamPropertyAsync<int>("Products", 123, "Thumbnail", thumbStream, "image/png");

using var fullStream = File.OpenRead("full.jpg");
await client.SetStreamPropertyAsync<int>("Products", 123, "FullImage", fullStream, "image/jpeg");

// Download specific property
using var thumb = await client.GetStreamPropertyAsync<int>("Products", 123, "Thumbnail");
using var full = await client.GetStreamPropertyAsync<int>("Products", 123, "FullImage");
```

## Working with Files

### Upload with Custom Headers

```csharp
var headers = new Dictionary<string, string>
{
    { "Content-Disposition", "attachment; filename=\"report.pdf\"" }
};

using var stream = File.OpenRead("report.pdf");
await client.SetStreamAsync<int>("Documents", docId, stream, "application/pdf", headers);
```

### Stream to Memory

```csharp
using var stream = await client.GetStreamAsync<int>("Documents", 123);
using var memoryStream = new MemoryStream();
await stream.CopyToAsync(memoryStream);

var bytes = memoryStream.ToArray();
// Process bytes...
```

### Detect Content Type

```csharp
// Get stream with content type from response headers
// Note: You may need to inspect the response for content type
using var stream = await client.GetStreamAsync<int>("Documents", 123);

// Save with appropriate extension based on content type
```

## Entity Model Example

```csharp
public class Document
{
    public int Id { get; set; }
    
    public string Name { get; set; } = string.Empty;
    
    public string? Description { get; set; }
    
    public string? ContentType { get; set; }
    
    public long? Size { get; set; }
    
    public DateTimeOffset? CreatedAt { get; set; }
    
    public DateTimeOffset? ModifiedAt { get; set; }
}

public class Photo
{
    public int Id { get; set; }
    
    public string Title { get; set; } = string.Empty;
    
    // Metadata about the stream (not the stream itself)
    public string? MediaType { get; set; }
    
    public int? Width { get; set; }
    
    public int? Height { get; set; }
}
```

## Common Use Cases

### Document Management

```csharp
// Upload document
var doc = await client.CreateAsync("Documents", new Document
{
    Name = "Contract.pdf",
    ContentType = "application/pdf"
});

using var pdf = File.OpenRead("Contract.pdf");
await client.SetStreamAsync<int>("Documents", doc.Id, pdf, "application/pdf");

// Download document
using var stream = await client.GetStreamAsync<int>("Documents", doc.Id);
using var file = File.Create($"{doc.Name}");
await stream.CopyToAsync(file);
```

### Image Gallery

```csharp
// Upload photo with thumbnail
var photo = await client.CreateAsync("Photos", new Photo
{
    Title = "Sunset",
    MediaType = "image/jpeg"
});

// Set full image
using var full = File.OpenRead("sunset.jpg");
await client.SetStreamAsync<int>("Photos", photo.Id, full, "image/jpeg");

// Set thumbnail
using var thumb = File.OpenRead("sunset_thumb.jpg");
await client.SetStreamPropertyAsync<int>("Photos", photo.Id, "Thumbnail", thumb, "image/jpeg");
```

### Profile Pictures

```csharp
// Update user profile picture
using var avatar = File.OpenRead("avatar.png");
await client.SetStreamPropertyAsync<string>(
    "Users", 
    userId, 
    "ProfilePicture", 
    avatar, 
    "image/png"
);

// Get profile picture
using var stream = await client.GetStreamPropertyAsync<string>("Users", userId, "ProfilePicture");
```

## Best Practices

1. **Use streaming** - Avoid loading entire files into memory
2. **Set correct content type** - Helps clients handle the data properly
3. **Handle large files** - Consider chunked uploads for very large files
4. **Clean up streams** - Always use `using` statements or dispose streams properly
