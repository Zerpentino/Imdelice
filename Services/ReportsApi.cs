using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;

namespace Imdeliceapp.Services;

public class ReportsApi
{
    readonly JsonSerializerOptions _json = new()
    {
        PropertyNameCaseInsensitive = true,
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

    public class ProfitLossReportDTO
    {
        public PaymentsReportDTO payments { get; set; } = new();
        public ExpensesSummaryDTO expenses { get; set; } = new();
        public int netAmountCents { get; set; }
    }

    public class PaymentsReportDTO
    {
        public List<PaymentSummaryDTO> totalsByMethod { get; set; } = new();
        public GrandTotals grandTotals { get; set; } = new();
        public List<OrderEntryDTO> orders { get; set; } = new();
    }

    public class GrandTotals
    {
        public int amountCents { get; set; }
    }

    public class PaymentSummaryDTO
    {
        public string method { get; set; } = string.Empty;
        public int amountCents { get; set; }
        public int paymentsCount { get; set; }
    }

    public class OrderEntryDTO
    {
        public int orderId { get; set; }
        public string? code { get; set; }
        public string? status { get; set; }
        public string? serviceType { get; set; }
        public string? source { get; set; }
        public DateTime? openedAt { get; set; }
        public DateTime? closedAt { get; set; }
        public int totalCents { get; set; }
        public List<PaymentSummaryDTO> payments { get; set; } = new();
    }

    public class ExpensesSummaryDTO
    {
        public int totalAmountCents { get; set; }
        public List<CategoryTotalDTO> totalsByCategory { get; set; } = new();
    }

    public class CategoryTotalDTO
    {
        public ExpensesApi.ExpenseCategory category { get; set; }
        public int amountCents { get; set; }
    }

    public async Task<ProfitLossReportDTO> GetProfitLossAsync(DateTime from, DateTime to, int tzOffsetMinutes)
    {
        using var http = await NewAuthClientAsync();
        var query = $"?from={from:yyyy-MM-dd}&to={to:yyyy-MM-dd}&tzOffsetMinutes={tzOffsetMinutes}";
        var resp = await http.GetAsync("/api/reports/profit-loss" + query);
        var body = await resp.Content.ReadAsStringAsync();
        if (!resp.IsSuccessStatusCode)
            throw new HttpRequestException(body, null, resp.StatusCode);

        var env = JsonSerializer.Deserialize<ApiEnvelope<ProfitLossReportDTO>>(body, _json);
        if (env?.data == null)
            throw new HttpRequestException("Respuesta inválida del servidor.");
        return env.data;
    }
}
