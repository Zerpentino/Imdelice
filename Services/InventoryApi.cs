using System;
using System.Collections.Generic;
using System.Diagnostics;
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

public class InventoryApi
{
    readonly JsonSerializerOptions _json = new() { PropertyNameCaseInsensitive = true };
    static readonly JsonSerializerOptions _writeJson = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };
    static readonly JsonSerializerOptions _writeJsonAllowNulls = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.Never
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

    public async Task<List<InventoryItemDTO>> ListItemsAsync(
        string? search = null,
        int? locationId = null,
        int? categoryId = null,
        int? itemId = null,
        int? productId = null,
        int? variantId = null)
    {
        using var http = await NewAuthClientAsync();
        var query = BuildItemsQuery(search, locationId, categoryId, itemId, productId, variantId);
        var url = "/api/inventory/items" + query;
        Debug.WriteLine($"[InventoryApi] GET {url}");
        var resp = await http.GetAsync(url);
        var body = await resp.Content.ReadAsStringAsync();
        Debug.WriteLine($"[InventoryApi] <- {(int)resp.StatusCode} {resp.StatusCode}: {body}");
        if (!resp.IsSuccessStatusCode)
            throw new HttpRequestException(body, null, resp.StatusCode);

        var env = JsonSerializer.Deserialize<ApiEnvelope<List<InventoryItemDTO>>>(body, _json);
        return env?.data ?? new();
    }

    public async Task<InventoryItemDTO?> GetItemAsync(int itemId)
    {
        using var http = await NewAuthClientAsync();
        var resp = await http.GetAsync($"/api/inventory/items/{itemId}");
        var body = await resp.Content.ReadAsStringAsync();
        
        if (!resp.IsSuccessStatusCode)
            throw new HttpRequestException(body, null, resp.StatusCode);

        var env = JsonSerializer.Deserialize<ApiEnvelope<InventoryItemDTO>>(body, _json);
        return env?.data;
    }

    public async Task<List<InventoryMovementDTO>> GetItemMovementsAsync(int itemId, int? limit = 50)
    {
        using var http = await NewAuthClientAsync();
        var query = limit.HasValue ? $"?limit={limit.Value}" : string.Empty;
        var resp = await http.GetAsync($"/api/inventory/items/{itemId}/movements{query}");
        var body = await resp.Content.ReadAsStringAsync();
        if (!resp.IsSuccessStatusCode)
            throw new HttpRequestException(body, null, resp.StatusCode);

        var env = JsonSerializer.Deserialize<ApiEnvelope<List<InventoryMovementDTO>>>(body, _json);
        return env?.data ?? new();
    }

    public async Task<List<InventoryMovementDTO>> ListMovementsAsync(
        int? productId = null,
        int? variantId = null,
        int? locationId = null,
        int? orderId = null,
        string? type = null,
        int? limit = 50)
    {
        using var http = await NewAuthClientAsync();
        var query = BuildMovementsQuery(productId, variantId, locationId, orderId, type, limit);
        var resp = await http.GetAsync("/api/inventory/movements" + query);
        var body = await resp.Content.ReadAsStringAsync();
        if (!resp.IsSuccessStatusCode)
            throw new HttpRequestException(body, null, resp.StatusCode);

        var env = JsonSerializer.Deserialize<ApiEnvelope<List<InventoryMovementDTO>>>(body, _json);
        return env?.data ?? new();
    }

    public async Task<InventoryMovementDTO?> CreateMovementAsync(InventoryMovementRequest dto)
    {
        using var http = await NewAuthClientAsync();
        using var content = ToJsonContent(dto);
        var resp = await http.PostAsync("/api/inventory/movements", content);
        var body = await resp.Content.ReadAsStringAsync();
        if (!resp.IsSuccessStatusCode)
            throw new HttpRequestException(body, null, resp.StatusCode);

        var env = JsonSerializer.Deserialize<ApiEnvelope<InventoryMovementDTO>>(body, _json);
        return env?.data;
    }

    public async Task<InventoryMovementDTO?> CreateMovementByBarcodeAsync(InventoryMovementByBarcodeRequest dto)
    {
        using var http = await NewAuthClientAsync();
        using var content = ToJsonContent(dto);
        var resp = await http.PostAsync("/api/inventory/movements/by-barcode", content);
        var body = await resp.Content.ReadAsStringAsync();
        if (!resp.IsSuccessStatusCode)
            throw new HttpRequestException(body, null, resp.StatusCode);

        var env = JsonSerializer.Deserialize<ApiEnvelope<InventoryMovementDTO>>(body, _json);
        return env?.data;
    }

    public async Task<List<InventoryLocationDTO>> ListLocationsAsync()
    {
        using var http = await NewAuthClientAsync();
        var resp = await http.GetAsync("/api/inventory/locations");
        var body = await resp.Content.ReadAsStringAsync();
        if (!resp.IsSuccessStatusCode)
            throw new HttpRequestException(body, null, resp.StatusCode);

        var env = JsonSerializer.Deserialize<ApiEnvelope<List<InventoryLocationDTO>>>(body, _json);
        return env?.data ?? new();
    }

    public async Task<InventoryLocationDTO?> CreateLocationAsync(InventoryLocationCreateRequest dto)
    {
        using var http = await NewAuthClientAsync();
        using var content = ToJsonContent(dto);
        var resp = await http.PostAsync("/api/inventory/locations", content);
        var body = await resp.Content.ReadAsStringAsync();
        if (!resp.IsSuccessStatusCode)
            throw new HttpRequestException(body, null, resp.StatusCode);

        var env = JsonSerializer.Deserialize<ApiEnvelope<InventoryLocationDTO>>(body, _json);
        return env?.data;
    }

    public async Task<InventoryLocationDTO?> UpdateLocationAsync(int id, object payload, bool allowNulls = false)
    {
        using var http = await NewAuthClientAsync();
        var options = allowNulls ? _writeJsonAllowNulls : _writeJson;
        var json = JsonSerializer.Serialize(payload, options);
        using var content = new StringContent(json, Encoding.UTF8, "application/json");
        var resp = await http.PatchAsync($"/api/inventory/locations/{id}", content);
        var body = await resp.Content.ReadAsStringAsync();
        if (!resp.IsSuccessStatusCode)
            throw new HttpRequestException(body, null, resp.StatusCode);

        var env = JsonSerializer.Deserialize<ApiEnvelope<InventoryLocationDTO>>(body, _json);
        return env?.data;
    }

    public async Task DeleteLocationAsync(int id)
    {
        using var http = await NewAuthClientAsync();
        var resp = await http.DeleteAsync($"/api/inventory/locations/{id}");
        var body = await resp.Content.ReadAsStringAsync();
        if (!resp.IsSuccessStatusCode)
            throw new HttpRequestException(body, null, resp.StatusCode);
    }

    static string BuildItemsQuery(string? search, int? locationId, int? categoryId, int? itemId, int? productId, int? variantId)
    {
        var parts = new List<string>();
        if (itemId.HasValue)
            parts.Add($"id={itemId.Value}");
        if (productId.HasValue)
            parts.Add($"productId={productId.Value}");
        if (variantId.HasValue)
            parts.Add($"variantId={variantId.Value}");
        if (!string.IsNullOrWhiteSpace(search))
            parts.Add($"search={Uri.EscapeDataString(search)}");
        if (locationId.HasValue)
            parts.Add($"locationId={locationId.Value}");
        if (categoryId.HasValue)
            parts.Add($"categoryId={categoryId.Value}");
        if (parts.Count == 0) return string.Empty;
        return "?" + string.Join("&", parts);
    }

    static string BuildMovementsQuery(int? productId, int? variantId, int? locationId, int? orderId, string? type, int? limit)
    {
        var parts = new List<string>();
        if (productId.HasValue)
            parts.Add($"productId={productId.Value}");
        if (variantId.HasValue)
            parts.Add($"variantId={variantId.Value}");
        if (locationId.HasValue)
            parts.Add($"locationId={locationId.Value}");
        if (orderId.HasValue)
            parts.Add($"orderId={orderId.Value}");
        if (!string.IsNullOrWhiteSpace(type))
            parts.Add($"type={Uri.EscapeDataString(type)}");
        if (limit.HasValue)
            parts.Add($"limit={limit.Value}");

        return parts.Count == 0 ? string.Empty : "?" + string.Join("&", parts);
    }
}
