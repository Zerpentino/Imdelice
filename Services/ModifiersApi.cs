using System.Net.Http.Headers;
using System.Text.Json;
using System.Text;
using System.Net.Http;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Maui.Storage;
using Microsoft.Maui.Controls; // para Application.Current.Resources
using Imdeliceapp.Models; // <— AQUI
using System.Text.Json.Serialization;


namespace Imdeliceapp.Services;

public class ModifiersApi
{
    readonly JsonSerializerOptions _json = new() { PropertyNameCaseInsensitive = true };
    // NUEVO: para ESCRIBIR ignorando nulls
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

    // ModifiersApi.cs
    static async Task<HttpClient> NewAuthClientAsync()
    {
        var baseUrl = Application.Current.Resources["urlbase"].ToString().TrimEnd('/');
        var token = await GetTokenAsync() ?? "";
        var cli = new HttpClient { BaseAddress = new Uri(baseUrl), Timeout = TimeSpan.FromSeconds(20) };
        cli.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return cli;
    }


    // LISTAR grupos (filtros opcionales)
    public async Task<List<ModifierGroupDTO>> GetGroupsAsync(bool? isActive = null, int? categoryId = null, string? categorySlug = null, string? search = null)
    {
        using var http = await NewAuthClientAsync();
        var q = new List<string>();
        if (isActive.HasValue) q.Add($"isActive={isActive.Value.ToString().ToLower()}");
        if (categoryId.HasValue) q.Add($"categoryId={categoryId.Value}");
        if (!string.IsNullOrWhiteSpace(categorySlug)) q.Add($"categorySlug={Uri.EscapeDataString(categorySlug)}");
        if (!string.IsNullOrWhiteSpace(search)) q.Add($"search={Uri.EscapeDataString(search)}");

        var url = "/api/modifiers/groups" + (q.Count > 0 ? "?" + string.Join("&", q) : "");
        var resp = await http.GetAsync(url);
        var body = await resp.Content.ReadAsStringAsync();
        if (!resp.IsSuccessStatusCode) throw new HttpRequestException(body);

        var env = JsonSerializer.Deserialize<ApiEnvelopeMods<List<ModifierGroupDTO>>>(body, _json);
        return env?.data ?? new();
    }

    // OBTENER grupo por id
    public async Task<ModifierGroupDTO?> GetGroupAsync(int id)
    {
        using var http = await NewAuthClientAsync();
        var resp = await http.GetAsync($"/api/modifiers/groups/{id}");
        var body = await resp.Content.ReadAsStringAsync();
        if (!resp.IsSuccessStatusCode) throw new HttpRequestException(body);

        var env = JsonSerializer.Deserialize<ApiEnvelopeMods<ModifierGroupDTO>>(body, _json);
        return env?.data;
    }

    // CREAR grupo con opciones
    public class CreateGroupDto
    {
        public string name { get; set; } = "";
        public string? description { get; set; }
        public int minSelect { get; set; }
        public int? maxSelect { get; set; }
        public bool isRequired { get; set; }
        public List<ModifierOptionDTO> options { get; set; } = new();
        public int? appliesToCategoryId { get; set; }
        public bool isActive { get; set; } = true;
        public int position { get; set; } = 0;
    }

    public async Task<int> CreateGroupAsync(CreateGroupDto dto)
    {
        using var http = await NewAuthClientAsync();
        var json = JsonSerializer.Serialize(dto, _writeJson);   // 👈 usar _writeJson
        var resp = await http.PostAsync("/api/modifiers/groups",
            new StringContent(json, System.Text.Encoding.UTF8, "application/json"));
        var body = await resp.Content.ReadAsStringAsync();
        if (!resp.IsSuccessStatusCode) throw new HttpRequestException(body);

        var env = JsonSerializer.Deserialize<ApiEnvelopeMods<ModifierGroupDTO>>(body, _json);
        return env?.data?.id ?? 0;
    }

    // ACTUALIZAR grupo (puedes reemplazar TODAS las opciones)
    public class UpdateGroupDto
    {
        public string? description { get; set; }
        public string? name { get; set; }
        public int? minSelect { get; set; }
        public int? maxSelect { get; set; }
        public bool? isRequired { get; set; }
        public bool? isActive { get; set; }
        public int? position { get; set; }
        public int? appliesToCategoryId { get; set; }
        public List<ModifierOptionDTO>? replaceOptions { get; set; }
    }

    public async Task UpdateGroupAsync(int id, UpdateGroupDto dto)
    {
        using var http = await NewAuthClientAsync();
        var json = JsonSerializer.Serialize(dto, _writeJson);   // 👈 usar _writeJson
        var req = new HttpRequestMessage(new HttpMethod("PATCH"), $"/api/modifiers/groups/{id}")
        {
            Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json"),
        };
        var resp = await http.SendAsync(req);
        var body = await resp.Content.ReadAsStringAsync();
        if (!resp.IsSuccessStatusCode) throw new HttpRequestException(body);
    }
    // En ModifiersApi
    // En ModifiersApi.cs
// ModifiersApi.cs
public async Task ReplaceGroupOptionsAsync(int groupId, List<ModifierOptionDTO> options)
{
    using var http = await NewAuthClientAsync();

    // 👇 IMPORTANTE: no mandar el array directo; encapsular en un objeto
    var payload = new { replaceOptions = options };
    var json = JsonSerializer.Serialize(payload, _writeJson);

    var req = new HttpRequestMessage(HttpMethod.Patch, $"/api/modifiers/groups/{groupId}")
    {
        Content = new StringContent(json, Encoding.UTF8, "application/json")
    };

    var resp = await http.SendAsync(req);
    var body = await resp.Content.ReadAsStringAsync();
    if (!resp.IsSuccessStatusCode)
        throw new HttpRequestException(body);
}


public async Task SetOptionActiveAsync(int optionId, bool isActive)
{
    using var http = await NewAuthClientAsync();

    var json = JsonSerializer.Serialize(new { isActive });
    var req = new HttpRequestMessage(HttpMethod.Patch, $"/api/modifiers/modifier-options/{optionId}")
    {
        Content = new StringContent(json, Encoding.UTF8, "application/json")
    };

    var resp = await http.SendAsync(req);
    var body = await resp.Content.ReadAsStringAsync();
    if (!resp.IsSuccessStatusCode)
        throw new HttpRequestException(body);
}




    // ELIMINAR grupo (hard = true para borrado duro)
    public async Task DeleteGroupAsync(int id, bool hard = false)
    {
        using var http = await NewAuthClientAsync();
        var url = $"/api/modifiers/groups/{id}" + (hard ? "?hard=true" : "");
        var resp = await http.DeleteAsync(url);
        var body = await resp.Content.ReadAsStringAsync();
        if (!resp.IsSuccessStatusCode) throw new HttpRequestException(body);
    }

    // ADJUNTAR grupo a producto
    public class AttachDto { public int productId { get; set; } public int groupId { get; set; } public int position { get; set; } }
    public async Task AttachGroupToProductAsync(int productId, int groupId, int position)
    {
        using var http = await NewAuthClientAsync();
        var json = JsonSerializer.Serialize(new AttachDto
        {
            productId = productId,
            groupId = groupId,
            position = position
        });

        var resp = await http.PostAsync("/api/products/attach-modifier",
            new StringContent(json, System.Text.Encoding.UTF8, "application/json"));
        var body = await resp.Content.ReadAsStringAsync();
        if (!resp.IsSuccessStatusCode) throw new HttpRequestException(body);
    }

    // LISTAR grupos por producto (ya lo usas en ProductModifiersPage)
    public async Task<List<ProductGroupLinkDTO>> GetGroupsByProductAsync(int productId)
    {
        using var http = await NewAuthClientAsync();
        var resp = await http.GetAsync($"/api/modifiers/groups/by-product/{productId}");
        var body = await resp.Content.ReadAsStringAsync();
        if (!resp.IsSuccessStatusCode) throw new HttpRequestException(body);

        var env = JsonSerializer.Deserialize<ApiEnvelopeMods<List<ProductGroupLinkDTO>>>(body, _json);
        return env?.data ?? new();
    }
    //     public class CategoryDTO
    // {
    //     public int id { get; set; }
    //     public string? name { get; set; }
    //     public string? slug { get; set; }
    //     public bool isActive { get; set; }
    // }

    // Lista de categorías (ajusta la ruta si tu backend usa otra)
    public async Task<List<CategoryDTO>> GetCategoriesAsync(bool? isActive = true)
    {
        using var http = await NewAuthClientAsync();
        var url = "/api/categories" + (isActive.HasValue ? $"?isActive={isActive.Value.ToString().ToLower()}" : "");
        var resp = await http.GetAsync(url);
        var body = await resp.Content.ReadAsStringAsync();
        if (!resp.IsSuccessStatusCode) throw new HttpRequestException(body);

        var env = JsonSerializer.Deserialize<ApiEnvelopeMods<List<CategoryDTO>>>(body, _json);
        return env?.data ?? new();
    }

   

    // ---------------- LISTAR PRODUCTOS POR GRUPO ----------------
    public async Task<List<GroupProductLinkDTO>> GetProductsByGroupAsync(
        int groupId,
        bool? isActive = null,
        string? search = null,
        int limit = 50,
        int offset = 0)
    {
        using var http = await NewAuthClientAsync();
        var q = new List<string>();
        if (isActive.HasValue) q.Add($"isActive={(isActive.Value ? "true" : "false")}");
        if (!string.IsNullOrWhiteSpace(search)) q.Add($"search={Uri.EscapeDataString(search)}");
        if (limit > 0) q.Add($"limit={limit}");
        if (offset > 0) q.Add($"offset={offset}");
        var url = $"/api/modifiers/groups/{groupId}/products" + (q.Count > 0 ? "?" + string.Join("&", q) : "");

        var resp = await http.GetAsync(url);
        var body = await resp.Content.ReadAsStringAsync();
        if (!resp.IsSuccessStatusCode) throw new HttpRequestException(body);

        var env = JsonSerializer.Deserialize<ApiEnvelopeMods<List<GroupProductLinkDTO>>>(body, _json);
        return env?.data ?? new();
    }

    // ---------------- DESVINCULAR ----------------
    public class DetachByLinkDto { public int linkId { get; set; } }
    public class DetachByPairDto { public int productId { get; set; } public int groupId { get; set; } }

    public async Task DetachGroupFromProductByLinkAsync(int linkId)
{
    using var http = await NewAuthClientAsync();
    var json = JsonSerializer.Serialize(new DetachByLinkDto { linkId = linkId }); // 👈
    var resp = await http.PostAsync("/api/products/detach-modifier",
        new StringContent(json, Encoding.UTF8, "application/json"));
    var body = await resp.Content.ReadAsStringAsync();
    if (!resp.IsSuccessStatusCode) throw new HttpRequestException(body);
}

    public async Task DetachGroupFromProductAsync(int productId, int groupId)
{
    using var http = await NewAuthClientAsync();
    var json = JsonSerializer.Serialize(new DetachByPairDto { productId = productId, groupId = groupId }); // 👈
    var resp = await http.PostAsync("/api/products/detach-modifier",
        new StringContent(json, Encoding.UTF8, "application/json"));
    var body = await resp.Content.ReadAsStringAsync();
    if (!resp.IsSuccessStatusCode) throw new HttpRequestException(body);
}

    // ---------------- CAMBIAR POSICIÓN (1) ----------------
    public class UpdateLinkPositionDto { public int linkId { get; set; } public int position { get; set; } }
   public async Task UpdateLinkPositionAsync(int linkId, int position)
{
    using var http = await NewAuthClientAsync();
    var json = JsonSerializer.Serialize(new UpdateLinkPositionDto { linkId = linkId, position = position }); // 👈
    var resp = await http.PostAsync("/api/products/modifier-position",
        new StringContent(json, Encoding.UTF8, "application/json"));
    var body = await resp.Content.ReadAsStringAsync();
    if (!resp.IsSuccessStatusCode) throw new HttpRequestException(body);
}

    // ---------------- REORDENAR VARIOS (opcional) ----------------
    public class ReorderItem { public int linkId { get; set; } public int position { get; set; } }
    public class BulkReorderDto { public int productId { get; set; } public List<ReorderItem> items { get; set; } = new(); }

    public async Task BulkReorderAsync(int productId, List<ReorderItem> items)
    {
        using var http = await NewAuthClientAsync();
        var json = JsonSerializer.Serialize(new BulkReorderDto { productId = productId, items = items });
        var resp = await http.PostAsync("/api/products/modifier-reorder",
            new StringContent(json, Encoding.UTF8, "application/json"));
        var body = await resp.Content.ReadAsStringAsync();
        if (!resp.IsSuccessStatusCode) throw new HttpRequestException(body);
    }

    // ---------------- VARIANT OVERRIDES ----------------
    public async Task<List<VariantModifierGroupLinkDTO>> GetVariantModifierGroupsAsync(int variantId)
    {
        using var http = await NewAuthClientAsync();
        var resp = await http.GetAsync($"/api/products/variants/{variantId}/modifier-groups");
        var body = await resp.Content.ReadAsStringAsync();
        if (!resp.IsSuccessStatusCode) throw new HttpRequestException(body);

        var env = JsonSerializer.Deserialize<ApiEnvelopeMods<List<VariantModifierGroupLinkDTO>>>(body, _json);
        return env?.data ?? new();
    }

    public class AttachVariantModifierGroupDto
    {
        public int groupId { get; set; }
        public int minSelect { get; set; }
        public int? maxSelect { get; set; }
        public bool isRequired { get; set; }
    }

    public async Task AttachGroupToVariantAsync(int variantId, AttachVariantModifierGroupDto dto)
    {
        using var http = await NewAuthClientAsync();
        var json = JsonSerializer.Serialize(dto, _writeJson);
        var resp = await http.PostAsync($"/api/products/variants/{variantId}/modifier-groups",
            new StringContent(json, Encoding.UTF8, "application/json"));
        var body = await resp.Content.ReadAsStringAsync();
        if (!resp.IsSuccessStatusCode) throw new HttpRequestException(body);
    }

    public class UpdateVariantModifierGroupDto
    {
        public int? minSelect { get; set; }
        public int? maxSelect { get; set; }
        public bool? isRequired { get; set; }
    }

    public async Task UpdateVariantModifierGroupAsync(int variantId, int groupId, UpdateVariantModifierGroupDto dto)
    {
        using var http = await NewAuthClientAsync();
        var json = JsonSerializer.Serialize(dto, _writeJson);
        var req = new HttpRequestMessage(HttpMethod.Patch, $"/api/products/variants/{variantId}/modifier-groups/{groupId}")
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
        var resp = await http.SendAsync(req);
        var body = await resp.Content.ReadAsStringAsync();
        if (!resp.IsSuccessStatusCode) throw new HttpRequestException(body);
    }

    public async Task DeleteVariantModifierGroupAsync(int variantId, int groupId)
    {
        using var http = await NewAuthClientAsync();
        var resp = await http.DeleteAsync($"/api/products/variants/{variantId}/modifier-groups/{groupId}");
        var body = await resp.Content.ReadAsStringAsync();
        if (!resp.IsSuccessStatusCode) throw new HttpRequestException(body);
    }

public class SimpleProductDTO
{
    public int id { get; set; }
    public string? name { get; set; }
    public string? type { get; set; }        // "SIMPLE" | "VARIANTED" | "COMBO"
    public int? priceCents { get; set; }     // null si VARIANTED
    public bool isActive { get; set; }
    public int categoryId { get; set; }
}

// GET /api/products?isActive=&categoryId=&search=&limit=&offset=
public async Task<List<SimpleProductDTO>> GetProductsAsync(
    bool? isActive = null,
    int? categoryId = null,
    string? search = null,
    int limit = 50,
    int offset = 0)
{
    using var http = await NewAuthClientAsync();
    var q = new List<string>();
    if (isActive.HasValue) q.Add($"isActive={(isActive.Value ? "true" : "false")}");
    if (categoryId.HasValue && categoryId.Value > 0) q.Add($"categoryId={categoryId.Value}");
    if (!string.IsNullOrWhiteSpace(search)) q.Add($"search={Uri.EscapeDataString(search)}");
    if (limit > 0) q.Add($"limit={limit}");
    if (offset > 0) q.Add($"offset={offset}");

    var url = "/api/products" + (q.Count > 0 ? "?" + string.Join("&", q) : "");
    var resp = await http.GetAsync(url);
    var body = await resp.Content.ReadAsStringAsync();
    if (!resp.IsSuccessStatusCode) throw new HttpRequestException(body);

    var env = JsonSerializer.Deserialize<ApiEnvelopeMods<List<SimpleProductDTO>>>(body, _json);
    return env?.data ?? new();
}


}
