using Imdeliceapp.Services;
using System.Collections.ObjectModel;
using System.Linq;
using Imdeliceapp.Models;
using ModelsCategoryDTO = Imdeliceapp.Models.CategoryDTO; // 👈 alias claro
using System.Text.Json;                 // ← JsonSerializer
using System.Collections.Generic;       // ← Dictionary<,>
using Microsoft.Maui.Controls;          // ← ContentPage, ToolbarItem, etc.
using Microsoft.Maui.ApplicationModel;  // ← MainThread, HapticFeedback
using Microsoft.Maui.Graphics;          // ← Color, Colors

namespace Imdeliceapp.Pages;

[QueryProperty(nameof(GroupId), "id")]
public partial class GroupEditorPage : ContentPage
{
    public int GroupId { get; set; } // 0 = crear
    readonly ModifiersApi _api = new();

    public ObservableCollection<ModifierOptionDTO> Options { get; } = new();

    public ObservableCollection<ModelsCategoryDTO> Categories { get; } = new();
    ModelsCategoryDTO? _selectedCategory;
    bool _isSaving;

    void SetSaving(bool v)
    {
        _isSaving = v;
        SavingSpinner.IsVisible = v;
        SavingSpinner.IsRunning = v;

        _saveTI.IsEnabled = !v && IsValid();   // toolbar
        this.IsEnabled = !v;                   // bloquea toda la página (opcional)
    }


    // Normaliza para comparar: quita espacios y usa minúsculas.
    static string Norm(string? s) => (s ?? "").Trim().ToLowerInvariant();

    bool ValidateUniqueOptionNames()
    {
        // Ignora vacíos: eso ya lo valida ValidateOptionsNames()
        var normalized = Options
            .Select((o, idx) => new { idx, key = Norm(o.name) })
            .Where(x => !string.IsNullOrEmpty(x.key))
            .ToList();

        var dup = normalized
            .GroupBy(x => x.key)
            .FirstOrDefault(g => g.Count() > 1);

        if (dup == null) return true;

        // Muestra cuáles chocan
        var nameMostrado = Options[dup.First().idx].name?.Trim() ?? "";
        _ = DisplayAlert("Duplicado", $"Ya existe otra opción con el nombre \"{nameMostrado}\". Cambia los nombres para que sean únicos.", "OK");

        // Lleva scroll a la primera repetida
        var firstIdx = dup.Select(x => x.idx).OrderBy(i => i).Skip(1).First(); // la segunda aparición
        OptsCV.ScrollTo(firstIdx, position: ScrollToPosition.Center, animate: true);
        return false;
    }

    bool _catsLoaded;
    async Task LoadCategoriesAsync()
    {
        if (_catsLoaded) return;
        Categories.Clear();
        // Opción "ninguna"
        Categories.Add(new ModelsCategoryDTO { id = 0, name = "— Ninguna —", isActive = true });

        var cats = await _api.GetCategoriesAsync(true);
        foreach (var c in cats.OrderBy(c => c.name))
            Categories.Add(c);

        _catsLoaded = true;
    }
    // ⬇️ a nivel de clase:
    string _baselineNoActiveJson = "";
    Dictionary<int, bool> _baselineActive = new();

    static string ToNoActiveSnapshot(IEnumerable<ModifierOptionDTO> opts)
    {
        var anon = opts.Select(o => new { o.id, o.name, o.priceExtraCents, o.isDefault, o.position })
                       .OrderBy(x => x.id).ToList();
        return JsonSerializer.Serialize(anon);
    }


    ToolbarItem _saveTI;

    public GroupEditorPage()
    {
        InitializeComponent();
        BindingContext = this;
        _saveTI = ToolbarItems.OfType<ToolbarItem>().First(); // "Guardar"
        HookValidation();

        WireNumericSanitizers();

    }
    bool _dirty;

    void MarkDirty() { _dirty = true; UpdateSaveEnabled(); }
    protected override bool OnBackButtonPressed()
    {
        if (!_dirty) return base.OnBackButtonPressed();
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            if (await DisplayAlert("Sin guardar", "Hay cambios sin guardar. ¿Salir?", "Salir", "Cancelar"))
                await Navigation.PopAsync();
        });
        return true; // intercepta
    }
    void EnforceDefaultForSingleChoice()
    {
        int min = int.TryParse(MinEntry.Text, out var mi) ? mi : 0;
        int? max = int.TryParse(MaxEntry.Text, out var ma) ? ma : null;

        if (min == 1 && max == 1)
        {
            // si hay más de uno en default, deja solo el primero
            var defaults = Options.Where(o => o.isDefault).ToList();
            if (defaults.Count > 1)
                for (int i = 1; i < defaults.Count; i++) defaults[i].isDefault = false;

            // si no hay ninguno en default y hay opciones, marca el primero
            if (Options.Count > 0 && !Options.Any(o => o.isDefault))
                Options[0].isDefault = true;
        }
    }
    void HookValidation()
    {
        NameEntry.TextChanged += (_, __) => UpdateSaveEnabled();
        MinEntry.TextChanged += (_, __) => UpdateSaveEnabled();
        MaxEntry.TextChanged += (_, __) => UpdateSaveEnabled();
        ReqSwitch.Toggled += (_, __) => UpdateSaveEnabled();
        ActiveSwitch.Toggled += (_, __) => UpdateSaveEnabled();
        Options.CollectionChanged += (_, __) => UpdateSaveEnabled();
        NameEntry.TextChanged += (_, __) => _dirty = true;
        MinEntry.TextChanged += (_, __) => _dirty = true;
        MaxEntry.TextChanged += (_, __) => _dirty = true;
        ReqSwitch.Toggled += (_, __) => _dirty = true;
        ActiveSwitch.Toggled += (_, __) => _dirty = true;
        Options.CollectionChanged += (_, __) => _dirty = true;
        MinEntry.TextChanged += (_, __) => { _dirty = true; UpdateSaveEnabled(); EnforceDefaultForSingleChoice(); };
        MaxEntry.TextChanged += (_, __) => { _dirty = true; UpdateSaveEnabled(); EnforceDefaultForSingleChoice(); };
    }

    bool IsValid()
    {
        if (string.IsNullOrWhiteSpace(NameEntry.Text)) return false;
        var min = int.TryParse(MinEntry.Text, out var mi) ? mi : 0;
        int? max = int.TryParse(MaxEntry.Text, out var ma) ? ma : null;
        return !max.HasValue || min <= max.Value;
    }

    void UpdateSaveEnabled() => _saveTI.IsEnabled = IsValid();


    void ApplySingleChoicePreset() { MinEntry.Text = "1"; MaxEntry.Text = "1"; }
    void ApplyMultiChoicePreset() { MinEntry.Text = "0"; MaxEntry.Text = ""; }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        Title = GroupId > 0 ? "Editar grupo" : "Crear grupo";
        await LoadCategoriesAsync();


        if (GroupId > 0)
        {
            await LoadAsync();      // edición
            Title = "Editar grupo"; // asegura el texto tras cargar
        }
        else if (Options.Count == 0)
            ApplySingleChoicePreset();  // ← crea: preset de selección única
    }



    async Task LoadAsync()
    {
        try
        {
            var g = await _api.GetGroupAsync(GroupId);
            if (g == null) { await DisplayAlert("Error", "No encontrado", "OK"); return; }

            Title = $"Grupo #{g.id}";
            NameEntry.Text = g.name;
            DescEntry.Text = g.description;
            MinEntry.Text = g.minSelect.ToString();
            MaxEntry.Text = g.maxSelect?.ToString() ?? "";
            ReqSwitch.IsToggled = g.isRequired;
            ActiveSwitch.IsToggled = g.isActive;
            PositionEntry.Text = g.position.ToString();
            _selectedCategory = null;
            if (g.appliesToCategoryId.HasValue)
                _selectedCategory = Categories.FirstOrDefault(c => c.id == g.appliesToCategoryId.Value);

            CategoryPicker.SelectedItem = _selectedCategory ?? Categories.First(); // “Ninguna”


            Options.Clear();
            foreach (var o in (g.options ?? new()).OrderBy(o => o.position))
                Options.Add(o);
            // ===== baseline para decidir si sólo cambió disponibilidad
            _baselineNoActiveJson = ToNoActiveSnapshot(Options);
            _baselineActive = Options.Where(o => o.id > 0)
                                     .ToDictionary(o => o.id, o => o.isActive);

        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", ex.Message, "OK");
        }
    }

    // ===== Opciones: agregar / quitar / mover / default único =====

    void AddOption_Clicked(object sender, EventArgs e)
    {
        var opt = new ModifierOptionDTO { name = "", priceExtraCents = 0, isActive = true, position = Options.Count };
        Options.Add(opt);
        ReindexOptions();

        // Defer para que el ítem exista visualmente
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            await Task.Delay(50);
            OptsCV.ScrollTo(opt, position: ScrollToPosition.End, animate: true);
        });
    }
    // campos nuevos en GroupEditorPage

    void CategoryPicker_SelectedIndexChanged(object? sender, EventArgs e)
    {
        _selectedCategory = CategoryPicker.SelectedItem as ModelsCategoryDTO;
        MarkDirty();
    }

    void ClearCategory_Clicked(object? sender, EventArgs e)
    {
        CategoryPicker.SelectedIndex = 0; // “Ninguna”
        _selectedCategory = Categories.FirstOrDefault();
        MarkDirty();
    }

    void RemoveOption_Clicked(object sender, EventArgs e)
    {
        if ((sender as Button)?.BindingContext is ModifierOptionDTO opt)
        {
            if (Options.Count <= 1)
            {
                _ = DisplayAlert("No permitido", "Debe existir al menos una opción.", "OK");
                return;
            }
            Options.Remove(opt);
            ReindexOptions();
            MarkDirty();
        }
    }

    bool ValidateOptionsNames()
    {
        var invalid = Options.FirstOrDefault(o => string.IsNullOrWhiteSpace(o.name));
        if (invalid == null) return true;

        DisplayAlert("Falta", "Hay opciones sin nombre.", "OK");
        // si quieres, localiza el índice y haz scroll:
        var index = Options.IndexOf(invalid);
        if (index >= 0) OptsCV.ScrollTo(index, position: ScrollToPosition.Center, animate: true);
        return false;
    }

    void MoveUp_Clicked(object sender, EventArgs e)
    {
        if ((sender as Button)?.BindingContext is not ModifierOptionDTO opt) return;
        var idx = Options.IndexOf(opt);
        if (idx > 0)
        {
            Options.Move(idx, idx - 1);
            ReindexOptions();
        }
    }

    void MoveDown_Clicked(object sender, EventArgs e)
    {
        if ((sender as Button)?.BindingContext is not ModifierOptionDTO opt) return;
        var idx = Options.IndexOf(opt);
        if (idx >= 0 && idx < Options.Count - 1)
        {
            Options.Move(idx, idx + 1);
            ReindexOptions();
        }
    }

    void NormalizePrice(ModifierOptionDTO o)
    {
        if (o.priceExtraCents < 0) o.priceExtraCents = 0;
    }
    void ReindexOptions()
    {
        for (int i = 0; i < Options.Count; i++)
        {
            Options[i].position = i;
            NormalizePrice(Options[i]);
        }
    }

    // Si el grupo es de selección única, permite solo un default
    void Default_Toggled(object sender, ToggledEventArgs e)
    {
        var min = int.TryParse(MinEntry.Text, out var mi) ? mi : 0;
        int? max = int.TryParse(MaxEntry.Text, out var ma) ? ma : null;

        if (min == 1 && max == 1 && e.Value && (sender as Element)?.BindingContext is ModifierOptionDTO current)
        {
            foreach (var o in Options) if (!ReferenceEquals(o, current)) o.isDefault = false;
            // vibración ligera para feedback (Android/iOS soportado)
            try { HapticFeedback.Default.Perform(HapticFeedbackType.Click); } catch { }
        }
    }


    // =================== Guardar ===================

    async void Save_Clicked(object s, EventArgs e)
    {
        if (_isSaving) return;
        SetSaving(true);
        try
        {
            await Task.Yield();
            if (!await ValidateAndScrollAsync()) return;
            if (!ValidateOptionsNames()) return;
            if (!ValidateUniqueOptionNames()) return;


            // lee valores
            int min = int.TryParse(MinEntry.Text, out var mi) ? mi : 0;
            int? max = int.TryParse(MaxEntry.Text, out var ma) ? ma : null;
            bool isActive = ActiveSwitch.IsToggled;
            int position = int.TryParse(PositionEntry.Text, out var pos) ? pos : 0;
            int? appliesToCategoryId = (_selectedCategory != null && _selectedCategory.id > 0) ? _selectedCategory.id : (int?)null;

            if (Options.Count == 0)
            { await DisplayAlert("Falta", "Agrega al menos una opción.", "OK"); return; }

            ReindexOptions();

            if (GroupId == 0)
            {
                var dto = new ModifiersApi.CreateGroupDto
                {
                    name = NameEntry.Text?.Trim() ?? "",
                    description = string.IsNullOrWhiteSpace(DescEntry.Text) ? null : DescEntry.Text.Trim(),
                    minSelect = min,
                    maxSelect = max,
                    isRequired = ReqSwitch.IsToggled,
                    isActive = isActive,
                    position = position,
                    appliesToCategoryId = appliesToCategoryId,
                    options = Options.Select(o => new ModifierOptionDTO
                    {
                        id = o.id,
                        groupId = o.groupId,
                        name = o.name?.Trim() ?? "",
                        priceExtraCents = o.priceExtraCents,
                        isDefault = o.isDefault,
                        isActive = o.isActive,
                        position = o.position
                    }).ToList()
                };

                var newId = await _api.CreateGroupAsync(dto);
                await DisplayAlert("Guardado", $"Creado grupo #{newId}", "OK");
            }
            else
            {
                // 1) Actualiza datos del grupo
                var groupDto = new ModifiersApi.UpdateGroupDto
                {
                    name = NameEntry.Text?.Trim(),
                    description = string.IsNullOrWhiteSpace(DescEntry.Text) ? null : DescEntry.Text.Trim(),
                    minSelect = min,
                    maxSelect = max,
                    isRequired = ReqSwitch.IsToggled,
                    isActive = isActive,
                    position = position,
                    appliesToCategoryId = appliesToCategoryId
                };
                await _api.UpdateGroupAsync(GroupId, groupDto);

                // 2) Decide si hace falta reemplazar TODAS las opciones
                var currentNoActive = ToNoActiveSnapshot(Options);
                var onlyAvailabilityChanged = (currentNoActive == _baselineNoActiveJson);

                if (!onlyAvailabilityChanged)
                {
                    // Hubo cambios en nombre/precio/orden/etc → reemplazar
                    var optsPayload = Options.Select(o => new ModifierOptionDTO
                    {
                        id = o.id,
                        groupId = GroupId,
                        name = o.name?.Trim() ?? "",
                        priceExtraCents = o.priceExtraCents,
                        isDefault = o.isDefault,
                        isActive = o.isActive,
                        position = o.position
                    }).ToList();

                    await _api.ReplaceGroupOptionsAsync(GroupId, optsPayload);

                    // // 3) Reafirma disponibilidad por si el replace la pisó
                    // var changedActive = Options.Where(o => o.id > 0
                    //                     && _baselineActive.TryGetValue(o.id, out var b) && b != o.isActive);
                    // foreach (var o in changedActive)
                    //     await _api.SetOptionActiveAsync(o.id, o.isActive);
                }

                await DisplayAlert("Guardado", "Actualizado", "OK");
            }


            _dirty = false;
            await Navigation.PopAsync();
        }


        catch (Exception ex)
        {
            await DisplayAlert("Error", ex.Message, "OK");
        }
        finally
        {
            SetSaving(false);
        }
    }
    async void OptionActive_Toggled(object sender, ToggledEventArgs e)
    {
        if ((sender as Switch)?.BindingContext is not ModifierOptionDTO opt) return;

        // Si todavía no existe en DB (creación), solo cambia localmente.
        if (opt.id <= 0)
        {
            opt.isActive = e.Value;
            MarkDirty();
            return;
        }

        var nuevo = e.Value;
        var anterior = opt.isActive;

        try
        {
            // UI optimista
            opt.isActive = nuevo;

            await _api.SetOptionActiveAsync(opt.id, nuevo);
            // éxito → no hacemos nada más
        }
        catch (Exception ex)
        {
            // Revertir UI si falla el PATCH
            MainThread.BeginInvokeOnMainThread(() =>
            {
                opt.isActive = anterior;
                if (sender is Switch sw) sw.IsToggled = anterior;
            });

            await DisplayAlert("Error", string.IsNullOrWhiteSpace(ex.Message)
                ? "No se pudo actualizar la disponibilidad."
                : ex.Message, "OK");
        }
    }

    async Task<bool> ValidateAndScrollAsync()
    {
        if (string.IsNullOrWhiteSpace(NameEntry.Text)) { await ScrollToAsync(NameEntry); return false; }
        var min = int.TryParse(MinEntry.Text, out var mi) ? mi : 0;
        int? max = int.TryParse(MaxEntry.Text, out var ma) ? ma : null;
        if (max.HasValue && min > max.Value) { await ScrollToAsync(MaxEntry); return false; }
        return true;
    }
    static string DigitsOnly(string? s) => new string((s ?? "").Where(char.IsDigit).ToArray());

    void WireNumericSanitizers()
    {
        MinEntry.TextChanged += (_, e) => { if (e.NewTextValue != DigitsOnly(e.NewTextValue)) MinEntry.Text = DigitsOnly(e.NewTextValue); };
        MaxEntry.TextChanged += (_, e) => { if (e.NewTextValue != DigitsOnly(e.NewTextValue)) MaxEntry.Text = DigitsOnly(e.NewTextValue); };
        PositionEntry.TextChanged += (_, e) => { if (e.NewTextValue != DigitsOnly(e.NewTextValue)) PositionEntry.Text = DigitsOnly(e.NewTextValue); };

        // Para cada item de Options, engancha precio cuando se agregue
        Options.CollectionChanged += (_, __) =>
        {
            foreach (var o in Options) NormalizePrice(o);
        };
    }

    void OptIsActive_Toggled(object sender, ToggledEventArgs e)
    {
        if ((sender as Switch)?.BindingContext is ModifierOptionDTO opt)
        {
            // Aunque el TwoWay ya lo hace, lo fijamos explícito
            opt.isActive = e.Value;
            MarkDirty();   // habilita Guardar
        }
    }




    async Task ScrollToAsync(VisualElement el)
    {
        el.BackgroundColor = new Color(1f, 0f, 0f, 0.08f);
        await Task.Delay(300);
        el.BackgroundColor = Colors.Transparent;
    }

}
