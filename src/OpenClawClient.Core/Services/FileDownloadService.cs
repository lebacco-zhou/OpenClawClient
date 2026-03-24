using System.IO;
using System.Net.Http;
using System.Threading;

namespace OpenClawClient.Core.Services;

/// <summary>
/// 文件下载服务
/// </summary>
public interface IFileDownloadService
{
    Task<string> DownloadFileAsync(string url, string? destinationPath = null, IProgress<double>? progress = null);
    Task<byte[]> DownloadFileToMemoryAsync(string url, IProgress<double>? progress = null);
}

/// <summary>
/// 文件下载服务实现
/// </summary>
public class FileDownloadService : IFileDownloadService
{
    private readonly HttpClient _httpClient = new();

    public async Task<string> DownloadFileAsync(string url, string? destinationPath = null, IProgress<double>? progress = null)
    {
        if (string.IsNullOrEmpty(destinationPath))
        {
            var downloadsPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                "Downloads", "OpenClaw");
            
            Directory.CreateDirectory(downloadsPath);
            
            var fileName = Path.GetFileName(new Uri(url).LocalPath);
            destinationPath = Path.Combine(downloadsPath, fileName);
        }

        using var response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
        response.EnsureSuccessStatusCode();

        var totalBytes = response.Content.Headers.ContentLength ?? -1;
        var canReportProgress = totalBytes != -1 && progress != null;

        using var stream = await response.Content.ReadAsStreamAsync();
        using var fileStream = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true);

        var buffer = new byte[8192];
        long totalRead = 0;
        int read;

        while ((read = await stream.ReadAsync(buffer, CancellationToken.None)) > 0)
        {
            await fileStream.WriteAsync(buffer.AsMemory(0, read), CancellationToken.None);
            totalRead += read;

            if (canReportProgress)
            {
                var percent = (double)totalRead / totalBytes * 100;
                progress?.Report(percent);
            }
        }

        return destinationPath;
    }

    public async Task<byte[]> DownloadFileToMemoryAsync(string url, IProgress<double>? progress = null)
    {
        using var response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
        response.EnsureSuccessStatusCode();

        var totalBytes = response.Content.Headers.ContentLength ?? -1;
        var canReportProgress = totalBytes != -1 && progress != null;

        using var stream = await response.Content.ReadAsStreamAsync();
        using var memoryStream = new MemoryStream();

        var buffer = new byte[8192];
        long totalRead = 0;
        int read;

        while ((read = await stream.ReadAsync(buffer, CancellationToken.None)) > 0)
        {
            await memoryStream.WriteAsync(buffer.AsMemory(0, read), CancellationToken.None);
            totalRead += read;

            if (canReportProgress)
            {
                var percent = (double)totalRead / totalBytes * 100;
                progress?.Report(percent);
            }
        }

        return memoryStream.ToArray();
    }
}
