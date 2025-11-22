using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;

namespace Imdeliceapp.Services;

public class ExpensesApi
{
    readonly JsonSerializerOptions _json = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };
    static readonly JsonSerializerOptions _writeJson = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter() }
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

    public enum ExpenseCategory
    {
        SUPPLIES,
        MAINTENANCE,
        LOSS,
        OTHER
    }

    public enum PaymentMethod
    {
        CASH,
        CARD,
        TRANSFER,
        OTHER
    }

    public enum InventoryMovementType
    {
        PURCHASE,
        SALE,
        ADJUSTMENT,
        WASTE,
        TRANSFER,
        SALE_RETURN
    }

    public class ExpenseDTO
    {
        public int id { get; set; }
        public string concept { get; set; } = string.Empty;
        public ExpenseCategory category { get; set; }
        public int amountCents { get; set; }
        public PaymentMethod? paymentMethod { get; set; }
        public string? notes { get; set; }
        public DateTime recordedAt { get; set; }
        public DateTime incurredAt { get; set; }
        public int recordedByUserId { get; set; }
        public int? inventoryMovementId { get; set; }
    }

    public class ExpenseSummaryDTO
    {
        public int totalAmountCents { get; set; }
        public List<CategoryTotalDTO> totalsByCategory { get; set; } = new();
    }

    public class CategoryTotalDTO
    {
        public ExpenseCategory category { get; set; }
        public int amountCents { get; set; }
    }

    public class CreateExpenseRequest
    {
        public string concept { get; set; } = string.Empty;
        public ExpenseCategory category { get; set; }
        public int amountCents { get; set; }
        public PaymentMethod? paymentMethod { get; set; }
        public string? notes { get; set; }
        public string? incurredAt { get; set; }
        public InventoryMovementPayload? inventoryMovement { get; set; }
    }

    public class InventoryMovementPayload
    {
        public int? productId { get; set; }
        public int? variantId { get; set; }
        public int? locationId { get; set; }
        public string? barcode { get; set; }
        public InventoryMovementType type { get; set; } = InventoryMovementType.PURCHASE;
        public double quantity { get; set; }
        public string? reason { get; set; }
    }

    public async Task<List<ExpenseDTO>> ListAsync(DateTime? from = null, DateTime? to = null, ExpenseCategory? category = null, string? search = null, int? tzOffsetMinutes = null)
    {
        using var http = await NewAuthClientAsync();
        var query = BuildQuery(from, to, category, search, tzOffsetMinutes);
        var resp = await http.GetAsync("/api/expenses" + query);
        var body = await resp.Content.ReadAsStringAsync();
        if (!resp.IsSuccessStatusCode)
            throw new HttpRequestException(body, null, resp.StatusCode);

        var env = JsonSerializer.Deserialize<ApiEnvelope<List<ExpenseDTO>>>(body, _json);
        return env?.data ?? new();
    }

    public async Task<ExpenseDTO> CreateAsync(CreateExpenseRequest payload)
    {
        using var http = await NewAuthClientAsync();
        using var content = ToJsonContent(payload);
        var resp = await http.PostAsync("/api/expenses", content);
        var body = await resp.Content.ReadAsStringAsync();
        if (!resp.IsSuccessStatusCode)
            throw new HttpRequestException(body, null, resp.StatusCode);

        var env = JsonSerializer.Deserialize<ApiEnvelope<ExpenseDTO>>(body, _json);
        if (env?.data == null)
            throw new HttpRequestException("Respuesta inválida del servidor.");
        return env.data;
    }

    public async Task<ExpenseSummaryDTO> GetSummaryAsync(DateTime? from = null, DateTime? to = null, ExpenseCategory? category = null, int? tzOffsetMinutes = null)
    {
        using var http = await NewAuthClientAsync();
        var query = BuildQuery(from, to, category, null, tzOffsetMinutes);
        var resp = await http.GetAsync("/api/expenses/summary" + query);
        var body = await resp.Content.ReadAsStringAsync();
        if (!resp.IsSuccessStatusCode)
            throw new HttpRequestException(body, null, resp.StatusCode);

        var env = JsonSerializer.Deserialize<ApiEnvelope<ExpenseSummaryDTO>>(body, _json);
        return env?.data ?? new ExpenseSummaryDTO();
    }

    static string BuildQuery(DateTime? from, DateTime? to, ExpenseCategory? category, string? search, int? tzOffsetMinutes)
    {
        var parts = new List<string>();
        if (from.HasValue)
            parts.Add("from=" + Uri.EscapeDataString(from.Value.ToString("yyyy-MM-dd")));
        if (to.HasValue)
            parts.Add("to=" + Uri.EscapeDataString(to.Value.ToString("yyyy-MM-dd")));
        if (category.HasValue)
            parts.Add("category=" + category.Value);
        if (!string.IsNullOrWhiteSpace(search))
            parts.Add("search=" + Uri.EscapeDataString(search));
        if (tzOffsetMinutes.HasValue)
            parts.Add("tzOffsetMinutes=" + tzOffsetMinutes.Value);

        return parts.Count > 0 ? "?" + string.Join("&", parts) : string.Empty;
    }
}
