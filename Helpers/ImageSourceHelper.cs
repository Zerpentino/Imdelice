using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;

namespace Imdeliceapp.Helpers;

public static class ImageSourceHelper
{
    static readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(20) };

    public static ImageSource Build(string? path, string? placeholder = "no_disponible.png")
    {
        if (string.IsNullOrWhiteSpace(path))
            return ImageSource.FromFile(placeholder ?? "no_disponible.png");

        var uri = ResolveUri(path);
        if (uri == null)
            return ImageSource.FromFile(placeholder ?? "no_disponible.png");

        var source = new StreamImageSource
        {
            Stream = async cancellationToken =>
            {
                var stream = await TryDownloadAsync(uri, cancellationToken);
                if (stream != null)
                    return stream;

                Debug.WriteLine($"[ImageSourceHelper] Falling back to placeholder for {uri}");
                return await LoadPlaceholderAsync(placeholder);
            }
        };

        return source;
    }

    static Uri? ResolveUri(string path)
    {
        if (Uri.TryCreate(path, UriKind.Absolute, out var absolute))
            return absolute;

        var baseUrlObj = Application.Current?.Resources["urlbase"];
        var baseUrl = baseUrlObj?.ToString()?.TrimEnd('/');
        if (string.IsNullOrWhiteSpace(baseUrl))
            return null;

        var finalPath = path.StartsWith('/') ? path : "/" + path;
        if (!finalPath.StartsWith("/api/", StringComparison.OrdinalIgnoreCase))
            finalPath = "/api" + finalPath;

        return Uri.TryCreate(baseUrl + finalPath, UriKind.Absolute, out var full)
            ? full
            : null;
    }

    static async Task<Stream?> TryDownloadAsync(Uri uri, CancellationToken cancellationToken)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, uri);
            var token = await GetTokenAsync();
            if (!string.IsNullOrWhiteSpace(token))
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            Debug.WriteLine($"[ImageSourceHelper] GET {uri}");
            using var response = await _http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            Debug.WriteLine($"[ImageSourceHelper] <- {(int)response.StatusCode} {response.StatusCode}");
            if (!response.IsSuccessStatusCode)
                return null;

            await using var responseStream = await response.Content.ReadAsStreamAsync(cancellationToken);
            var ms = new MemoryStream();
            await responseStream.CopyToAsync(ms, cancellationToken);
            ms.Position = 0;
            return ms;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ImageSourceHelper] Error downloading {uri}: {ex.Message}");
            return null;
        }
    }

    static async Task<Stream> LoadPlaceholderAsync(string? placeholder)
    {
        try
        {
            var file = placeholder ?? "no_disponible.png";
            return await FileSystem.OpenAppPackageFileAsync(file);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ImageSourceHelper] Placeholder error: {ex.Message}");
            return Stream.Null;
        }
    }

    static async Task<string?> GetTokenAsync()
    {
        try
        {
            var secure = await SecureStorage.GetAsync("token");
            if (!string.IsNullOrWhiteSpace(secure)) return secure;
        }
        catch
        {
            // ignored
        }

        var pref = Preferences.Default.Get("token", string.Empty);
        return string.IsNullOrWhiteSpace(pref) ? null : pref;
    }
}
