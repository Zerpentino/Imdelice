using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using Imdeliceapp.Helpers;
using Imdeliceapp.Models;
using Imdeliceapp.Services;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;
using CategoryModel = Imdeliceapp.Models.CategoryDTO;

namespace Imdeliceapp.Pages;

[QueryProperty(nameof(ProductIdQuery), "productId")]
[QueryProperty(nameof(InitialBarcode), "initialBarcode")]
public partial class InventoryInsumoEditorPage : ContentPage
{
    readonly ProductsApi _productsApi = new();
    readonly InventoryApi _inventoryApi = new();
    List<CategoryModel> _categories = new();
    const string InventorySlug = "inventario";
    bool _isSaving;
    byte[]? _imageBytes;
    string? _imageFileName;
    int? _productId;
    bool _productLoaded;
    int? _productCategoryId;
    bool _hasServerImage;
    bool _removeServerImage;
    string? _initialBarcode;

    public InventoryInsumoEditorPage()
    {
        InitializeComponent();
        ApplyModeUi();
    }

    public string? ProductIdQuery
    {
        get => _productId?.ToString();
        set
        {
            if (int.TryParse(value, out var id))
            {
                _productId = id;
                _productLoaded = false;
                ApplyModeUi();
            }
        }
    }

    public string? InitialBarcode
    {
        get => _initialBarcode;
        set
        {
            _initialBarcode = string.IsNullOrWhiteSpace(value) ? null : value;
            ApplyInitialBarcode();
        }
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        ApplyInitialBarcode();
        await EnsureCategoriesAsync();
        if (_productId.HasValue && !_productLoaded)
            await LoadExistingProductAsync(_productId.Value);
    }

    async Task EnsureCategoriesAsync()
    {
        try
        {
            _categories = await _productsApi.ListCategoriesAsync();
        }
        catch (HttpRequestException ex)
        {
            await ErrorHandler.MostrarErrorUsuario(ErrorHandler.ObtenerMensajeHttp(
                new HttpResponseMessage(ex.StatusCode ?? System.Net.HttpStatusCode.InternalServerError),
                ex.Message));
        }
        catch (Exception ex)
        {
            await ErrorHandler.MostrarErrorTecnico(ex, "Inventario – Cargar categorías");
        }
    }

    async void SaveButton_Clicked(object sender, EventArgs e)
    {
        if (_isSaving) return;
        ErrorLabel.IsVisible = false;

        var name = NameEntry.Text?.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            ShowError("Ingresa el nombre del insumo.");
            return;
        }

        var barcode = BarcodeEntry.Text?.Trim();
        var sku = SkuEntry.Text?.Trim();
        var description = DescEditor.Text?.Trim();
        decimal initialQty = 0;

        if (!_productId.HasValue)
        {
            var quantityText = QuantityEntry.Text?.Trim();
            if (!decimal.TryParse(quantityText, out initialQty) || initialQty <= 0)
            {
                ShowError("Captura una cantidad inicial válida.");
                return;
            }
        }

        var category = _categories.FirstOrDefault(c =>
            string.Equals(c.slug, InventorySlug, StringComparison.OrdinalIgnoreCase));
        var categoryId = _productId.HasValue
            ? _productCategoryId ?? category?.id
            : category?.id;

        if (!categoryId.HasValue)
        {
            ShowError("No se encontró la categoría Inventario. Crea una antes de registrar insumos.");
            return;
        }

        _isSaving = true;

        try
        {
            if (_productId.HasValue)
            {
                var updatePayload = new
                {
                    name,
                    categoryId = categoryId.Value,
                    description = string.IsNullOrWhiteSpace(description) ? null : description,
                    sku = string.IsNullOrWhiteSpace(sku) ? null : sku,
                    barcode = string.IsNullOrWhiteSpace(barcode) ? null : barcode,
                    isActive = IsActiveSwitch.IsToggled
                };

                await _productsApi.UpdateProductAsync(_productId.Value, updatePayload);

                if (_imageBytes != null)
                {
                    await _productsApi.UploadImageAsync(_productId.Value, _imageBytes, _imageFileName);
                }
                else if (_removeServerImage)
                {
                    await _productsApi.RemoveImageAsync(_productId.Value);
                }

                await DisplayAlert("Inventario", "Insumo actualizado correctamente.", "OK");
            }
            else
            {
                var payload = new
                {
                    name,
                    categoryId = categoryId.Value,
                    priceCents = 0,
                    description = string.IsNullOrWhiteSpace(description) ? null : description,
                    sku = string.IsNullOrWhiteSpace(sku) ? null : sku,
                    barcode = string.IsNullOrWhiteSpace(barcode) ? null : barcode,
                    isActive = IsActiveSwitch.IsToggled
                };

                var product = await _productsApi.CreateSimpleAsync(payload);
                if (product?.id == null)
                {
                    ShowError("No se pudo crear el insumo.");
                    return;
                }

                if (_imageBytes != null)
                {
                    await _productsApi.UploadImageAsync(product.id, _imageBytes, _imageFileName);
                }

                if (RecordMovementSwitch.IsToggled)
                {
                    var movement = new InventoryMovementRequest
                    {
                        productId = product.id,
                        type = "PURCHASE",
                        quantity = initialQty,
                        reason = "Carga inicial"
                    };
                    await _inventoryApi.CreateMovementAsync(movement);
                }

                await DisplayAlert("Inventario", "Insumo registrado correctamente.", "OK");
            }

            await Shell.Current.GoToAsync("..");
        }
        catch (HttpRequestException ex)
        {
            var message = ErrorHandler.ObtenerMensajeHttp(
                new HttpResponseMessage(ex.StatusCode ?? System.Net.HttpStatusCode.InternalServerError),
                ex.Message);
            ShowError(message);
        }
        catch (Exception ex)
        {
            await ErrorHandler.MostrarErrorTecnico(ex, "Inventario – Registrar insumo");
        }
        finally
        {
            _isSaving = false;
        }
    }

    void ApplyInitialBarcode()
    {
        if (_productId.HasValue)
            return;
        if (string.IsNullOrWhiteSpace(_initialBarcode))
            return;
        if (BarcodeEntry == null)
            return;
        if (string.IsNullOrWhiteSpace(BarcodeEntry.Text))
            BarcodeEntry.Text = _initialBarcode;
    }

    void ShowError(string message)
    {
        ErrorLabel.Text = message;
        ErrorLabel.IsVisible = true;
    }

    async void PickImageButton_Clicked(object sender, EventArgs e)
    {
        try
        {
            var file = await FilePicker.PickAsync(new PickOptions
            {
                PickerTitle = "Selecciona una foto",
                FileTypes = FilePickerFileType.Images
            });
            if (file == null) return;

            var allowed = file.FileName.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) ||
                          file.FileName.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase) ||
                          file.FileName.EndsWith(".png", StringComparison.OrdinalIgnoreCase);
            if (!allowed)
            {
                await DisplayAlert("Imagen", "Elige un archivo JPG o PNG.", "OK");
                return;
            }

            await using var stream = await file.OpenReadAsync();
            if (stream.Length > 5 * 1024 * 1024)
            {
                await DisplayAlert("Imagen", "Máximo 5 MB.", "OK");
                return;
            }

            await using var ms = new MemoryStream();
            await stream.CopyToAsync(ms);
            _imageBytes = ms.ToArray();
            _imageFileName = file.FileName;

            _hasServerImage = false;
            _removeServerImage = false;

            UpdateImagePreview(ImageSource.FromStream(() => new MemoryStream(_imageBytes)));
        }
        catch (Exception ex)
        {
            await ErrorHandler.MostrarErrorTecnico(ex, "Inventario – Seleccionar imagen");
        }
    }

    void RemoveImageButton_Clicked(object sender, EventArgs e)
    {
        _removeServerImage = _productId.HasValue && _hasServerImage;
        _hasServerImage = false;
        _imageBytes = null;
        _imageFileName = null;
        UpdateImagePreview(null);
    }

    async Task LoadExistingProductAsync(int productId)
    {
        try
        {
            var product = await _productsApi.GetProductAsync(productId);
            if (product == null)
            {
                await DisplayAlert("Inventario", "No encontramos el insumo seleccionado.", "OK");
                await Shell.Current.GoToAsync("..");
                return;
            }

            _productCategoryId = product.categoryId;
            NameEntry.Text = product.name ?? string.Empty;
            SkuEntry.Text = product.sku ?? string.Empty;
            BarcodeEntry.Text = product.barcode ?? string.Empty;
            DescEditor.Text = product.description ?? string.Empty;
            IsActiveSwitch.IsToggled = product.isActive;

            _hasServerImage = product.hasImage || !string.IsNullOrWhiteSpace(product.imageUrl);
            _removeServerImage = false;
            _imageBytes = null;
            _imageFileName = null;
            await LoadAndShowServerImageAsync(product.id, product.imageUrl, _hasServerImage);

            _productLoaded = true;
            ApplyModeUi();
        }
        catch (HttpRequestException ex)
        {
            var message = ErrorHandler.ObtenerMensajeHttp(
                new HttpResponseMessage(ex.StatusCode ?? System.Net.HttpStatusCode.InternalServerError),
                ex.Message);
            await ErrorHandler.MostrarErrorUsuario(message);
        }
        catch (Exception ex)
        {
            await ErrorHandler.MostrarErrorTecnico(ex, "Inventario – Cargar insumo");
        }
    }

    void ApplyModeUi()
    {
        var isEdit = _productId.HasValue;
        Title = isEdit ? "Editar insumo" : "Registrar insumo";
        if (HeaderLabel != null)
            HeaderLabel.Text = isEdit ? "Editar insumo" : "Registrar insumo";
        if (SaveButton != null)
            SaveButton.Text = isEdit ? "Guardar cambios" : "Registrar insumo";
        if (QuantityBorder != null)
            QuantityBorder.IsVisible = !isEdit;
        if (MovementGrid != null)
            MovementGrid.IsVisible = !isEdit;
    }

    void UpdateImagePreview(ImageSource? source)
    {
        PreviewImage.Source = source;
        ImagePlaceholderLabel.IsVisible = source == null;
    }

    async Task LoadAndShowServerImageAsync(int productId, string? imageUrl, bool hasImage)
    {
        if (!hasImage)
        {
            UpdateImagePreview(null);
            return;
        }

        try
        {
            var token = await GetTokenAsync();
            if (string.IsNullOrWhiteSpace(token))
            {
                UpdateImagePreview(null);
                return;
            }

            var baseUrlObj = Application.Current?.Resources["urlbase"];
            var baseUrl = baseUrlObj?.ToString()?.TrimEnd('/');
            if (string.IsNullOrWhiteSpace(baseUrl))
            {
                UpdateImagePreview(null);
                return;
            }

            using var http = NewAuthClient(baseUrl, token);
            var path = BuildImagePath(imageUrl, productId);
            if (path == null)
            {
                UpdateImagePreview(null);
                return;
            }

            var resp = await http.GetAsync(path);
            if (!resp.IsSuccessStatusCode)
            {
                UpdateImagePreview(null);
                return;
            }

            var bytes = await resp.Content.ReadAsByteArrayAsync();
            UpdateImagePreview(ImageSource.FromStream(() => new MemoryStream(bytes)));
        }
        catch
        {
            UpdateImagePreview(null);
        }
    }

    static string? BuildImagePath(string? raw, int productId)
    {
        if (!string.IsNullOrWhiteSpace(raw))
        {
            if (raw.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                return raw.Contains("?") ? raw : $"{raw}?ts={DateTimeOffset.UtcNow.ToUnixTimeSeconds()}";

            var path = raw.StartsWith('/') ? raw : "/" + raw;
            if (!path.StartsWith("/api/", StringComparison.OrdinalIgnoreCase))
                path = "/api" + path;
            var sep = path.Contains("?") ? "&" : "?";
            return $"{path}{sep}ts={DateTimeOffset.UtcNow.ToUnixTimeSeconds()}";
        }

        return $"/api/products/{productId}/image?ts={DateTimeOffset.UtcNow.ToUnixTimeSeconds()}";
    }

    static async Task<string?> GetTokenAsync()
    {
        try
        {
            var secure = await SecureStorage.GetAsync("token");
            if (!string.IsNullOrWhiteSpace(secure)) return secure;
        }
        catch
        {
            // ignored
        }

        var pref = Preferences.Default.Get("token", string.Empty);
        return string.IsNullOrWhiteSpace(pref) ? null : pref;
    }

    static HttpClient NewAuthClient(string baseUrl, string token)
    {
        var client = new HttpClient
        {
            BaseAddress = new Uri(baseUrl),
            Timeout = TimeSpan.FromSeconds(20)
        };
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }
}
