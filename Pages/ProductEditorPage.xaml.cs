using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.RegularExpressions;
using Imdeliceapp.Helpers;
using Microsoft.Maui.Networking;
using Microsoft.Maui.Storage;
using System.Linq;
using Microsoft.Maui.Media;     // solo si vas a usar MediaPicker (capturar/galería)
using System.Text;              // por Encoding en Txt(...)
using System.IO;                // por MemoryStream, etc.
using System.Net.Http;          // ByteArrayContent, MultipartFormDataContent
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Controls.Shapes;

namespace Imdeliceapp.Pages;

[QueryProperty(nameof(Mode), "mode")]
[QueryProperty(nameof(ProductId), "id")]
[QueryProperty(nameof(PreselectedCategoryId), "categoryId")]
[QueryProperty(nameof(InitialBarcode), "initialBarcode")]


public partial class ProductEditorPage : ContentPage
{
    FileResult? _pickedImage;          // archivo elegido en esta sesión
    byte[]? _pickedBytes;              // caché para re-enviar
    bool _removeImageFlag = false;     // marcar “quitar imagen” en PATCH
    string? _baseUrlCache;             // para armar preview del GET /image

    public int PreselectedCategoryId { get; set; }
    public string? Mode { get; set; }   // create | edit
    public int ProductId { get; set; }
    public string? InitialBarcode { get; set; }

    class CategoryDTO
    {
        public int id { get; set; }
        public string? name { get; set; }
        public string? slug { get; set; }
        public int position { get; set; }
        public bool isComboOnly { get; set; }   // <-- NUEVO

    }
    class ProductDetailDTO
{
    public int id { get; set; }
    public string? type { get; set; }
    public string? name { get; set; }
    public int categoryId { get; set; }
    public int? priceCents { get; set; }
    public string? description { get; set; }
    public string? sku { get; set; }
    public string? barcode { get; set; }
    public string? imageUrl { get; set; }
    public bool isActive { get; set; }
    public List<VariantDTO> variants { get; set; } = new();

    // NUEVO: viene en GET /api/products/:id cuando es COMBO
    public List<ComboItemAsComboDTO>? comboItemsAsCombo { get; set; }
}

class ComboItemAsComboDTO
{
    public int id { get; set; }
    public int quantity { get; set; }
    public string? notes { get; set; }
    public bool isRequired { get; set; } = true;
    public ProductMiniDTO? componentProduct { get; set; } // trae id, categoryId, etc.
    public VariantDTO? componentVariant { get; set; }
}

    class VariantDTO { public int id { get; set; } public string? name { get; set; } public int priceCents { get; set; } public bool isActive { get; set; } }
    class ApiEnvelope<T> { public bool ok { get; set; } public T? data { get; set; } public string? error { get; set; } public string? message { get; set; } }

    class ProductOption
    {
        public int id { get; set; }
        public string? name { get; set; }
        public string? type { get; set; } // SIMPLE | VARIANTED | COMBO
        public override string ToString() => name ?? $"#{id}";
    }

class ComboItemDTO
{
    public int id { get; set; }                 // comboItemId
    public int componentProductId { get; set; }
    public int? componentVariantId { get; set; }
        public int quantity { get; set; }
        public bool isRequired { get; set; } = true;
        public string? notes { get; set; }
    }

class ProductMiniDTO   // para resolver categoría del componente
{
    public int id { get; set; }
    public string? name { get; set; }
    public int categoryId { get; set; }
    public string? type { get; set; }  // SIMPLE / VARIANTED / COMBO
}

record ComboItemInput(
    int? ComboItemId,
    int ComponentProductId,
    string ProductName,
    string ProductType,
    int? ComponentVariantId,
    int Quantity,
    bool IsRequired,
    string? Notes,
    int? OriginalComponentProductId,
    int? OriginalComponentVariantId);

    class ComboRowContext
    {
        public int? comboItemId { get; set; }
        public int? originalComponentProductId { get; set; }
        public int? originalComponentVariantId { get; set; }
        public Picker? PkCategory { get; set; }
        public Picker? PkProduct { get; set; }
        public Picker? PkVariant { get; set; }
        public Entry? QtyEntry { get; set; }
        public Switch? RequiredSwitch { get; set; }
        public Entry? NotesEntry { get; set; }
        public int? PendingVariantId { get; set; }
    }


    static readonly JsonSerializerOptions _json = new() { PropertyNameCaseInsensitive = true };
    static readonly JsonSerializerOptions _jsonWrite = new()
    {
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
        PropertyNameCaseInsensitive = true
    };
    // async Task<List<ComboItemDTO>> LoadComboItemsAsync(HttpClient http, int productId)
    // {
    //     var resp = await http.GetAsync($"/api/products/{productId}/combo-items");
    //     if (!resp.IsSuccessStatusCode) return new();
    //     var body = await resp.Content.ReadAsStringAsync();
    //     var env = JsonSerializer.Deserialize<ApiEnvelope<List<ComboItemDTO>>>(body, _json);
    //     return env?.data ?? new();
    // }

    async Task<ProductDetailDTO?> LoadProductDetailAsync(HttpClient http, int productId)
    {
        var resp = await http.GetAsync($"/api/products/{productId}");
        if (!resp.IsSuccessStatusCode) return null;
        var body = await resp.Content.ReadAsStringAsync();
        var env = JsonSerializer.Deserialize<ApiEnvelope<ProductDetailDTO>>(body, _json);
        return env?.data;
    }

    // originales para comparar (PATCH)
    string? _origName, _origSku, _origBarcode, _origImage, _origDesc, _origType;
    int _origCategoryId;
    int? _origPriceCents;
    List<VariantDTO> _origVariants = new();

    List<CategoryDTO> _cats = new();
    // Piezas originales del combo (para diff al guardar)
    List<ComboItemDTO> _origComboItems = new();
    readonly Dictionary<int, List<VariantDTO>> _variantCache = new();

    readonly Color _errorStroke = Color.FromArgb("#E53935");
    readonly Color _inputStrokeLight = Color.FromArgb("#DDD");
    readonly Color _inputStrokeDark = Color.FromArgb("#444");

// Lee las filas del UI e incluye comboItemId si existe
    static string? MapProductError(HttpStatusCode status, string? body)
    {
        var b = (body ?? "").ToLowerInvariant();
        bool dup = status == HttpStatusCode.Conflict
                || b.Contains("p2002")
                || b.Contains("unique constraint failed");

        if (!dup) return null;

        if (b.Contains("product_name_key") || (b.Contains("constraint") && b.Contains("name")))
            return "Ya existe un producto con ese nombre.";
        if (b.Contains("product_sku_key") || (b.Contains("constraint") && b.Contains("sku")))
            return "Ese SKU ya está en uso.";
        return "Ese registro ya existe.";
    }
    static string ExtractApiError(string? body)
    {
        if (string.IsNullOrWhiteSpace(body)) return "Error al procesar la respuesta.";
        try
        {
            using var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;

            // 1) $.error
            if (root.TryGetProperty("error", out var err))
            {
                if (err.ValueKind == JsonValueKind.String) return err.GetString() ?? body;

                // si es objeto, intenta $.error.message
                if (err.ValueKind == JsonValueKind.Object &&
                    err.TryGetProperty("message", out var msg) &&
                    msg.ValueKind == JsonValueKind.String)
                    return msg.GetString() ?? body;

                // o conviértelo a texto
                return err.ToString();
            }

            // 2) $.message directo
            if (root.TryGetProperty("message", out var message) &&
                message.ValueKind == JsonValueKind.String)
                return message.GetString() ?? body;

            // 3) fallback
            return body;
        }
        catch
        {
            return body;
        }
    }

    MultipartFormDataContent NewForm()
    {
        return new MultipartFormDataContent(); // boundary automático
    }

    // helpers
    static StringContent Txt(string? v) => new(v ?? "", Encoding.UTF8, "text/plain");
    // ⬇️ importante: que sea application/json (no text/plain)
    static StringContent Num(int v) => new(v.ToString(), Encoding.UTF8, "application/json");
    static StringContent JsonText(string json) => new(json, Encoding.UTF8, "application/json");
    static string? NormalizeBarcode(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    static JsonSerializerOptions JsonWriteAllowNulls() => new()
    {
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.Never,
        PropertyNameCaseInsensitive = true
    };

    async Task<List<ProductOption>> LoadProductsForCategoryAsync(CategoryDTO cat)
    {
        var token = await GetTokenAsync();
        if (string.IsNullOrWhiteSpace(token)) return new();

        var baseUrl = Application.Current.Resources["urlbase"].ToString().TrimEnd('/');
        using var http = NewAuthClient(baseUrl, token);

        var slug = Uri.EscapeDataString(cat.slug ?? string.Empty);

        var result = new List<ProductOption>();

        // SIMPLE
        var r1 = await http.GetAsync($"/api/products?type=SIMPLE&categorySlug={slug}&isActive=true");
        if (r1.IsSuccessStatusCode)
        {
            var b1 = await r1.Content.ReadAsStringAsync();
            var e1 = JsonSerializer.Deserialize<ApiEnvelope<List<ProductOption>>>(b1, _json);
            if (e1?.data != null) result.AddRange(e1.data);
        }

        // VARIANTED
        var r2 = await http.GetAsync($"/api/products?type=VARIANTED&categorySlug={slug}&isActive=true");
        if (r2.IsSuccessStatusCode)
        {
            var b2 = await r2.Content.ReadAsStringAsync();
            var e2 = JsonSerializer.Deserialize<ApiEnvelope<List<ProductOption>>>(b2, _json);
            if (e2?.data != null) result.AddRange(e2.data);
        }

        // (por si acaso viniera alguno marcado como COMBO, lo filtramos)
        return result
            .Where(p => !string.Equals(p.type, "COMBO", StringComparison.OrdinalIgnoreCase))
            .OrderBy(p => p.name)
            .ToList();
    }


    // agrega campo file si existe
    void AddImageIfAny(MultipartFormDataContent form)
    {
        if (_removeImageFlag)
        {
            // “image” presente con valor vacío => backend interpreta eliminar imagen
            form.Add(Txt(string.Empty), "image");
            return;
        }

        if (_pickedBytes != null && _pickedImage != null)
        {
            var sc = new ByteArrayContent(_pickedBytes);
            // content-type
            var ct = _pickedImage.FileName.EndsWith(".png", StringComparison.OrdinalIgnoreCase) ? "image/png" : "image/jpeg";
            sc.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(ct);
            // nombre de archivo
            form.Add(sc, "image", _pickedImage.FileName);
        }
    }
    void EnforceTypeByCategory()
    {
        var cat = PkCategory.SelectedItem as CategoryDTO;
        if (cat == null) return;

        if (cat.isComboOnly)
        {
            // forzar COMBO y deshabilitar el picker
            PkType.SelectedItem = "COMBO";
            PkType.IsEnabled = false;
        }
        else
        {
            // habilitar normal
            if (PkType.SelectedIndex < 0) PkType.SelectedIndex = 0; // SIMPLE por defecto
            PkType.IsEnabled = true;
        }
        UpdateTitleForType(PkType.SelectedItem as string, isEdit: IsEdit);

        TogglePanels();
    }

    public ProductEditorPage() => InitializeComponent();

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        // provisional mientras carga
        UpdateTitleForType(null, IsEdit);

        await CargarCategoriasAsync();

        if (!IsEdit)
        {
            if (PkType.SelectedIndex < 0) PkType.SelectedIndex = 0;
            TogglePanels();
        }

        if (IsEdit && ProductId > 0)
            await CargarProductoAsync(ProductId);

        // precargar código de barras cuando venga de escáner
        if (!string.IsNullOrWhiteSpace(InitialBarcode) && string.IsNullOrWhiteSpace(TxtBarcode.Text))
            TxtBarcode.Text = InitialBarcode;
    }



    bool IsEdit => string.Equals(Mode, "edit", StringComparison.OrdinalIgnoreCase);

    static async Task<string?> GetTokenAsync()
    {
        var s = await SecureStorage.GetAsync("token");
        if (!string.IsNullOrWhiteSpace(s)) return s;
        var p = Preferences.Default.Get("token", string.Empty);
        return string.IsNullOrWhiteSpace(p) ? null : p;
    }
    HttpClient NewAuthClient(string baseUrl, string token)
    {
        var cli = new HttpClient { BaseAddress = new Uri(baseUrl), Timeout = TimeSpan.FromSeconds(20) };
        cli.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return cli;
    }

    void SetSaving(bool v) { BtnGuardar.IsEnabled = !v; BtnCancelar.IsEnabled = !v; }

    async Task CargarCategoriasAsync()
    {
        try
        {
            var token = await GetTokenAsync();
            if (string.IsNullOrWhiteSpace(token)) { await AuthHelper.VerificarYRedirigirSiExpirado(this); return; }




            var baseUrl = Application.Current.Resources["urlbase"].ToString().TrimEnd('/');
            using var http = NewAuthClient(baseUrl, token);

            var resp = await http.GetAsync("/api/categories");
            var body = await resp.Content.ReadAsStringAsync();
            if (!resp.IsSuccessStatusCode)
            { if (resp.StatusCode == HttpStatusCode.Unauthorized) { await AuthHelper.VerificarYRedirigirSiExpirado(this); return; } await ErrorHandler.MostrarErrorUsuario(body); return; }

            var env = JsonSerializer.Deserialize<ApiEnvelope<List<CategoryDTO>>>(body, _json);






            _cats = (env?.data ?? new()).OrderBy(c => c.position).ToList();
            PkCategory.ItemsSource = _cats;

            if (PreselectedCategoryId > 0 && PkCategory.SelectedItem == null)
            {
                var m = _cats.FirstOrDefault(c => c.id == PreselectedCategoryId);
                if (m != null) PkCategory.SelectedItem = m;
            }

            // fallback
            if (PkCategory.SelectedItem == null && _cats.Count > 0)
                PkCategory.SelectedItem = _cats[0];

            EnforceTypeByCategory(); // <- aquí



        }
        catch (Exception ex) { await ErrorHandler.MostrarErrorTecnico(ex, "Product – Cargar categorías"); }
    }
    void PkCategory_SelectedIndexChanged(object s, EventArgs e) => EnforceTypeByCategory();

    async Task CargarProductoAsync(int id)
    {
        try
        {
            var token = await GetTokenAsync();
            if (string.IsNullOrWhiteSpace(token)) { await AuthHelper.VerificarYRedirigirSiExpirado(this); return; }
            var baseUrl = Application.Current.Resources["urlbase"].ToString().TrimEnd('/');
            using var http = NewAuthClient(baseUrl, token);

            var resp = await http.GetAsync($"/api/products/{id}");
            var body = await resp.Content.ReadAsStringAsync();
            if (!resp.IsSuccessStatusCode)
            { if (resp.StatusCode == HttpStatusCode.Unauthorized) { await AuthHelper.VerificarYRedirigirSiExpirado(this); return; } await ErrorHandler.MostrarErrorUsuario(body); return; }

            var env = JsonSerializer.Deserialize<ApiEnvelope<ProductDetailDTO>>(body, _json);
            var p = env?.data; if (p is null) { await ErrorHandler.MostrarErrorUsuario("Producto no encontrado."); await Shell.Current.GoToAsync(".."); return; }

            _origType = p.type ?? "SIMPLE";
            UpdateTitleForType(_origType, isEdit: true);

            _origName = p.name ?? "";
            _origSku = p.sku ?? "";
            _origBarcode = NormalizeBarcode(p.barcode);
            _origImage = p.imageUrl ?? "";
            _origDesc = p.description ?? "";
            _origCategoryId = p.categoryId;
            _origPriceCents = p.priceCents;
            _origVariants = p.variants ?? new();

            _baseUrlCache = Application.Current.Resources["urlbase"].ToString().TrimEnd('/');

            // si el producto ya tiene imagen en el backend, úsala vía GET /products/:id/image
            // agrega un "ts" para evitar cache agresivo
            ImgPreview.Source = null;
            _removeImageFlag = false;
            _pickedImage = null;
            _pickedBytes = null;
            await LoadServerImageAsync(id);


            PkType.SelectedItem = _origType;
            PkCategory.SelectedItem = _cats.FirstOrDefault(c => c.id == _origCategoryId);
            TxtName.Text = _origName;
            TxtSku.Text = _origSku;
            TxtBarcode.Text = _origBarcode ?? string.Empty;
            TxtDesc.Text = _origDesc;

            TogglePanels();
            if (_origType == "COMBO")
            {
                // Precio ya se pobló con el bloque del punto 1

                // Limpiar host y cargar piezas
                ComboItemsHost.Children.Clear();

                token = await GetTokenAsync();
                baseUrl = Application.Current.Resources["urlbase"].ToString().TrimEnd('/');
                using var httpLocal = NewAuthClient(baseUrl, token!);

                ComboItemsHost.Children.Clear();

                // Traer piezas desde el endpoint de items
                _origComboItems =
    p.comboItemsAsCombo?
     .Where(ci => ci.componentProduct != null)
     .Select(ci => new ComboItemDTO {
         id = ci.id,
         componentProductId = ci.componentProduct!.id,
         componentVariantId = ci.componentVariant?.id,
         quantity = ci.quantity,
         isRequired = ci.isRequired,
         notes = ci.notes
     }).ToList()
    ?? new List<ComboItemDTO>();

                foreach (var it in _origComboItems)
                {
                    // Necesitamos la categoría del producto componente para preseleccionar
                    var comp = await LoadProductDetailAsync(http, it.componentProductId);
                    if (comp?.variants != null && comp.variants.Count > 0)
                        _variantCache[it.componentProductId] = comp.variants;

                    var row = NewComboItemRow(
                        qty: (it.quantity <= 0 ? "1" : it.quantity.ToString()),
                        notes: it.notes ?? string.Empty,
                        isRequired: it.isRequired,
                        variantId: it.componentVariantId
                    );

                    // guarda metadatos en la fila
                    if (row.BindingContext is ComboRowContext ctx)
                    {
                        ctx.comboItemId = it.id;
                        ctx.originalComponentProductId = it.componentProductId;
                        ctx.originalComponentVariantId = it.componentVariantId;
                        ctx.PendingVariantId = it.componentVariantId;
                        if (ctx.RequiredSwitch != null)
                            ctx.RequiredSwitch.IsToggled = it.isRequired;
                    }

                    ComboItemsHost.Children.Add(row);

                    if (comp != null && row.BindingContext is ComboRowContext rowCtx)
                    {
                        var pkCat = rowCtx.PkCategory;
                        var pkProd = rowCtx.PkProduct;

                        if (pkCat != null && pkProd != null)
                        {
                            var catObj = _cats.FirstOrDefault(c => c.id == comp.categoryId);
                            if (catObj != null)
                            {
                                pkCat.SelectedItem = catObj;
                                var list = await LoadProductsForCategoryAsync(catObj);
                                pkProd.ItemsSource = list;
                                pkProd.SelectedItem = list.FirstOrDefault(p => p.id == it.componentProductId);
                            }
                        }
                    }
                }

            }


            if ((_origType == "SIMPLE" || _origType == "COMBO") && _origPriceCents.HasValue)
            {
                TxtPrice.Text = (_origPriceCents.Value / 100.0M).ToString("0.00");
            }
            if (_origType == "VARIANTED")
            {
                VariantsHost.Children.Clear();
                foreach (var v in _origVariants)
                    VariantsHost.Children.Add(NewVariantRow(v.name ?? "", (v.priceCents / 100.0M).ToString("0.00")));
            }
            ResetValidationState();
        }
        catch (Exception ex) { await ErrorHandler.MostrarErrorTecnico(ex, "Product – Cargar detalle"); }
    }

  void PkType_SelectedIndexChanged(object s, EventArgs e)
{
    UpdateTitleForType(PkType.SelectedItem as string, IsEdit);
    TogglePanels();
}

    void TogglePanels()
    {
        var t = (PkType.SelectedItem as string) ?? "SIMPLE";
        PanelSimple.IsVisible = (t == "SIMPLE" || t == "COMBO");
        PanelVariants.IsVisible = (t == "VARIANTED");
        ComboCard.IsVisible = (t == "COMBO");
        if (t != "COMBO")
            HideComboError();
    }
    View NewComboItemRow(string qty = "1", string notes = "", bool isRequired = true, int? variantId = null)
    {
        var catsNoCombos = _cats.Where(c => !c.isComboOnly).ToList();
        var frame = new Border
        {
            Stroke = Colors.Transparent,
            StrokeShape = new RoundRectangle { CornerRadius = 12 },
            BackgroundColor = Application.Current.RequestedTheme == AppTheme.Dark ? Color.FromArgb("#1A1A1A") : Color.FromArgb("#F6F2F6"),
            Padding = new Thickness(14),
            Margin = new Thickness(0)
        };

        var container = new VerticalStackLayout
        {
            Spacing = 12
        };

        var pkCat = new Picker { Title = "Categoría", ItemsSource = catsNoCombos, ItemDisplayBinding = new Binding("name"), HeightRequest = 44 };
        var pkProd = new Picker { Title = "Producto", ItemDisplayBinding = new Binding("name"), HeightRequest = 44 };
        var pkVariant = new Picker { Title = "Variante", ItemDisplayBinding = new Binding("name"), HeightRequest = 44, IsVisible = false, IsEnabled = false };
        var eQty = new Entry { Placeholder = "Cantidad", Keyboard = Keyboard.Numeric, Text = string.IsNullOrWhiteSpace(qty) ? "1" : qty, HeightRequest = 44 };
        var swRequired = new Switch { IsToggled = isRequired, HorizontalOptions = LayoutOptions.Start, VerticalOptions = LayoutOptions.Center };
        var requiredLayout = new HorizontalStackLayout
        {
            Spacing = 12,
            Children =
            {
                new Label { Text = "Obligatorio", VerticalOptions = LayoutOptions.Center, FontSize = 13 },
                swRequired
            }
        };
        var eNotes = new Entry { Placeholder = "Notas (opcional)", Text = notes, HeightRequest = 44 };
        var btnRemove = new Button
        {
            Text = "–",
            WidthRequest = 44,
            HeightRequest = 44,
            CornerRadius = 22,
            BackgroundColor = Color.FromArgb("#894164"),
            TextColor = Colors.White,
            FontAttributes = FontAttributes.Bold,
            HorizontalOptions = LayoutOptions.End
        };

        btnRemove.Clicked += (s, e) => ComboItemsHost.Children.Remove(frame);

        var ctx = new ComboRowContext
        {
            comboItemId = null,
            originalComponentProductId = null,
            originalComponentVariantId = null,
            PkCategory = pkCat,
            PkProduct = pkProd,
            PkVariant = pkVariant,
            QtyEntry = eQty,
            RequiredSwitch = swRequired,
            NotesEntry = eNotes,
            PendingVariantId = variantId
        };
        frame.BindingContext = ctx;

        pkCat.SelectedIndexChanged += async (_, __) =>
        {
            if (frame.BindingContext is ComboRowContext rowCtx && pkCat.SelectedItem is CategoryDTO c)
            {
                rowCtx.PkVariant!.ItemsSource = null;
                rowCtx.PkVariant.IsVisible = false;
                rowCtx.PkVariant.IsEnabled = false;
                rowCtx.PkVariant.SelectedItem = null;
                rowCtx.PendingVariantId = null;

                pkProd.ItemsSource = null;
                pkProd.SelectedItem = null;
                pkProd.IsEnabled = false;

                var list = await LoadProductsForCategoryAsync(c);
                pkProd.ItemsSource = list;
                pkProd.IsEnabled = true;
            }
        };

        pkProd.SelectedIndexChanged += async (_, __) =>
        {
            if (frame.BindingContext is ComboRowContext rowCtx)
                await ConfigureVariantPickerAsync(rowCtx);
        };

        var headerGrid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitionCollection
            {
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(new GridLength(44))
            },
            ColumnSpacing = 8
        };
        headerGrid.Add(pkCat);
        Grid.SetColumn(pkCat, 0);
        headerGrid.Add(btnRemove);
        Grid.SetColumn(btnRemove, 1);
        container.Add(headerGrid);

        container.Add(pkProd);
        container.Add(pkVariant);

        var qtyRow = new Grid
        {
            ColumnDefinitions = new ColumnDefinitionCollection
            {
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Star)
            },
            ColumnSpacing = 12
        };
        qtyRow.Add(eQty);
        Grid.SetColumn(eQty, 0);
        qtyRow.Add(requiredLayout);
        Grid.SetColumn(requiredLayout, 1);
        container.Add(qtyRow);

        container.Add(eNotes);

        frame.Content = container;
        return frame;
    }

    async Task ConfigureVariantPickerAsync(ComboRowContext ctx)
    {
        if (ctx.PkVariant == null || ctx.PkProduct == null)
            return;

        ctx.PkVariant.ItemsSource = null;
        ctx.PkVariant.IsVisible = false;
        ctx.PkVariant.IsEnabled = false;
        ctx.PkVariant.SelectedItem = null;

        if (ctx.PkProduct.SelectedItem is not ProductOption prod)
            return;

        if (!string.Equals(prod.type, "VARIANTED", StringComparison.OrdinalIgnoreCase))
        {
            ctx.PendingVariantId = null;
            return;
        }

        var variants = await LoadVariantsForProductAsync(prod.id);
        if (variants == null || variants.Count == 0)
        {
            ctx.PendingVariantId = null;
            return;
        }

        ctx.PkVariant.ItemsSource = variants;
        ctx.PkVariant.IsVisible = true;
        ctx.PkVariant.IsEnabled = true;

        VariantDTO? selected = null;
        if (ctx.PendingVariantId.HasValue)
            selected = variants.FirstOrDefault(v => v.id == ctx.PendingVariantId.Value);

        if (selected == null)
            selected = variants.FirstOrDefault();

        ctx.PkVariant.SelectedItem = selected;
        ctx.PendingVariantId = null;
    }

    void AddComboItem_Clicked(object s, EventArgs e) => ComboItemsHost.Children.Add(NewComboItemRow());



    // === Variantes ===
    View NewVariantRow(string name = "", string price = "")
    {
        var grid = new Grid { ColumnDefinitions = new ColumnDefinitionCollection { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Star), new ColumnDefinition(40) } };
        var eName = new Entry { Placeholder = "Nombre (ej. CH)", Text = name, HeightRequest = 44 };
        var ePrice = new Entry { Placeholder = "Precio (MXN)", Keyboard = Keyboard.Numeric, Text = price, HeightRequest = 44 };
        var btnX = new Button { Text = "–", WidthRequest = 36, HeightRequest = 36 };
        btnX.Clicked += (s, e) => VariantsHost.Children.Remove(grid);
        grid.Add(eName, 0, 0);
        grid.Add(ePrice, 1, 0);
        grid.Add(btnX, 2, 0);
        return grid;
    }
    void AddVariant_Clicked(object s, EventArgs e) => VariantsHost.Children.Add(NewVariantRow());
    async void PickImage_Clicked(object? sender, EventArgs e)
    {
        try
        {
            // Puedes usar MediaPicker.PickPhoto (pide permisos) o FilePicker para limitar tipos
            var file = await FilePicker.PickAsync(new PickOptions
            {
                PickerTitle = "Elige una imagen",
                FileTypes = FilePickerFileType.Images
            });
            if (file == null) return;

            // Validar extensión tamaño
            var extOk = file.FileName.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase)
                        || file.FileName.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase)
                        || file.FileName.EndsWith(".png", StringComparison.OrdinalIgnoreCase);

            if (!extOk) { await DisplayAlert("Imagen", "Elige JPG o PNG.", "OK"); return; }

            using var s = await file.OpenReadAsync();
            if (s.Length > 5 * 1024 * 1024) { await DisplayAlert("Imagen", "Máximo 5 MB.", "OK"); return; }

            // cachear bytes para subir
            using var ms = new MemoryStream();
            await s.CopyToAsync(ms);
            _pickedBytes = ms.ToArray();
            _pickedImage = file;
            _removeImageFlag = false; // ya no se quiere quitar

            // Preview local
            ImgPreview.Source = ImageSource.FromStream(() => new MemoryStream(_pickedBytes));
        }
        catch (Exception ex)
        {
            await ErrorHandler.MostrarErrorTecnico(ex, "Producto – Seleccionar imagen");
        }
    }

    void RemoveImage_Clicked(object? sender, EventArgs e)
    {
        _pickedImage = null;
        _pickedBytes = null;
        _removeImageFlag = true;
        ImgPreview.Source = null;
    }

    void UpdateTitleForType(string? t, bool isEdit)
{
    var tt = (t ?? "").ToUpperInvariant();
    if (isEdit)
        TitleLabel.Text = tt == "COMBO" ? "Editar combo" : "Editar producto";
    else
        TitleLabel.Text = tt == "COMBO" ? "Crear combo"  : "Crear producto";
}


    async Task LoadServerImageAsync(int productId)
    {
        try
        {
            var token = await GetTokenAsync();
            if (string.IsNullOrWhiteSpace(token)) return;

            var baseUrl = Application.Current.Resources["urlbase"].ToString().TrimEnd('/');
            using var http = NewAuthClient(baseUrl, token);
            var ts = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            using var resp = await http.GetAsync($"/api/products/{productId}/image?ts={ts}");
            if (!resp.IsSuccessStatusCode) { ImgPreview.Source = null; return; }

            var bytes = await resp.Content.ReadAsByteArrayAsync();
            ImgPreview.Source = ImageSource.FromStream(() => new MemoryStream(bytes));
        }
        catch { ImgPreview.Source = null; }
    }

    // === Guardar ===
    async void Guardar_Clicked(object s, EventArgs e)
    {
        var isEdit = IsEdit;
        var type = (PkType.SelectedItem as string) ?? "SIMPLE";
        var cat = PkCategory.SelectedItem as CategoryDTO;
        var name = TxtName.Text?.Trim();
        var sku = TxtSku.Text?.Trim();
        var barcode = NormalizeBarcode(TxtBarcode.Text);
        var barcodeChanged = !string.Equals(barcode ?? string.Empty, _origBarcode ?? string.Empty, StringComparison.Ordinal);
        var desc = TxtDesc.Text?.Trim();

        if (cat is null) { await DisplayAlert("Categoría", "Selecciona la categoría.", "OK"); return; }

        ResetValidationState();

        if (string.IsNullOrWhiteSpace(name))
        {
            ShowFieldError(BdrName, ErrName, "Escribe el nombre.");
            await ScrollToAsync(BdrName);
            return;
        }

        decimal? priceMxn = null;
        if (type == "SIMPLE" || type == "COMBO")
        {
            if (string.IsNullOrWhiteSpace(TxtPrice.Text) || !decimal.TryParse(TxtPrice.Text.Replace(',', '.'), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var pMx) || pMx < 0)
            {
                ShowFieldError(BdrPrice, ErrPrice, "Ingresa un precio válido.");
                await ScrollToAsync(BdrPrice);
                return;
            }
            priceMxn = pMx;
        }

        // Variantes input
        List<(string name, int cents)> variantsInput = new();
        if (type == "VARIANTED")
        {
            if (VariantsHost.Children.Count == 0)
            {
                await DisplayAlert("Variantes", "Agrega al menos una variante.", "OK");
                return;
            }

            foreach (var row in VariantsHost.Children.OfType<Grid>())
            {
                var eName = (Entry)row.Children[0];
                var ePrice = (Entry)row.Children[1];
                var vn = eName.Text?.Trim();
                if (string.IsNullOrWhiteSpace(vn)) { await DisplayAlert("Variantes", "Nombre de variante requerido.", "OK"); return; }
                if (string.IsNullOrWhiteSpace(ePrice.Text) || !decimal.TryParse(ePrice.Text.Replace(',', '.'), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var vpmx) || vpmx < 0)
                { await DisplayAlert("Variantes", "Precio de variante inválido.", "OK"); return; }
                variantsInput.Add((vn!, (int)Math.Round(vpmx * 100)));
            }
        }





        if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet)
        { await DisplayAlert("Servidor no disponible", "Sin conexión a Internet.", "OK"); return; }

        SetSaving(true);
        try
        {
            var token = await GetTokenAsync();
            if (string.IsNullOrWhiteSpace(token)) { await AuthHelper.VerificarYRedirigirSiExpirado(this); return; }

            var baseUrl = Application.Current.Resources["urlbase"].ToString().TrimEnd('/');
            using var http = NewAuthClient(baseUrl, token);

            // antes de entrar al if (!isEdit) / else
            HttpResponseMessage resp = new(HttpStatusCode.OK); // valor por defecto "OK"
            string body = "";



            if (!isEdit)
            {
                // 1) Crear con JSON
                HttpResponseMessage respCreate;
                string bodyCreate;

                if (type == "SIMPLE")
                {
                    var dto = new
                    {
                        name = name,
                        categoryId = cat.id,                                // <- number
                        priceCents = (int)Math.Round(priceMxn!.Value * 100),// <- number
                        description = string.IsNullOrWhiteSpace(desc) ? null : desc,
                        sku = string.IsNullOrWhiteSpace(sku) ? null : sku,
                        barcode = barcode
                    };
                    var json = JsonSerializer.Serialize(dto, _jsonWrite);
                    respCreate = await http.PostAsync("/api/products/simple",
                        new StringContent(json, Encoding.UTF8, "application/json"));
                    bodyCreate = await respCreate.Content.ReadAsStringAsync();
                }
                else // VARIANTED
                {
                    var dto = new
                    {
                        name = name,
                        categoryId = cat.id, // <- number
                        description = string.IsNullOrWhiteSpace(desc) ? null : desc,
                        sku = string.IsNullOrWhiteSpace(sku) ? null : sku,
                        barcode = barcode,
                        variants = variantsInput.Select(v => new { name = v.name, priceCents = v.cents }).ToList()
                    };
                    var json = JsonSerializer.Serialize(dto, _jsonWrite);
                    respCreate = await http.PostAsync("/api/products/varianted",
                        new StringContent(json, Encoding.UTF8, "application/json"));
                    bodyCreate = await respCreate.Content.ReadAsStringAsync();
                }
                // --- CREAR COMBO (multipart + items como texto JSON) ---
                // --- CREAR COMBO (JSON + PATCH imagen) ---
                if (type == "COMBO")
                {
                    // 2) Validar categoría que acepte combos
                    var catSel = (CategoryDTO)PkCategory.SelectedItem!;
                    if (!catSel.isComboOnly)
                    {
                        await DisplayAlert("Categoría", "Esta categoría no acepta combos.", "OK");
                        SetSaving(false); return;
                    }

                    // 3) Recolectar piezas (array real)
                    if (!TryCollectComboItems(out var comboInputs, out var comboError))
                    {
                        SetSaving(false);
                        ShowComboError(comboError ?? "Completa los productos del combo.");
                        await ScrollToAsync(ComboCard);
                        return;
                    }
                    HideComboError();

                    // 4) POST JSON puro
                    var dtoCombo = new
                    {
                        name = name,
                        categoryId = catSel.id,
                        priceCents = (int)Math.Round(priceMxn!.Value * 100),
                        description = string.IsNullOrWhiteSpace(desc) ? null : desc,
                        sku = string.IsNullOrWhiteSpace(sku) ? null : sku,
                        barcode = barcode,
                        items = comboInputs.Select(i => new
                        {
                            componentProductId = i.ComponentProductId,
                            componentVariantId = i.ComponentVariantId,
                            quantity = i.Quantity,
                            isRequired = i.IsRequired,
                            notes = i.Notes
                        }).ToList()
                    };

                    var jsonCombo = JsonSerializer.Serialize(dtoCombo, _jsonWrite);
                    respCreate = await http.PostAsync("/api/products/combo",
                        new StringContent(jsonCombo, Encoding.UTF8, "application/json"));
                    bodyCreate = await respCreate.Content.ReadAsStringAsync();

                    if (!respCreate.IsSuccessStatusCode)
                    {
                        var friendly = MapProductError(respCreate.StatusCode, bodyCreate);
                        if (friendly != null) { await DisplayAlert("Aviso", friendly, "OK"); return; }
                        if (respCreate.StatusCode == HttpStatusCode.Unauthorized)
                        { await AuthHelper.VerificarYRedirigirSiExpirado(this); return; }
                        var msgC = ExtractApiError(bodyCreate);
                        await ErrorHandler.MostrarErrorUsuario(string.IsNullOrWhiteSpace(msgC) ? "Error al crear." : msgC);
                        return;
                    }

                    // 5) Si hay imagen, PATCH multipart solo con image
                    var envCreate1 = JsonSerializer.Deserialize<ApiEnvelope<ProductDetailDTO>>(bodyCreate, _json);
                    var newId1 = envCreate1?.data?.id ?? 0;

                    if (newId1 > 0 && _pickedBytes != null)
                    {
                        var formImg = NewForm();
                        AddImageIfAny(formImg); // agrega "image" file
                        var respImg = await http.PatchAsync($"/api/products/{newId1}", formImg);
                        var bodyImg = await respImg.Content.ReadAsStringAsync();
                        if (!respImg.IsSuccessStatusCode)
                        {
                            var msgI = ExtractApiError(bodyImg);
                            await ErrorHandler.MostrarErrorUsuario($"Producto creado pero falló la imagen: {msgI}");
                        }
                    }


                    await DisplayAlert("Listo", "Producto creado.", "OK");
                    await Shell.Current.GoToAsync("..", new Dictionary<string, object> { ["refresh"] = "1" });
                    return;
                }





                if (!respCreate.IsSuccessStatusCode)
                {
                    var friendly = MapProductError(respCreate.StatusCode, bodyCreate);
                    if (friendly != null) { await DisplayAlert("Aviso", friendly, "OK"); return; }
                    if (respCreate.StatusCode == HttpStatusCode.Unauthorized)
                    { await AuthHelper.VerificarYRedirigirSiExpirado(this); return; }
                    var msgC = ExtractApiError(bodyCreate);
                    await ErrorHandler.MostrarErrorUsuario(string.IsNullOrWhiteSpace(msgC) ? "Error al crear." : msgC);
                    return;
                }

                // Tomar el id del creado
                var envCreate = JsonSerializer.Deserialize<ApiEnvelope<ProductDetailDTO>>(bodyCreate, _json);
                var newId = envCreate?.data?.id ?? 0;
                if (newId > 0)
                {
                    var rCheck = await http.GetAsync($"/api/products/{newId}");
                    var bCheck = await rCheck.Content.ReadAsStringAsync();
                    await DisplayAlert("Producto (después de crear)", bCheck, "OK");
                }

                // 2) Si hay imagen, subirla por PATCH multipart
                if (newId > 0 && _pickedBytes != null)
                {
                    var formImg = NewForm();
                    AddImageIfAny(formImg); // agrega "image" con el file
                    var respImg = await http.PatchAsync($"/api/products/{newId}", formImg);
                    var bodyImg = await respImg.Content.ReadAsStringAsync();

                    if (!respImg.IsSuccessStatusCode)
                    {
                        var msgI = ExtractApiError(bodyImg);
                        await ErrorHandler.MostrarErrorUsuario(
                            $"Producto creado pero falló la imagen: {msgI}");
                        // No retornes en error duro; el producto quedó creado.
                    }
                }

                await DisplayAlert("Listo", "Producto creado.", "OK");
                await Shell.Current.GoToAsync("..", new Dictionary<string, object> { ["refresh"] = "1" });
                return;
            }



            else
            {
                // 0) Si el tipo cambió, convertir primero
                bool converted = false;

                if (type != _origType)
                {
                    if (_origType == "SIMPLE" && type == "VARIANTED")
                    {
                        // Requerimos al menos una variante
                        if (variantsInput.Count == 0)
                        {
                            await DisplayAlert("Variantes", "Agrega al menos una variante.", "OK");
                            SetSaving(false);
                            return;
                        }

                        var payload = new
                        {
                            variants = variantsInput.Select(v => new { name = v.name, priceCents = v.cents }).ToList()
                        };

                        var json = JsonSerializer.Serialize(payload, _jsonWrite);
                        var r = await http.PostAsync($"/api/products/{ProductId}/convert-to-varianted",
                            new StringContent(json, Encoding.UTF8, "application/json"));
                        var b = await r.Content.ReadAsStringAsync();

                        if (!r.IsSuccessStatusCode)
                        {
                            var msg = ExtractApiError(b);
                            await ErrorHandler.MostrarErrorUsuario(string.IsNullOrWhiteSpace(msg) ? "No se pudo convertir a VARIANTED." : msg);
                            SetSaving(false);
                            return;
                        }

                        // estado base tras convertir
                        converted = true;
                        _origType = "VARIANTED";
                        _origPriceCents = null;
                        _origVariants = variantsInput.Select(v => new VariantDTO { name = v.name, priceCents = v.cents, isActive = true }).ToList();
                    }
                    else if (_origType == "VARIANTED" && type == "SIMPLE")
                    {
                        // Requerimos precio
                        if (!priceMxn.HasValue)
                        {
                            await DisplayAlert("Precio", "Indica el precio para SIMPLE.", "OK");
                            SetSaving(false);
                            return;
                        }

                        var payload = new { priceCents = (int)Math.Round(priceMxn.Value * 100) };
                        var json = JsonSerializer.Serialize(payload, _jsonWrite);
                        var r = await http.PostAsync($"/api/products/{ProductId}/convert-to-simple",
                            new StringContent(json, Encoding.UTF8, "application/json"));
                        var b = await r.Content.ReadAsStringAsync();

                        if (!r.IsSuccessStatusCode)
                        {
                            var msg = ExtractApiError(b);
                            await ErrorHandler.MostrarErrorUsuario(string.IsNullOrWhiteSpace(msg) ? "No se pudo convertir a SIMPLE." : msg);
                            SetSaving(false);
                            return;
                        }

                        // estado base tras convertir
                        converted = true;
                        _origType = "SIMPLE";
                        _origPriceCents = payload.priceCents;
                        _origVariants.Clear();
                        VariantsHost.Children.Clear();
                    }

  
                }
                                  // === Sincronizar piezas del COMBO en edición ===
                    if (type == "COMBO" && ProductId > 0)
                    {
                    if (!TryCollectComboItems(out var now, out var comboError))
                    {
                        SetSaving(false);
                        ShowComboError(comboError ?? "Completa los productos del combo.");
                        await ScrollToAsync(ComboCard);
                        return;
                    }
                    HideComboError();

                        // 1) Borrar las que ya no están en UI
                        var idsNow = now.Where(x => x.ComboItemId.HasValue)
                                        .Select(x => x.ComboItemId!.Value)
                                        .ToHashSet();

                        var toDelete = _origComboItems
                                        .Where(o => !idsNow.Contains(o.id))
                                        .Select(o => o.id)
                                        .ToList();

                        foreach (var delId in toDelete)
                        {
                            var rDel = await http.DeleteAsync($"/api/products/combo-items/{delId}");
                            if (!rDel.IsSuccessStatusCode)
                            {
                                var bDel = await rDel.Content.ReadAsStringAsync();
                                await DisplayAlert("Aviso",
                                    $"No se pudo eliminar pieza #{delId}: {ExtractApiError(bDel)}", "OK");
                            }
                        }

                        // 2) Actualizar cantidad/notas cuando NO cambió el producto
                        var origById = _origComboItems.ToDictionary(i => i.id, i => i);
                        var changedProductRows = new List<ComboItemInput>();

                        foreach (var row in now.Where(x => x.ComboItemId.HasValue))
                        {
                            var id = row.ComboItemId!.Value;
                            if (!origById.TryGetValue(id, out var orig)) continue;

                            bool sameProduct = orig.componentProductId == row.ComponentProductId;
                            bool sameVariant = orig.componentVariantId == row.ComponentVariantId;

                            if (sameProduct && sameVariant)
                            {
                                bool qtyDiff = orig.quantity != row.Quantity;
                                bool notesDiff = (orig.notes ?? "") != (row.Notes ?? "");
                                bool requiredDiff = orig.isRequired != row.IsRequired;
                                if (qtyDiff || notesDiff || requiredDiff)
                                {
                                    var patch = new { quantity = row.Quantity, notes = row.Notes, isRequired = row.IsRequired };
                                    var json = JsonSerializer.Serialize(patch, _jsonWrite);

                                    var rPat = await http.PatchAsync($"/api/products/combo-items/{id}",
                                                   new StringContent(json, Encoding.UTF8, "application/json"));

                                    if (!rPat.IsSuccessStatusCode)
                                    {
                                        var bPat = await rPat.Content.ReadAsStringAsync();
                                        await DisplayAlert("Aviso",
                                            $"No se pudo actualizar pieza #{id}: {ExtractApiError(bPat)}", "OK");
                                    }
                                }
                            }
                            else
                            {
                                // Si cambió el producto, borraremos y recrearemos
                                changedProductRows.Add(row);
                            }
                        }

                        // 2b) Borrar las que cambiaron de producto (se recrean abajo)
                        foreach (var ch in changedProductRows)
                        {
                            if (!ch.ComboItemId.HasValue)
                                continue;
                            var rDel = await http.DeleteAsync($"/api/products/combo-items/{ch.ComboItemId.Value}");
                            if (!rDel.IsSuccessStatusCode)
                            {
                                var bDel = await rDel.Content.ReadAsStringAsync();
                                await DisplayAlert("Aviso",
                                    $"No se pudo eliminar pieza #{ch.ComboItemId.Value} (para reemplazarla): {ExtractApiError(bDel)}", "OK");
                            }
                        }

                        // 3) Crear nuevas (sin comboItemId) y las reemplazadas
                        var toCreate = now
                            .Where(x => !x.ComboItemId.HasValue
                                     || changedProductRows.Any(c => c.ComboItemId == x.ComboItemId))
                            .Select(x => new
                            {
                                componentProductId = x.ComponentProductId,
                                componentVariantId = x.ComponentVariantId,
                                quantity = x.Quantity,
                                isRequired = x.IsRequired,
                                notes = x.Notes
                            })
                            .ToList();

                        if (toCreate.Count > 0)
                        {
                            var payload = JsonSerializer.Serialize(new { items = toCreate }, _jsonWrite);

                            // Debug útil para el P2003: ver a qué ID le estás pegando y el JSON que mandas.
                            // await DisplayAlert("Debug combo-items",
                            //     $"POST /api/products/{ProductId}/combo-items\n\n{payload}", "OK");

                            var rAdd = await http.PostAsync($"/api/products/{ProductId}/combo-items",
                                            new StringContent(payload, Encoding.UTF8, "application/json"));
                            var bAdd = await rAdd.Content.ReadAsStringAsync();

                            if (!rAdd.IsSuccessStatusCode)
                            {
                                await DisplayAlert("Aviso",
                                    $"No se pudieron agregar nuevas piezas: {ExtractApiError(bAdd)}", "OK");
                            }
                            else
                            {
                                // Verificación visual
                                var rCheck = await http.GetAsync($"/api/products/{ProductId}");
                                var bCheck = await rCheck.Content.ReadAsStringAsync();
                                // await DisplayAlert("Producto (después de guardar)", bCheck, "OK");

                            }
                        }
                    }



                // 1) ¿toco imagen o quiero eliminarla?
                var touchesImage = _removeImageFlag || (_pickedBytes != null);

                if (touchesImage)
                {
                    var form = NewForm();

                    if (!string.IsNullOrWhiteSpace(name) && name != _origName) form.Add(Txt(name), "name");
                    if (cat.id != _origCategoryId) form.Add(Num(cat.id), "categoryId");

                    // OJO: si acabo de convertir a SIMPLE, el precio ya se envió en convert-to-simple.
                    if (!converted && type == "SIMPLE" && !string.IsNullOrWhiteSpace(TxtPrice.Text))
                    {
                        var cents = (int)Math.Round(decimal.Parse(TxtPrice.Text.Replace(',', '.'), System.Globalization.CultureInfo.InvariantCulture) * 100);
                        if (_origPriceCents != cents) form.Add(Num(cents), "priceCents");
                    }

                    if (!string.IsNullOrWhiteSpace(desc) && desc != _origDesc) form.Add(Txt(desc), "description");
                    if (!string.IsNullOrWhiteSpace(sku) && sku != _origSku) form.Add(Txt(sku), "sku");

                    AddImageIfAny(form);

                    resp = await http.PatchAsync($"/api/products/{ProductId}", form);
                    body = await resp.Content.ReadAsStringAsync();
                    if (!resp.IsSuccessStatusCode) goto handle_error;

                    // Si acabo de convertir a VARIANTED ya mandé las variantes en la conversión; no repetir PUT.
                    if (!converted && type == "VARIANTED" && VariantsHost.Children.Count > 0)
                    {
                        var newVariants = new
                        {
                            variants = VariantsHost.Children.OfType<Grid>().Select(row =>
                            {
                                var eName = (Entry)row.Children[0];
                                var ePrice = (Entry)row.Children[1];
                                var cents = (int)Math.Round(decimal.Parse(ePrice.Text.Replace(',', '.'), System.Globalization.CultureInfo.InvariantCulture) * 100);
                                return new { name = eName.Text?.Trim(), priceCents = cents };
                            }).ToList()
                        };
                        var jsonVar = JsonSerializer.Serialize(newVariants, _jsonWrite);
                        resp = await http.PutAsync($"/api/products/{ProductId}/variants",
                            new StringContent(jsonVar, Encoding.UTF8, "application/json"));
                        body = await resp.Content.ReadAsStringAsync();
                    }

                    if (barcodeChanged)
                    {
                        var payload = JsonSerializer.Serialize(new { barcode }, JsonWriteAllowNulls());
                        resp = await http.PatchAsync($"/api/products/{ProductId}",
                            new StringContent(payload, Encoding.UTF8, "application/json"));
                        body = await resp.Content.ReadAsStringAsync();
                        if (!resp.IsSuccessStatusCode) goto handle_error;
                    }
                }
                else
                {
                    var patch = new Dictionary<string, object?>();
                    var patchIncludesNull = false;

                    if (!string.IsNullOrWhiteSpace(name) && name != _origName) patch["name"] = name;
                    if (cat.id != _origCategoryId) patch["categoryId"] = cat.id;

                    // Igual que arriba: si acabo de convertir a SIMPLE, ya envié el precio.
                    if (!converted && type == "SIMPLE" && !string.IsNullOrWhiteSpace(TxtPrice.Text))
                    {
                        var cents = (int)Math.Round(decimal.Parse(TxtPrice.Text.Replace(',', '.'), System.Globalization.CultureInfo.InvariantCulture) * 100);
                        if (_origPriceCents != cents) patch["priceCents"] = cents;
                    }
                    if (!string.IsNullOrWhiteSpace(desc) && desc != _origDesc) patch["description"] = desc;
                    if (!string.IsNullOrWhiteSpace(sku) && sku != _origSku) patch["sku"] = sku;
                    if (barcodeChanged)
                    {
                        patch["barcode"] = barcode;
                        if (barcode == null) patchIncludesNull = true;
                    }

                    if (patch.Count > 0)
                    {
                        var options = patchIncludesNull ? JsonWriteAllowNulls() : _jsonWrite;
                        var json = JsonSerializer.Serialize(patch, options);
                        resp = await http.PatchAsync($"/api/products/{ProductId}",
                            new StringContent(json, Encoding.UTF8, "application/json"));
                        body = await resp.Content.ReadAsStringAsync();
                        if (!resp.IsSuccessStatusCode) goto handle_error;
                    }

                    if (!converted && type == "VARIANTED" && VariantsHost.Children.Count > 0)
                    {
                        var newVariants = new
                        {
                            variants = VariantsHost.Children.OfType<Grid>().Select(row =>
                            {
                                var eName = (Entry)row.Children[0];
                                var ePrice = (Entry)row.Children[1];
                                var cents = (int)Math.Round(decimal.Parse(ePrice.Text.Replace(',', '.'), System.Globalization.CultureInfo.InvariantCulture) * 100);
                                return new { name = eName.Text?.Trim(), priceCents = cents };
                            }).ToList()
                        };
                        var jsonVar = JsonSerializer.Serialize(newVariants, _jsonWrite);
                        resp = await http.PutAsync($"/api/products/{ProductId}/variants",
                            new StringContent(jsonVar, Encoding.UTF8, "application/json"));
                        body = await resp.Content.ReadAsStringAsync();
                    }
                }

            handle_error:
                if (!resp.IsSuccessStatusCode)
                {
                    var friendly = MapProductError(resp.StatusCode, body);
                    if (friendly != null) { await DisplayAlert("Aviso", friendly, "OK"); return; }
                    if (resp.StatusCode == HttpStatusCode.Unauthorized)
                    { await AuthHelper.VerificarYRedirigirSiExpirado(this); return; }
                    if (resp.StatusCode is HttpStatusCode.ServiceUnavailable or HttpStatusCode.GatewayTimeout)
                    { await DisplayAlert("Servidor no disponible", "El servidor no responde.", "OK"); return; }

                    var msg = ExtractApiError(body);
                    await ErrorHandler.MostrarErrorUsuario(string.IsNullOrWhiteSpace(msg) ? "Error al guardar." : msg);
                    return;
                }
            }

            if (!resp.IsSuccessStatusCode)
            {
                if (resp.StatusCode == HttpStatusCode.Unauthorized) { await AuthHelper.VerificarYRedirigirSiExpirado(this); return; }
                if (resp.StatusCode is HttpStatusCode.ServiceUnavailable or HttpStatusCode.GatewayTimeout)
                { await DisplayAlert("Servidor no disponible", "El servidor no responde.", "OK"); return; }

                var msg = ExtractApiError(body);
                await ErrorHandler.MostrarErrorUsuario(string.IsNullOrWhiteSpace(msg) ? "Error al guardar." : msg);
                return;

            }

            await DisplayAlert("Listo", "Cambios guardados.", "OK");
            await Shell.Current.GoToAsync("..", new Dictionary<string, object>
            {
                ["refresh"] = "1"
            });

        }
        catch (TaskCanceledException)
        {
            await DisplayAlert("Servidor no disponible", "Tiempo de espera agotado.", "OK");
        }
        catch (HttpRequestException) { await DisplayAlert("Servidor no disponible", "No se pudo contactar al servidor.", "OK"); }
        catch (Exception ex) { await ErrorHandler.MostrarErrorTecnico(ex, "Product – Guardar"); }
        finally { SetSaving(false); }
    }

    async void Cancelar_Clicked(object s, EventArgs e) => await Shell.Current.GoToAsync("..");

    async Task<List<VariantDTO>> LoadVariantsForProductAsync(int productId)
    {
        if (_variantCache.TryGetValue(productId, out var cached))
            return cached;

        var token = await GetTokenAsync();
        if (string.IsNullOrWhiteSpace(token)) return new();

        var baseUrl = Application.Current.Resources["urlbase"].ToString().TrimEnd('/');
        using var http = NewAuthClient(baseUrl, token);

        var resp = await http.GetAsync($"/api/products/{productId}");
        if (!resp.IsSuccessStatusCode) return new();

        var body = await resp.Content.ReadAsStringAsync();
        var env = JsonSerializer.Deserialize<ApiEnvelope<ProductDetailDTO>>(body, _json);
        var list = env?.data?.variants ?? new();
        _variantCache[productId] = list;
        return list;
    }

    bool TryCollectComboItems(out List<ComboItemInput> items, out string? error)
    {
        items = new();
        error = null;

        foreach (var frame in ComboItemsHost.Children.OfType<Border>())
        {
            if (frame.BindingContext is not ComboRowContext ctx)
                continue;

            if (ctx.PkProduct?.SelectedItem is not ProductOption prod)
            {
                error = "Completa los productos del combo.";
                return false;
            }

            VariantDTO? variantDto = null;
            if (string.Equals(prod.type, "VARIANTED", StringComparison.OrdinalIgnoreCase))
            {
                if (ctx.PkVariant?.SelectedItem is not VariantDTO variant)
                {
                    error = $"Selecciona la variante para {prod.name}.";
                    return false;
                }
                variantDto = variant;
            }

            int qty = 1;
            if (!int.TryParse(ctx.QtyEntry?.Text, out qty) || qty <= 0)
                qty = 1;

            var notes = ctx.NotesEntry?.Text?.Trim();
            if (string.IsNullOrWhiteSpace(notes))
                notes = null;

            var isRequired = ctx.RequiredSwitch?.IsToggled ?? true;

            items.Add(new ComboItemInput(
                ctx.comboItemId,
                prod.id,
                prod.name ?? $"Producto #{prod.id}",
                prod.type ?? string.Empty,
                variantDto?.id,
                qty,
                isRequired,
                notes,
                ctx.originalComponentProductId,
                ctx.originalComponentVariantId));
        }

        if (items.Count == 0)
        {
            error = "Agrega al menos un producto al combo.";
            return false;
        }
        return true;
    }

    void ResetValidationState()
    {
        ResetFieldState(BdrName, ErrName);
        ResetFieldState(BdrPrice, ErrPrice);
        HideComboError();
    }

    void ResetFieldState(Border? border, Label? label)
    {
        if (border != null)
        {
            border.ClearValue(Border.StrokeProperty);
            border.SetAppThemeColor(Border.StrokeProperty, _inputStrokeLight, _inputStrokeDark);
        }
        if (label != null)
            label.IsVisible = false;
    }

    void ShowFieldError(Border border, Label label, string message)
    {
        border.Stroke = _errorStroke;
        label.Text = message;
        label.IsVisible = true;
    }

    void ShowComboError(string message)
    {
        ErrCombo.Text = message;
        ErrCombo.IsVisible = true;
        ComboCard.Stroke = _errorStroke;
    }

    void HideComboError()
    {
        ErrCombo.IsVisible = false;
        ComboCard.ClearValue(Border.StrokeProperty);
        ComboCard.SetAppThemeColor(Border.StrokeProperty, _inputStrokeLight, _inputStrokeDark);
    }

    async Task ScrollToAsync(View? target)
    {
        if (target == null || MainScroll == null)
            return;
        try
        {
            await MainScroll.ScrollToAsync(target, ScrollToPosition.Start, true);
        }
        catch { }
    }
}
