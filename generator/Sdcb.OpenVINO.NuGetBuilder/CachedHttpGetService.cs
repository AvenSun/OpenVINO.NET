﻿using System.Text.Json;

namespace Sdcb.OpenVINO.NuGetBuilder;

public class CachedHttpGetService
{
    private readonly string _cacheFolder;

    public CachedHttpGetService(string cacheFolder)
    {
        if (cacheFolder == null) throw new ArgumentNullException(nameof(cacheFolder));
        Directory.CreateDirectory(cacheFolder);

        _cacheFolder = cacheFolder;
    }

    public async Task<MemoryStream> DownloadAsStream(string url, CancellationToken cancellationToken = default)
    {
        string fileName = url.Split('/').Last();
        string localFilePath = Path.Combine(_cacheFolder, fileName);

        if (!File.Exists(localFilePath))
        {
            // If the file does not exist locally, download it from the url and save it to the cache folder
            using (HttpClient client = new())
            {
                HttpResponseMessage response = await client.GetAsync(url, cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    Stream contentStream = await response.Content.ReadAsStreamAsync();

                    using (FileStream file = File.Create(localFilePath))
                    {
                        using (MemoryStream memoryStream = new())
                        {
                            await contentStream.CopyToAsync(memoryStream, cancellationToken);
                            memoryStream.Seek(0, SeekOrigin.Begin);
                            memoryStream.WriteTo(file);
                        }
                    }
                }
                else
                {
                    throw new Exception($"Error occurred while getting content from URL: {await response.Content.ReadAsStringAsync()}");
                }
            }
        }

        // Now we're sure that the file exists already locally, load it from the file
        MemoryStream ms = new();
        using (FileStream file = File.OpenRead(localFilePath))
        {
            await file.CopyToAsync(ms, cancellationToken);
        }
        ms.Position = 0;
        return ms;
    }

    public async Task<T> DownloadAsJsonAsync<T>(string url, CancellationToken cancellationToken = default)
    {
        using MemoryStream ms = await DownloadAsStream(url, cancellationToken);
        T result = JsonSerializer.Deserialize<T>(ms) ?? throw new Exception($"Failed to deserialize {url} stream to {typeof(T)}");
        return result;
    }
}
