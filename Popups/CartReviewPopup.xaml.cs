using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using CommunityToolkit.Maui.Views;
using Imdeliceapp.Pages;
using Microsoft.Maui.Controls;

namespace Imdeliceapp.Popups;

public partial class CartReviewPopup : Popup
{
    readonly IList<TakeOrderPage.CartEntry> _source;
    readonly CartReviewViewModel _viewModel;

    public CartReviewPopup(IList<TakeOrderPage.CartEntry> source)
    {
        InitializeComponent();
        _source = source;
        _viewModel = new CartReviewViewModel(source, CloseWithResult);
        BindingContext = _viewModel;
    }

    void CloseButton_Clicked(object sender, EventArgs e) => CloseWithResult(new CartPopupResult(CartPopupAction.None));

    void RemoveButton_Clicked(object sender, EventArgs e)
    {
        if (sender is not Button btn || btn.CommandParameter is not TakeOrderPage.CartEntry entry)
            return;

        if (_source.Contains(entry))
            _source.Remove(entry);

        _viewModel.Remove(entry);
    }

    void EditButton_Clicked(object sender, EventArgs e)
    {
        if (sender is not Button btn || btn.CommandParameter is not TakeOrderPage.CartEntry entry)
            return;

        CloseWithResult(new CartPopupResult(CartPopupAction.EditLine, entry));
    }

    void CloseWithResult(CartPopupResult result) => Close(result);
}

class CartReviewViewModel : INotifyPropertyChanged
{
    readonly ObservableCollection<TakeOrderPage.CartEntry> _items;
    readonly Action<CartPopupResult> _close;

    public CartReviewViewModel(IList<TakeOrderPage.CartEntry> source, Action<CartPopupResult> close)
    {
        _close = close;
        _items = new ObservableCollection<TakeOrderPage.CartEntry>(source);
        _items.CollectionChanged += ItemsOnCollectionChanged;

        foreach (var entry in _items)
            entry.PropertyChanged += EntryOnPropertyChanged;

        ContinueCommand = new Command(() => _close(new CartPopupResult(CartPopupAction.None)));
        CheckoutCommand = new Command(() => _close(new CartPopupResult(CartPopupAction.Checkout)), () => HasItems);

        RefreshTotals();
        (CheckoutCommand as Command)?.ChangeCanExecute();
    }

    public ObservableCollection<TakeOrderPage.CartEntry> Items => _items;

    decimal _total;
    public decimal Total
    {
        get => _total;
        private set
        {
            if (_total == value) return;
            _total = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(TotalFormatted));
        }
    }

    public string TotalFormatted => Total.ToString("$0.00", CultureInfo.CurrentCulture);

    public bool HasItems => _items.Count > 0;

    public ICommand ContinueCommand { get; }
    public ICommand CheckoutCommand { get; }

    public void Remove(TakeOrderPage.CartEntry entry)
    {
        if (_items.Contains(entry))
        {
            entry.PropertyChanged -= EntryOnPropertyChanged;
            _items.Remove(entry);
            RefreshTotals();
            OnPropertyChanged(nameof(HasItems));
            (CheckoutCommand as Command)?.ChangeCanExecute();
        }
    }

    void ItemsOnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.NewItems != null)
        {
            foreach (TakeOrderPage.CartEntry entry in e.NewItems)
                entry.PropertyChanged += EntryOnPropertyChanged;
        }

        if (e.OldItems != null)
        {
            foreach (TakeOrderPage.CartEntry entry in e.OldItems)
                entry.PropertyChanged -= EntryOnPropertyChanged;
        }

        RefreshTotals();
        (CheckoutCommand as Command)?.ChangeCanExecute();
        OnPropertyChanged(nameof(HasItems));
    }

    void EntryOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(TakeOrderPage.CartEntry.LineTotal) or nameof(TakeOrderPage.CartEntry.Quantity))
            RefreshTotals();
    }

    public void RefreshTotals()
    {
        Total = _items.Sum(item => item.LineTotal);
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
