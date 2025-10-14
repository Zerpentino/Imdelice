// ---------- Usings ----------
using System;
using System.IO;                    // MemoryStream, etc.
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Collections.Generic;   // List<T>
using System.Collections.ObjectModel;
using System.ComponentModel;        // INotifyPropertyChanged, PropertyChangedEventHandler
using System.Net.Http.Headers;
using Imdeliceapp.Models;
using Microsoft.Maui.Storage;
using Microsoft.Maui.Networking;
using Microsoft.Maui.ApplicationModel;   // MainThread
using Microsoft.Maui.Controls;

using Imdeliceapp.Helpers;

namespace Imdeliceapp.Pages
{
    #region DTOs y envolturas (locales)
//  public class ApiEnvelopeMods<T>
// {
//     public string? error { get; set; }
//     public T? data { get; set; }
//     public string? message { get; set; }
// }

//     // Opción dentro de un grupo de modificadores
//    public class ModifierOptionDTO
// {
//     public int id { get; set; }
//     public int groupId { get; set; }
//     public string name { get; set; } = "";
//     public int priceExtraCents { get; set; }
//     public bool isDefault { get; set; }
//     public bool isActive { get; set; }
//     public int position { get; set; }
// }

//     // Grupo de modificadores
//     public class ModifierGroupDTO
// {
//     public int id { get; set; }
//     public string name { get; set; } = "";
//     public string? description { get; set; }
//     public int minSelect { get; set; }
//     public int? maxSelect { get; set; }
//     public bool isRequired { get; set; }
//     public bool isActive { get; set; }
//     public int position { get; set; }
//     public int? appliesToCategoryId { get; set; }
//     public List<ModifierOptionDTO> options { get; set; } = new();
// }

// public class ProductGroupLinkDTO
// {
//     public int id { get; set; }         // id del link
//     public int position { get; set; }   // orden del grupo en el producto
//     public ModifierGroupDTO? group { get; set; }
// }
    #endregion

    // Recibe parámetros por Shell: ?productId=123&basePrice=79.9
    [QueryProperty(nameof(ProductId), "productId")]
    [QueryProperty(nameof(BasePrice), "basePrice")]
    public partial class ProductModifiersPage : ContentPage
    {
        #region Tipos de soporte (ViewModels locales)
        public class ModifierSelection : INotifyPropertyChanged
        {
			public bool ShowRadios  => Min == 1 && Max == 1;
public bool ShowChecks  => !(Min == 1 && Max == 1);

            public int GroupId { get; }
            public string GroupName { get; }
            public int Min { get; }
            public int? Max { get; }
            public bool IsRequired { get; }

            public ObservableCollection<ModifierOptionItem> Options { get; } = new();
			public string RuleText
{
    get
    {
        // Texto base según min/max
        string regla;
        if (Min == 1 && Max == 1)
            regla = "Elige 1";
        else if (Max.HasValue)
            regla = $"Elige {Min}–{Max.Value}";
        else
            regla = Min > 0 ? $"Elige al menos {Min}" : "Opcional";

        // Sufijo si es requerido
        if (IsRequired && Min > 0)
            regla += " (obligatorio)";
        else if (Min == 0)
            regla += " (opcional)";

        return regla;
    }
}

            public ModifierSelection(ModifierGroupDTO g)
            {
				
                GroupId = g.id; GroupName = g.name;
                Min = g.minSelect; Max = g.maxSelect; IsRequired = g.isRequired;

                foreach (var o in (g.options ?? new()).OrderBy(o => o.position))
                    Options.Add(new ModifierOptionItem(o, g));
            }

            public bool IsValid =>
                Options.Count(x => x.IsSelected) >= Min &&
                (!Max.HasValue || Options.Count(x => x.IsSelected) <= Max.Value);

            public event PropertyChangedEventHandler? PropertyChanged;
        }

        public class ModifierOptionItem : INotifyPropertyChanged
        {
			public string ExtraLabel => PriceExtraCents == 0 ? "" : $"+${PriceExtraCents / 100.0:0.00}";
public bool HasExtra => PriceExtraCents > 0;

            public int Id { get; }
            public string Name { get; }
            public int PriceExtraCents { get; }
            public bool IsDefault { get; }
            public bool IsActive { get; }
            public int GroupMin { get; }
            public int? GroupMax { get; }

            private bool _isSelected;
            public bool IsSelected
            {
                get => _isSelected;
                set
                {
                    if (_isSelected == value) return;
                    _isSelected = value;
                    PropertyChanged?.Invoke(this, new(nameof(IsSelected)));
                }
            }

            public ModifierOptionItem(ModifierOptionDTO dto, ModifierGroupDTO group)
            {
                Id = dto.id; Name = dto.name; PriceExtraCents = dto.priceExtraCents;
                IsDefault = dto.isDefault; IsActive = dto.isActive;
                GroupMin = group.minSelect; GroupMax = group.maxSelect;
                IsSelected = IsDefault;
            }

            public event PropertyChangedEventHandler? PropertyChanged;
        }
        #endregion

        #region Campos/props
        // Recibidos por QueryProperty
        public int ProductId
        {
            get => _productId;
            set => _productId = value;
        }
        public double BasePrice
        {
            get => _basePrice;
            set
            {
                _basePrice = value;
                TotalLabel = $"Total: ${_basePrice:0.00}";
            }
        }

        private int _productId;
        private double _basePrice; // dinero (no cents)

        private readonly JsonSerializerOptions _json = new() { PropertyNameCaseInsensitive = true };

        public ObservableCollection<ModifierSelection> Groups { get; } = new();

        public string TotalLabel
        {
            get => _totalLabel;
            set { _totalLabel = value; OnPropertyChanged(); }
        }
        private string _totalLabel = "$0.00";
        #endregion

        #region Ctor / ciclo de vida
        // IMPORTANTE: ctor vacío para XAML
        public ProductModifiersPage()
        {
            InitializeComponent();
            BindingContext = this;
        }

        // Si prefieres instanciar manualmente sin Shell:
        // public ProductModifiersPage(int productId, double basePrice) : this()
        // {
        //     ProductId = productId;
        //     BasePrice = basePrice;
        // }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
			    Console.WriteLine($"[Mods] OnAppearing -> ProductId={ProductId}, BasePrice={BasePrice}");

            await LoadGroupsAsync();
            RecalcTotal();
        }
        #endregion

        #region Carga de datos
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

				// Trae los grupos VINCULADOS a este producto (group + options)
				var resp = await http.GetAsync($"/api/modifiers/groups/by-product/{ProductId}");
				var body = await resp.Content.ReadAsStringAsync();
				if (!resp.IsSuccessStatusCode)
				{
					await DisplayAlert("Error", string.IsNullOrWhiteSpace(body) ? "No se pudo cargar los modificadores." : body, "OK");
					return;
				}
				Console.WriteLine($"[Mods] GET /by-product/{ProductId} -> HTTP {(int)resp.StatusCode}");


				var env = JsonSerializer.Deserialize<ApiEnvelopeMods<List<ProductGroupLinkDTO>>>(body, _json);
				if (env == null)
				{
					await DisplayAlert("Error", "Respuesta inválida del servidor.", "OK");
					return;
				}
				if (!string.IsNullOrWhiteSpace(env.error))
					Console.WriteLine($"[Mods] env.error: {env.error}");


				var links = env?.data ?? new();
				Console.WriteLine($"[Mods] links.Count={links.Count}");

				Groups.Clear();
				foreach (var link in links.OrderBy(l => l.position))
				{
					var g = link.group;
					if (g == null || !g.isActive) continue;

					// filtra opciones activas y ordena
					g.options = (g.options ?? new()).Where(o => o.isActive).OrderBy(o => o.position).ToList();
					Console.WriteLine($"[Mods] group='{g.name}' optionsActivas={g.options.Count}");

					Groups.Add(new ModifierSelection(g));
				}
				if (Groups.Count == 0)
            await DisplayAlert("Sin opciones", "Este producto no tiene grupos u opciones activas vinculadas.", "OK");
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
        #endregion

        #region Cálculo de total
        private void RecalcTotal()
        {
            var extraCents = Groups
                .SelectMany(g => g.Options)
                .Where(o => o.IsSelected)
                .Sum(o => o.PriceExtraCents);

            var total = BasePrice + extraCents / 100.0;
            TotalLabel = $"Total: ${total:0.00}";
        }
        #endregion

        #region Eventos UI
        // Radios: si marcas uno, desmarca los demás del grupo
        private void Radio_CheckedChanged(object sender, CheckedChangedEventArgs e)
        {
            if (sender is not RadioButton rb || rb.BindingContext is not ModifierOptionItem opt || !e.Value)
                return;

            var sel = Groups.FirstOrDefault(g => g.Options.Contains(opt));
            if (sel != null)
            {
                foreach (var o in sel.Options)
                    if (!ReferenceEquals(o, opt) && o.IsSelected) o.IsSelected = false;
            }
            RecalcTotal();
        }

        // Checkboxes: respeta maxSelect si existe
        private void Check_CheckedChanged(object sender, CheckedChangedEventArgs e)
        {
            if (sender is not CheckBox cb || cb.BindingContext is not ModifierOptionItem opt)
                return;

            var sel = Groups.FirstOrDefault(g => g.Options.Contains(opt));
            if (sel != null && e.Value && sel.Max.HasValue)
            {
                var count = sel.Options.Count(o => o.IsSelected);
                if (count > sel.Max.Value)
                {
                    // desmarcar este último y avisar
                    opt.IsSelected = false;
                    MainThread.BeginInvokeOnMainThread(async () =>
                        await DisplayAlert("Límite", $"Máximo {sel.Max.Value} opción(es) en {sel.GroupName}.", "OK"));
                }
            }
            RecalcTotal();
        }

        private async void Confirm_Clicked(object sender, EventArgs e)
        {
            // Validación isRequired/min
            var invalid = Groups.FirstOrDefault(g => !g.IsValid && g.IsRequired);
            if (invalid != null)
            {
                await DisplayAlert("Falta elegir", $"Selecciona al menos {invalid.Min} opción(es) en {invalid.GroupName}.", "OK");
                return;
            }

            // Resultado elegido (por grupo → lista de optionIds)
            var chosen = Groups.ToDictionary(
                g => g.GroupId,
                g => g.Options.Where(o => o.IsSelected).Select(o => o.Id).ToList()
            );

            // TODO: envía 'chosen' a tu carrito/orden donde construyes el payload del OrderItem
            // (El backend valida isActive && isAvailable del producto/variante; los modifiers son recargos.)

            await Navigation.PopAsync();
        }
        #endregion
    }
}
