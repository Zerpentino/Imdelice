// ---------- Usings ----------
using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Net.Http.Headers;
using Imdeliceapp.Models;
using Microsoft.Maui.Storage;
using Microsoft.Maui.Networking;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using System.Text; // Encoding.UTF8

namespace Imdeliceapp.Pages
{
    [QueryProperty(nameof(ProductId), "productId")]
    public partial class ProductModifiersPage : ContentPage
    {
        // Cambia a "isActive" si tu API no usa isAvailable:
        // 

        // ------------ VMs locales ------------
        public class ModifierSelection
        {
            public int GroupId { get; }
            public string GroupName { get; }
            public ObservableCollection<ModifierOptionItem> Options { get; } = new();

            public ModifierSelection(ModifierGroupDTO g)
            {
                GroupId = g.id;
                GroupName = g.name;
                foreach (var o in (g.options ?? new()).OrderBy(o => o.position))
                    Options.Add(new ModifierOptionItem(o));
            }
        }

        public class ModifierOptionItem : INotifyPropertyChanged
{
    public int Id { get; }
    public string Name { get; }
    public int PriceExtraCents { get; }

    bool _isActive;
    public bool IsActive
    {
        get => _isActive;
        set { if (_isActive == value) return; _isActive = value; PropertyChanged?.Invoke(this, new(nameof(IsActive))); }
    }

    public string ExtraLabel => PriceExtraCents == 0 ? "" : $"+${PriceExtraCents / 100.0:0.00}";
    public bool HasExtra => PriceExtraCents > 0;

    public ModifierOptionItem(ModifierOptionDTO dto)
    {
        Id = dto.id;
        Name = dto.name;
        PriceExtraCents = dto.priceExtraCents;
        IsActive = dto.isActive; // ← disponibilidad real en el backend
    }

    public event PropertyChangedEventHandler? PropertyChanged;
}


        // ------------ Estado ------------
        public int ProductId { get; set; }
        public ObservableCollection<ModifierSelection> Groups { get; } = new();
        readonly JsonSerializerOptions _json = new() { PropertyNameCaseInsensitive = true };
        bool _silenceSwitch;
        readonly HashSet<int> _busyOptions = new();

        public ProductModifiersPage()
        {
            InitializeComponent();
            BindingContext = this;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await LoadGroupsAsync();
        }

        // ------------ Carga ------------
        private async Task LoadGroupsAsync()
        {
            try
            {
                if (ProductId <= 0)
                {
                    await DisplayAlert("Aviso", "Falta productId.", "OK");
                    return;
                }

                var token = await SecureStorage.GetAsync("token") ?? Preferences.Default.Get("token", "");
                var baseUrl = Application.Current.Resources["urlbase"].ToString().TrimEnd('/');

                using var http = new HttpClient { BaseAddress = new Uri(baseUrl), Timeout = TimeSpan.FromSeconds(15) };
                http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                // grupos vinculados (group + options)
                
                var resp = await http.GetAsync($"/api/modifiers/groups/by-product/{ProductId}");
                var body = await resp.Content.ReadAsStringAsync();
                if (!resp.IsSuccessStatusCode)
                {
                    await DisplayAlert("Error", string.IsNullOrWhiteSpace(body) ? "No se pudo cargar los modificadores." : body, "OK");
                    return;
                }

                var env = JsonSerializer.Deserialize<ApiEnvelopeMods<List<ProductGroupLinkDTO>>>(body, _json);
                var links = env?.data ?? new();

                Groups.Clear();
                foreach (var link in links.OrderBy(l => l.position))
                {
                    var g = link.group;
                    if (g == null || !g.isActive) continue;

                    // IMPORTANTE: no filtramos por disponibilidad; necesitamos ver todas
                    g.options = (g.options ?? new()).OrderBy(o => o.position).ToList();
                    Groups.Add(new ModifierSelection(g));
                }

                if (Groups.Count == 0)
                    await DisplayAlert("Sin opciones", "Este producto no tiene grupos u opciones vinculadas.", "OK");
            }
            catch (TaskCanceledException)
            {
                await DisplayAlert("Tiempo agotado", "El servidor tardó demasiado en responder.", "OK");
            }
            catch (HttpRequestException)
            {
                await DisplayAlert("Red", "No se pudo contactar al servidor.", "OK");
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", ex.Message, "OK");
            }
        }

        // ------------ PATCH helper ------------
        HttpRequestMessage BuildPatch(string url, string fieldName, bool value)
        {
            var payload = new Dictionary<string, object> { [fieldName] = value };
            var req = new HttpRequestMessage(new HttpMethod("PATCH"), url)
            {
                Content = new StringContent(JsonSerializer.Serialize(payload),
                                            System.Text.Encoding.UTF8,
                                            "application/json"),
                Version = HttpVersion.Version11,
                VersionPolicy = HttpVersionPolicy.RequestVersionOrLower
            };
            req.Headers.ConnectionClose = true;
            return req;
        }

        static bool IsTransient(HttpRequestException ex)
        {
            var m = ex.Message?.ToLowerInvariant() ?? "";
            return m.Contains("unexpected end of stream")
                || m.Contains("socket closed")
                || m.Contains("reset");
        }

        async Task<HttpResponseMessage> SendWithOneRetryAsync(Func<HttpRequestMessage> makeRequest)
        {
            for (int attempt = 1; attempt <= 2; attempt++)
            {
                try
                {
                    var token = await SecureStorage.GetAsync("token") ?? Preferences.Default.Get("token", "");
                    var baseUrl = Application.Current.Resources["urlbase"].ToString().TrimEnd('/');

                    using var http = new HttpClient { BaseAddress = new Uri(baseUrl), Timeout = TimeSpan.FromSeconds(15) };
                    http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                    using var req = makeRequest();
                    return await http.SendAsync(req);
                }
                catch (HttpRequestException ex) when (attempt == 1 && IsTransient(ex))
                {
                    await Task.Delay(150);
                    continue;
                }
            }
            throw new HttpRequestException("Fallo al reintentar.");
        }
        // ------------ Toggle de disponibilidad ------------

async void OptionToggle_Toggled(object sender, ToggledEventArgs e)
{
    if (_silenceSwitch) return;
    if (sender is not Switch sw) return;
    if (sw.BindingContext is not ModifierOptionItem opt) return;

    if (_busyOptions.Contains(opt.Id))
    {
        _silenceSwitch = true;
        sw.IsToggled = opt.IsActive; // no permitir doble click concurrente
        _silenceSwitch = false;
        return;
    }

    var nuevo = e.Value;
    var anterior = opt.IsActive;

    _busyOptions.Add(opt.Id);
    sw.IsEnabled = false;

    try
    {
        // UI optimista
        opt.IsActive = nuevo;

        var resp = await SendWithOneRetryAsync(
            () => BuildPatch($"/api/modifiers/modifier-options/{opt.Id}", "isActive", nuevo)
            // ↑ si tu API no tiene /api, usa: $"/modifier-options/{opt.Id}"
        );

        if (!resp.IsSuccessStatusCode)
        {
            // revertir
            _silenceSwitch = true;
            opt.IsActive = anterior;
            sw.IsToggled = anterior;
            _silenceSwitch = false;

            var body = await resp.Content.ReadAsStringAsync();
            await DisplayAlert("Error",
                string.IsNullOrWhiteSpace(body) ? "No se pudo actualizar la disponibilidad." : body,
                "OK");
            return;
        }
    }
    catch (Exception ex)
    {
        _silenceSwitch = true;
        opt.IsActive = anterior;
        sw.IsToggled = anterior;
        _silenceSwitch = false;

        await DisplayAlert("Red", ex.Message, "OK");
    }
    finally
    {
        _busyOptions.Remove(opt.Id);
        sw.IsEnabled = true;
    }
}


    }
}
