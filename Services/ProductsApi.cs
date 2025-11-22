using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Imdeliceapp.Models;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;

namespace Imdeliceapp.Services;

public class ProductsApi
{
    readonly JsonSerializerOptions _json = new() { PropertyNameCaseInsensitive = true };
    static readonly JsonSerializerOptions _writeJson = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    record ApiEnvelope<T>(T? data, object? error, string? message);

    static async Task<string?> GetTokenAsync()
    {
        var s = await SecureStorage.GetAsync("token");
        if (!string.IsNullOrWhiteSpace(s)) return s;
        var p = Preferences.Default.Get("token", string.Empty);
        return string.IsNullOrWhiteSpace(p) ? null : p;
    }

    static async Task<HttpClient> NewAuthClientAsync()
    {
        var baseUrl = Application.Current.Resources["urlbase"].ToString().TrimEnd('/');
        var token = await GetTokenAsync() ?? string.Empty;
        var cli = new HttpClient { BaseAddress = new Uri(baseUrl), Timeout = TimeSpan.FromSeconds(20) };
        if (!string.IsNullOrWhiteSpace(token))
            cli.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return cli;
    }

    static StringContent ToJsonContent<T>(T payload)
    {
        var json = JsonSerializer.Serialize(payload, _writeJson);
        return new StringContent(json, Encoding.UTF8, "application/json");
    }

    public record ProductCreateResponse(int id);

    public async Task<ProductCreateResponse?> CreateSimpleAsync(object payload)
    {
        using var http = await NewAuthClientAsync();
        using var content = ToJsonContent(payload);
        var resp = await http.PostAsync("/api/products/simple", content);
        var body = await resp.Content.ReadAsStringAsync();
        if (!resp.IsSuccessStatusCode)
            throw new HttpRequestException(body, null, resp.StatusCode);

        var env = JsonSerializer.Deserialize<ApiEnvelope<ProductCreateResponse>>(body, _json);
        return env?.data;
    }

    public async Task UploadImageAsync(int productId, byte[] imageBytes, string? fileName = null)
    {
        if (imageBytes == null || imageBytes.Length == 0) return;

        using var http = await NewAuthClientAsync();
        using var form = new MultipartFormDataContent();
        var imageContent = new ByteArrayContent(imageBytes);

        var contentType = GuessContentType(fileName);
        imageContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);

        var safeName = string.IsNullOrWhiteSpace(fileName) ? $"insumo_{productId}.jpg" : fileName;
        form.Add(imageContent, "image", safeName);

        var resp = await http.PatchAsync($"/api/products/{productId}", form);
        var body = await resp.Content.ReadAsStringAsync();
        if (!resp.IsSuccessStatusCode)
            throw new HttpRequestException(body, null, resp.StatusCode);
    }

    static string GuessContentType(string? fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName)) return "image/jpeg";

        if (fileName.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
            return "image/png";
        if (fileName.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) ||
            fileName.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase))
            return "image/jpeg";

        return "image/jpeg";
    }

    public class ProductVariantBarcodeDTO
    {
        public int id { get; set; }
        public string? name { get; set; }
        public string? barcode { get; set; }
        public bool isActive { get; set; }
    }

    public class ProductBarcodeDTO
    {
        public int id { get; set; }
        public string? type { get; set; }
        public string? name { get; set; }
        public string? barcode { get; set; }
        public string? imageUrl { get; set; }
        public bool hasImage { get; set; }
        public bool isActive { get; set; }
        public List<ProductVariantBarcodeDTO> variants { get; set; } = new();
    }

    public class ProductDetailDTO
    {
        public int id { get; set; }
        public string? type { get; set; }
        public string? name { get; set; }
        public int categoryId { get; set; }
        public string? description { get; set; }
        public string? sku { get; set; }
        public string? barcode { get; set; }
        public bool isActive { get; set; }
        public string? imageUrl { get; set; }
        public bool hasImage { get; set; }
    }

    public async Task<ProductBarcodeDTO?> GetByBarcodeAsync(string barcode)
    {
        if (string.IsNullOrWhiteSpace(barcode)) return null;

        using var http = await NewAuthClientAsync();
        var escaped = Uri.EscapeDataString(barcode.Trim());
        var resp = await http.GetAsync($"/api/products/by-barcode/{escaped}");
        var body = await resp.Content.ReadAsStringAsync();
        if (!resp.IsSuccessStatusCode)
            throw new HttpRequestException(body, null, resp.StatusCode);

        var env = JsonSerializer.Deserialize<ApiEnvelope<ProductBarcodeDTO>>(body, _json);
        return env?.data;
    }

    public async Task<List<CategoryDTO>> ListCategoriesAsync()
    {
        using var http = await NewAuthClientAsync();
        var resp = await http.GetAsync("/api/categories?isActive=true");
        var body = await resp.Content.ReadAsStringAsync();
        if (!resp.IsSuccessStatusCode)
            throw new HttpRequestException(body, null, resp.StatusCode);

        var env = JsonSerializer.Deserialize<ApiEnvelope<List<CategoryDTO>>>(body, _json);
        return env?.data ?? new();
    }

    public class ProductSummaryDTO
    {
        public int id { get; set; }
        public string? name { get; set; }
        public string? type { get; set; }
        public int? priceCents { get; set; }
        public string? description { get; set; }
        public string? sku { get; set; }
        public string? imageUrl { get; set; }
        public bool isActive { get; set; }
        public bool isAvailable { get; set; }
        public string? categorySlug { get; set; }
        public bool hasImage { get; set; }
    }

    public async Task<List<ProductSummaryDTO>> ListProductsAsync(string? categorySlug = null)
    {
        using var http = await NewAuthClientAsync();
        var url = "/api/products";
        if (!string.IsNullOrWhiteSpace(categorySlug))
            url += $"?categorySlug={Uri.EscapeDataString(categorySlug)}";

        var resp = await http.GetAsync(url);
        var body = await resp.Content.ReadAsStringAsync();
        if (!resp.IsSuccessStatusCode)
            throw new HttpRequestException(body, null, resp.StatusCode);

        var env = JsonSerializer.Deserialize<ApiEnvelope<List<ProductSummaryDTO>>>(body, _json);
        return env?.data ?? new();
    }

    public async Task<ProductDetailDTO?> GetProductAsync(int productId)
    {
        using var http = await NewAuthClientAsync();
        var resp = await http.GetAsync($"/api/products/{productId}");
        var body = await resp.Content.ReadAsStringAsync();
        if (!resp.IsSuccessStatusCode)
            throw new HttpRequestException(body, null, resp.StatusCode);

        var env = JsonSerializer.Deserialize<ApiEnvelope<ProductDetailDTO>>(body, _json);
        return env?.data;
    }

    public async Task UpdateProductAsync(int productId, object payload)
    {
        using var http = await NewAuthClientAsync();
        using var content = ToJsonContent(payload);
        var resp = await http.PatchAsync($"/api/products/{productId}", content);
        var body = await resp.Content.ReadAsStringAsync();
        if (!resp.IsSuccessStatusCode)
            throw new HttpRequestException(body, null, resp.StatusCode);
    }

    public async Task RemoveImageAsync(int productId)
    {
        using var http = await NewAuthClientAsync();
        using var form = new MultipartFormDataContent();
        form.Add(new StringContent(string.Empty), "image");
        var resp = await http.PatchAsync($"/api/products/{productId}", form);
        var body = await resp.Content.ReadAsStringAsync();
        if (!resp.IsSuccessStatusCode)
            throw new HttpRequestException(body, null, resp.StatusCode);
    }
}
