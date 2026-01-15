using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Imdeliceapp.Models;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;

namespace Imdeliceapp.Services;

public class OrdersApi
{
    readonly JsonSerializerOptions _json = new() { PropertyNameCaseInsensitive = true };
    static readonly JsonSerializerOptions _writeJson = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public static int GetLocalTimezoneOffsetMinutes()
        => (int)Math.Round(TimeZoneInfo.Local.GetUtcOffset(DateTime.Now).TotalMinutes);

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

    #region Helpers

    static string BuildQuery(OrderListQuery? q)
    {
        if (q is null) return string.Empty;

        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(q.statuses))
            parts.Add($"statuses={Uri.EscapeDataString(q.statuses)}");
        if (!string.IsNullOrWhiteSpace(q.serviceType))
            parts.Add($"serviceType={Uri.EscapeDataString(q.serviceType)}");
        if (!string.IsNullOrWhiteSpace(q.source))
            parts.Add($"source={Uri.EscapeDataString(q.source)}");
        if (q.from.HasValue)
            parts.Add($"from={Uri.EscapeDataString(q.from.Value.ToString("yyyy-MM-dd"))}");
        if (q.to.HasValue)
            parts.Add($"to={Uri.EscapeDataString(q.to.Value.ToString("yyyy-MM-dd"))}");
        if (q.tableId.HasValue)
            parts.Add($"tableId={q.tableId.Value}");
        if (q.tzOffsetMinutes.HasValue)
            parts.Add($"tzOffsetMinutes={q.tzOffsetMinutes.Value}");

        if (parts.Count == 0) return string.Empty;
        return "?" + string.Join("&", parts);
    }

    static string BuildKdsQuery(KdsQuery? q)
    {
        if (q is null) return string.Empty;

        var parts = new List<string>();
        if (q.Statuses != null && q.Statuses.Count > 0)
            parts.Add($"statuses={Uri.EscapeDataString(string.Join(",", q.Statuses))}");
        if (!string.IsNullOrWhiteSpace(q.ServiceType))
            parts.Add($"serviceType={Uri.EscapeDataString(q.ServiceType)}");
        if (!string.IsNullOrWhiteSpace(q.Source))
            parts.Add($"source={Uri.EscapeDataString(q.Source)}");
        if (q.From.HasValue)
            parts.Add($"from={Uri.EscapeDataString(q.From.Value.ToString("o"))}");
        if (q.To.HasValue)
            parts.Add($"to={Uri.EscapeDataString(q.To.Value.ToString("o"))}");
        if (q.TzOffsetMinutes.HasValue)
            parts.Add($"tzOffsetMinutes={q.TzOffsetMinutes.Value}");

        return parts.Count == 0 ? string.Empty : "?" + string.Join("&", parts);
    }

    static string BuildPaymentsReportQuery(DateTime? from, DateTime? to, bool includeOrders, int? tzOffsetMinutes)
    {
        var parts = new List<string>();
        if (from.HasValue)
            parts.Add($"from={Uri.EscapeDataString(from.Value.ToString("yyyy-MM-dd"))}");
        if (to.HasValue)
            parts.Add($"to={Uri.EscapeDataString(to.Value.ToString("yyyy-MM-dd"))}");
        if (tzOffsetMinutes.HasValue)
            parts.Add($"tzOffsetMinutes={tzOffsetMinutes.Value}");
        if (includeOrders)
            parts.Add("includeOrders=true");
        return parts.Count == 0 ? string.Empty : "?" + string.Join("&", parts);
    }

    static StringContent ToJsonContent<T>(T payload)
    {
        var json = JsonSerializer.Serialize(payload, _writeJson);
        return new StringContent(json, Encoding.UTF8, "application/json");
    }

    static async Task EnsureSuccessAsync(HttpResponseMessage resp)
    {
        if (resp.IsSuccessStatusCode) return;
        var body = await resp.Content.ReadAsStringAsync();
        throw new HttpRequestException(body, null, resp.StatusCode);
    }

    #endregion

    public async Task<List<OrderSummaryDTO>> ListAsync(OrderListQuery? filters = null)
    {
        using var http = await NewAuthClientAsync();
        var resp = await http.GetAsync("/api/orders" + BuildQuery(filters));
        var body = await resp.Content.ReadAsStringAsync();
        if (!resp.IsSuccessStatusCode)
            throw new HttpRequestException(body, null, resp.StatusCode);

        var env = JsonSerializer.Deserialize<ApiEnvelope<List<OrderSummaryDTO>>>(body, _json);
        return env?.data ?? new();
    }

    public async Task<OrderDetailDTO?> GetAsync(int orderId)
    {
        using var http = await NewAuthClientAsync();
        var resp = await http.GetAsync($"/api/orders/{orderId}");
        var body = await resp.Content.ReadAsStringAsync();
        if (!resp.IsSuccessStatusCode)
            throw new HttpRequestException(body, null, resp.StatusCode);

        var env = JsonSerializer.Deserialize<ApiEnvelope<OrderDetailDTO>>(body, _json);
        return env?.data;
    }

    public async Task<OrderDetailDTO?> CreateAsync(CreateOrderDTO dto)
    {
        using var http = await NewAuthClientAsync();
        using var content = ToJsonContent(dto);
        var resp = await http.PostAsync("/api/orders", content);
        var body = await resp.Content.ReadAsStringAsync();
        if (!resp.IsSuccessStatusCode)
            throw new HttpRequestException(body, null, resp.StatusCode);

        var env = JsonSerializer.Deserialize<ApiEnvelope<OrderDetailDTO>>(body, _json);
        return env?.data;
    }

    public async Task<OrderDetailDTO?> CreateQuickOrderAsync(QuickOrderRequestDto dto)
    {
        using var http = await NewAuthClientAsync();
        using var content = ToJsonContent(dto);
        var resp = await http.PostAsync("/api/orders/quick", content);
        var body = await resp.Content.ReadAsStringAsync();
        if (!resp.IsSuccessStatusCode)
            throw new HttpRequestException(body, null, resp.StatusCode);

        var env = JsonSerializer.Deserialize<ApiEnvelope<OrderDetailDTO>>(body, _json);
        return env?.data;
    }

    public async Task<List<KdsTicketDTO>> GetKdsTicketsAsync(KdsQuery? query = null)
    {
        using var http = await NewAuthClientAsync();
        var resp = await http.GetAsync("/api/orders/kds/list" + BuildKdsQuery(query));
        var body = await resp.Content.ReadAsStringAsync();
        if (!resp.IsSuccessStatusCode)
            throw new HttpRequestException(body, null, resp.StatusCode);

        var env = JsonSerializer.Deserialize<ApiEnvelope<List<KdsTicketDTO>>>(body, _json);
        return env?.data ?? new();
    }

    public async Task<OrderItemDTO?> AddItemAsync(int orderId, AddOrderItemDto dto)
    {
        using var http = await NewAuthClientAsync();
        using var content = ToJsonContent(dto);
        var resp = await http.PostAsync($"/api/orders/{orderId}/items", content);
        var body = await resp.Content.ReadAsStringAsync();
        if (!resp.IsSuccessStatusCode)
            throw new HttpRequestException(body, null, resp.StatusCode);

        var env = JsonSerializer.Deserialize<ApiEnvelope<OrderItemEnvelope>>(body, _json);
        return env?.data?.item;
    }

    public async Task<OrderItemDTO?> UpdateItemAsync(int itemId, UpdateOrderItemDto dto)
    {
        using var http = await NewAuthClientAsync();
        using var content = ToJsonContent(dto);
        var resp = await http.PatchAsync($"/api/orders/items/{itemId}", content);
        var body = await resp.Content.ReadAsStringAsync();
        if (!resp.IsSuccessStatusCode)
            throw new HttpRequestException(body, null, resp.StatusCode);

        var env = JsonSerializer.Deserialize<ApiEnvelope<OrderItemEnvelope>>(body, _json);
        return env?.data?.item;
    }

    public async Task<OrderItemDTO?> UpdateItemStatusAsync(int itemId, UpdateOrderItemStatusDto dto)
    {
        using var http = await NewAuthClientAsync();
        using var content = ToJsonContent(dto);
        var resp = await http.PatchAsync($"/api/orders/items/{itemId}/status", content);
        var body = await resp.Content.ReadAsStringAsync();
        if (!resp.IsSuccessStatusCode)
            throw new HttpRequestException(body, null, resp.StatusCode);

        var env = JsonSerializer.Deserialize<ApiEnvelope<OrderItemEnvelope>>(body, _json);
        return env?.data?.item;
    }

    public async Task<bool> DeleteItemAsync(int itemId)
    {
        using var http = await NewAuthClientAsync();
        var resp = await http.DeleteAsync($"/api/orders/items/{itemId}");
        if (resp.StatusCode == HttpStatusCode.NoContent)
            return true;
        if (!resp.IsSuccessStatusCode)
        {
            var body = await resp.Content.ReadAsStringAsync();
            throw new HttpRequestException(body, null, resp.StatusCode);
        }
        return true;
    }

    public async Task<bool> UpdateMetaAsync(int orderId, UpdateOrderMetaDto dto)
    {
        using var http = await NewAuthClientAsync();
        using var content = ToJsonContent(dto);
        var resp = await http.PatchAsync($"/api/orders/{orderId}/meta", content);
        await EnsureSuccessAsync(resp);
        return true;
    }

    public async Task<OrderDetailDTO?> UpdateStatusAsync(int orderId, UpdateOrderStatusDto dto)
    {
        using var http = await NewAuthClientAsync();
        using var content = ToJsonContent(dto);
        var resp = await http.PatchAsync($"/api/orders/{orderId}/status", content);
        var body = await resp.Content.ReadAsStringAsync();
        if (!resp.IsSuccessStatusCode)
            throw new HttpRequestException(body, null, resp.StatusCode);

        var env = JsonSerializer.Deserialize<ApiEnvelope<OrderDetailDTO>>(body, _json);
        return env?.data;
    }

    public async Task<RefundOrderResponse?> RefundOrderAsync(int orderId, RefundOrderRequest dto)
    {
        using var http = await NewAuthClientAsync();
        using var content = ToJsonContent(dto);
        var resp = await http.PostAsync($"/api/orders/{orderId}/refund", content);
        var body = await resp.Content.ReadAsStringAsync();
        if (!resp.IsSuccessStatusCode)
            throw new HttpRequestException(body, null, resp.StatusCode);

        var env = JsonSerializer.Deserialize<ApiEnvelope<RefundOrderResponse>>(body, _json);
        return env?.data;
    }

    public async Task<OrderPaymentDTO?> AddPaymentAsync(int orderId, AddPaymentDto dto)
    {
        using var http = await NewAuthClientAsync();
        using var content = ToJsonContent(dto);
        var resp = await http.PostAsync($"/api/orders/{orderId}/payments", content);
        var body = await resp.Content.ReadAsStringAsync();
        if (!resp.IsSuccessStatusCode)
            throw new HttpRequestException(body, null, resp.StatusCode);

        var env = JsonSerializer.Deserialize<ApiEnvelope<OrderPaymentDTO>>(body, _json);
        return env?.data;
    }

    public async Task<bool> DeletePaymentAsync(int orderId, int paymentId)
    {
        using var http = await NewAuthClientAsync();
        var resp = await http.DeleteAsync($"/api/orders/{orderId}/payments/{paymentId}");
        if (resp.StatusCode == HttpStatusCode.NoContent)
            return true;
        if (!resp.IsSuccessStatusCode)
        {
            var body = await resp.Content.ReadAsStringAsync();
            throw new HttpRequestException(body, null, resp.StatusCode);
        }
        return true;
    }

    public async Task<OrderSplitResponseDto?> SplitAsync(int orderId, OrderSplitRequestDto dto)
    {
        using var http = await NewAuthClientAsync();
        var payload = BuildSplitPayload(dto);
        var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.Never
        });
        using var content = new StringContent(json, Encoding.UTF8, "application/json");
        var resp = await http.PostAsync($"/api/orders/{orderId}/split", content);
        var body = await resp.Content.ReadAsStringAsync();
        if (!resp.IsSuccessStatusCode)
            throw new HttpRequestException(body, null, resp.StatusCode);

        var env = JsonSerializer.Deserialize<ApiEnvelope<OrderSplitResponseDto>>(body, _json);
        return env?.data;
    }

    static Dictionary<string, object?> BuildSplitPayload(OrderSplitRequestDto dto)
    {
        var payload = new Dictionary<string, object?>
        {
            ["itemIds"] = dto.itemIds
        };

        if (!string.IsNullOrWhiteSpace(dto.serviceType))
            payload["serviceType"] = dto.serviceType;
        if (dto.SendTableId)
            payload["tableId"] = dto.tableId;
        if (!string.IsNullOrWhiteSpace(dto.note))
            payload["note"] = dto.note;
        if (dto.covers.HasValue)
            payload["covers"] = dto.covers.Value;
        if (dto.deliveryFeeCents.HasValue)
            payload["deliveryFeeCents"] = dto.deliveryFeeCents.Value;

        return payload;
    }

    public async Task<List<TableDTO>> ListTablesAsync(bool includeInactive = true)
    {
        using var http = await NewAuthClientAsync();
        var query = includeInactive ? "?includeInactive=true" : string.Empty;
        var resp = await http.GetAsync($"/api/tables{query}");
        var body = await resp.Content.ReadAsStringAsync();
        if (!resp.IsSuccessStatusCode)
            throw new HttpRequestException(body, null, resp.StatusCode);

        var env = JsonSerializer.Deserialize<ApiEnvelope<List<TableDTO>>>(body, _json);
        return env?.data ?? new();
    }

    public async Task<List<ChannelConfigDTO>> ListChannelConfigsAsync()
    {
        using var http = await NewAuthClientAsync();
        var resp = await http.GetAsync("/api/channel-config");
        var body = await resp.Content.ReadAsStringAsync();
        if (!resp.IsSuccessStatusCode)
            throw new HttpRequestException(body, null, resp.StatusCode);

        var env = JsonSerializer.Deserialize<ApiEnvelope<List<ChannelConfigDTO>>>(body, _json);
        return env?.data ?? new();
    }

    public async Task<ChannelConfigDTO?> UpdateChannelConfigAsync(string source, ChannelConfigUpdateDto dto)
    {
        using var http = await NewAuthClientAsync();
        using var content = ToJsonContent(dto);
        var resp = await http.PutAsync($"/api/channel-config/{Uri.EscapeDataString(source)}", content);
        var body = await resp.Content.ReadAsStringAsync();
        if (!resp.IsSuccessStatusCode)
            throw new HttpRequestException(body, null, resp.StatusCode);

        var env = JsonSerializer.Deserialize<ApiEnvelope<ChannelConfigDTO>>(body, _json);
        return env?.data;
    }

    public async Task<PaymentsReportDto?> GetPaymentsReportAsync(DateTime? from = null, DateTime? to = null, bool includeOrders = false, int? tzOffsetMinutes = null)
    {
        using var http = await NewAuthClientAsync();
        var resp = await http.GetAsync($"/api/reports/payments{BuildPaymentsReportQuery(from, to, includeOrders, tzOffsetMinutes)}");
        var body = await resp.Content.ReadAsStringAsync();
        if (!resp.IsSuccessStatusCode)
            throw new HttpRequestException(body, null, resp.StatusCode);

        var env = JsonSerializer.Deserialize<ApiEnvelope<PaymentsReportDto>>(body, _json);
        return env?.data;
    }

    class OrderItemEnvelope
    {
        public OrderItemDTO? item { get; set; }
    }
}
