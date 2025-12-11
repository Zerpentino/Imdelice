using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Imdeliceapp.Models;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;

namespace Imdeliceapp.Services;

public class MenusApi
{
    readonly JsonSerializerOptions _json = new() { PropertyNameCaseInsensitive = true };
    static readonly JsonSerializerOptions _writeJson = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

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
        cli.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return cli;
    }

    record ApiEnvelope<T>(T? data, object? error, string? message);

    #region Menu summaries
    public record MenuSummaryDto
    {
        public int id { get; init; }
        public string name { get; init; } = string.Empty;
        public bool isActive { get; init; }
        public bool? isPublished { get; init; }
        public DateTime? publishedAt { get; init; }
    }

    public record MenuPublicDto
    {
        public int id { get; init; }
        public string name { get; init; } = string.Empty;
        public bool isActive { get; init; }
        public DateTime? publishedAt { get; init; }
        public List<MenuPublicSectionDto> sections { get; init; } = new();
    }

    public record MenuPublicSectionDto
    {
        public int id { get; init; }
        public int menuId { get; init; }
        public string name { get; init; } = string.Empty;
        public int position { get; init; }
        public bool isActive { get; init; }
        public int? categoryId { get; init; }
        public List<MenuPublicItemDto> items { get; init; } = new();
    }

    public record MenuPublicItemDto
    {
        public int id { get; init; }
        public int sectionId { get; init; }
        public string refType { get; init; } = string.Empty;
        public int refId { get; init; }
        public string? displayName { get; init; }
        public int? displayPriceCents { get; init; }
        public int position { get; init; }
        public bool isFeatured { get; init; }
        public bool isActive { get; init; }
        public MenuPublicReferenceDto? @ref { get; init; }
    }

    public record MenuPublicReferenceDto
    {
        public string? kind { get; init; }
        public MenuPublicVariantReference? variant { get; init; }
        public MenuPublicProductReference? product { get; init; }
        public MenuPublicOptionReference? option { get; init; }
        public List<MenuPublicComboComponent> components { get; init; } = new();
    }

    public record MenuPublicVariantReference
    {
        public int id { get; init; }
        public string? name { get; init; }
        public int? priceCents { get; init; }
        public bool isActive { get; init; }
        public bool isAvailable { get; init; }
        public MenuPublicProductReference? product { get; init; }
        public string? imageUrl { get; init; }
        public bool hasImage { get; init; }
        public List<VariantModifierGroupLinkDTO> modifierGroups { get; init; } = new();
    }

    public record MenuPublicProductReference
    {
        public int id { get; init; }
        public string name { get; init; } = string.Empty;
        public string type { get; init; } = string.Empty;
        public string? description { get; init; }
        public int? priceCents { get; init; }
        public bool isActive { get; init; }
        public bool isAvailable { get; init; }
        public string? imageUrl { get; init; }
        public bool hasImage { get; init; }
    }

    public record MenuPublicOptionReference
    {
        public int id { get; init; }
        public string? name { get; init; }
        public int? priceExtraCents { get; init; }
        public bool isActive { get; init; }
    }

    public record MenuPublicComboComponent
    {
        public int quantity { get; init; }
        public bool isRequired { get; init; }
        public string? notes { get; init; }
        public MenuPublicProductReference? product { get; init; }
        public MenuPublicVariantReference? variant { get; init; }
    }
    #endregion

    public record MenuItemDto
    {
        public int id { get; init; }
        public int sectionId { get; init; }
        public string refType { get; init; } = string.Empty;
        public int refId { get; init; }
        public string? displayName { get; init; }
        public int? displayPriceCents { get; init; }
        public int position { get; init; }
        public bool isFeatured { get; init; }
        public bool isActive { get; init; }
        public DateTime createdAt { get; init; }
        public DateTime updatedAt { get; init; }
        public DateTime? deletedAt { get; init; }
        public MenuPublicReferenceDto? @ref { get; init; }
    }

    public record MenuItemCreateDto
    {
        public int sectionId { get; init; }
        public string refType { get; init; } = string.Empty;
        public int refId { get; init; }
        public string? displayName { get; init; }
        public int? displayPriceCents { get; init; }
        public int? position { get; init; }
        public bool? isFeatured { get; init; }
        public bool? isActive { get; init; }
    }

    public record MenuItemUpdateDto
    {
        public string? displayName { get; init; }
        public int? displayPriceCents { get; init; }
        public int? position { get; init; }
        public bool? isFeatured { get; init; }
        public bool? isActive { get; init; }
    }

    public record ProductVariantDto
    {
        public int id { get; init; }
        public string? name { get; init; }
        public bool? isActive { get; init; }
        public bool? isAvailable { get; init; }
        public int? priceCents { get; init; }
        public string? imageUrl { get; init; }
        public bool? hasImage { get; init; }
        public List<VariantModifierGroupLinkDTO> modifierGroups { get; init; } = new();
    }

    public record ProductSummaryDto
    {
        public int id { get; init; }
        public string name { get; init; } = string.Empty;
        public string type { get; init; } = string.Empty;
        public bool isActive { get; init; }
        public bool? isAvailable { get; init; }
        public int? priceCents { get; init; }
        public string? imageUrl { get; init; }
        public bool? hasImage { get; init; }
        public List<ProductVariantDto> variants { get; init; } = new();
    }

    public async Task<List<CategoryDTO>> GetCategoriesAsync(bool? isActive = true)
    {
        using var http = await NewAuthClientAsync();
        var url = "/api/categories" + (isActive.HasValue ? $"?isActive={isActive.Value.ToString().ToLower()}" : string.Empty);
        var resp = await http.GetAsync(url);
        var body = await resp.Content.ReadAsStringAsync();
        if (!resp.IsSuccessStatusCode) throw new HttpRequestException(body);

        var env = JsonSerializer.Deserialize<ApiEnvelope<List<CategoryDTO>>>(body, _json);
        return env?.data ?? new();
    }

    public async Task<List<MenuSummaryDto>> GetMenusAsync()
    {
        using var http = await NewAuthClientAsync();
        var resp = await http.GetAsync("/api/menus");
        var body = await resp.Content.ReadAsStringAsync();
        if (!resp.IsSuccessStatusCode) throw new HttpRequestException(body);

        var env = JsonSerializer.Deserialize<ApiEnvelope<List<MenuSummaryDto>>>(body, _json);
        return env?.data ?? new();
    }

    public async Task<List<VariantModifierGroupLinkDTO>> GetVariantModifierGroupsAsync(int variantId)
    {
        using var http = await NewAuthClientAsync();
        var resp = await http.GetAsync($"/api/products/variants/{variantId}/modifier-groups");
        var body = await resp.Content.ReadAsStringAsync();
        if (!resp.IsSuccessStatusCode) throw new HttpRequestException(body);

        var env = JsonSerializer.Deserialize<ApiEnvelope<List<VariantModifierGroupLinkDTO>>>(body, _json);
        return env?.data ?? new();
    }

    public async Task<MenuPublicDto?> GetMenuPublicAsync(int menuId)
    {
        using var http = await NewAuthClientAsync();
        var resp = await http.GetAsync($"/api/menus/{menuId}/public");
        var body = await resp.Content.ReadAsStringAsync();
        if (!resp.IsSuccessStatusCode) throw new HttpRequestException(body);

        var env = JsonSerializer.Deserialize<ApiEnvelope<MenuPublicDto>>(body, _json);
        return env?.data;
    }

    public async Task<List<MenuItemDto>> GetSectionItemsAsync(int sectionId, CancellationToken ct = default)
    {
        using var http = await NewAuthClientAsync();
        var resp = await http.GetAsync($"/api/menus/sections/{sectionId}/items", ct);
        var body = await resp.Content.ReadAsStringAsync();
        if (!resp.IsSuccessStatusCode) throw new HttpRequestException(body);

        var env = JsonSerializer.Deserialize<ApiEnvelope<List<MenuItemDto>>>(body, _json);
        return env?.data ?? new();
    }

    public async Task<List<MenuItemDto>> GetSectionItemsTrashAsync(int sectionId, CancellationToken ct = default)
    {
        using var http = await NewAuthClientAsync();
        var resp = await http.GetAsync($"/api/menus/sections/{sectionId}/items/trash", ct);
        var body = await resp.Content.ReadAsStringAsync();
        if (!resp.IsSuccessStatusCode) throw new HttpRequestException(body);

        var env = JsonSerializer.Deserialize<ApiEnvelope<List<MenuItemDto>>>(body, _json);
        return env?.data ?? new();
    }

    public async Task<MenuItemDto?> CreateMenuItemAsync(MenuItemCreateDto dto)
    {
        using var http = await NewAuthClientAsync();
        var payload = new StringContent(JsonSerializer.Serialize(dto, _writeJson), Encoding.UTF8, "application/json");
        var resp = await http.PostAsync("/api/menus/items", payload);
        var body = await resp.Content.ReadAsStringAsync();
        if (!resp.IsSuccessStatusCode) throw new HttpRequestException(body);

        var env = JsonSerializer.Deserialize<ApiEnvelope<MenuItemDto>>(body, _json);
        return env?.data;
    }

    public async Task<MenuItemDto?> UpdateMenuItemAsync(int id, MenuItemUpdateDto dto)
    {
        using var http = await NewAuthClientAsync();
        var payload = new StringContent(JsonSerializer.Serialize(dto, _writeJson), Encoding.UTF8, "application/json");
        var resp = await http.PatchAsync($"/api/menus/items/{id}", payload);
        var body = await resp.Content.ReadAsStringAsync();
        if (!resp.IsSuccessStatusCode) throw new HttpRequestException(body);

        var env = JsonSerializer.Deserialize<ApiEnvelope<MenuItemDto>>(body, _json);
        return env?.data;
    }

    public async Task ArchiveMenuItemAsync(int id)
    {
        using var http = await NewAuthClientAsync();
        var resp = await http.DeleteAsync($"/api/menus/items/{id}");
        if (!resp.IsSuccessStatusCode)
        {
            var body = await resp.Content.ReadAsStringAsync();
            throw new HttpRequestException(body);
        }
    }

    public async Task DeleteMenuItemHardAsync(int id)
    {
        using var http = await NewAuthClientAsync();
        var resp = await http.DeleteAsync($"/api/menus/items/{id}/hard");
        if (!resp.IsSuccessStatusCode)
        {
            var body = await resp.Content.ReadAsStringAsync();
            throw new HttpRequestException(body);
        }
    }

    public async Task RestoreMenuItemAsync(int id)
    {
        using var http = await NewAuthClientAsync();
        var resp = await http.PatchAsync($"/api/menus/items/{id}/restore", new StringContent(string.Empty, Encoding.UTF8, "application/json"));
        if (!resp.IsSuccessStatusCode && resp.StatusCode != HttpStatusCode.NoContent)
        {
            var body = await resp.Content.ReadAsStringAsync();
            throw new HttpRequestException(body);
        }
    }

    public async Task<ProductSummaryDto?> GetProductAsync(int productId)
    {
        using var http = await NewAuthClientAsync();
        var resp = await http.GetAsync($"/api/products/{productId}");
        var body = await resp.Content.ReadAsStringAsync();
        if (!resp.IsSuccessStatusCode) throw new HttpRequestException(body);

        var env = JsonSerializer.Deserialize<ApiEnvelope<ProductSummaryDto>>(body, _json);
        return env?.data;
    }
}
