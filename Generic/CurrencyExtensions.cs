namespace Imdeliceapp;

static class CurrencyExtensions
{
    public static decimal ToCurrency(this int cents) => cents / 100m;

    public static decimal? ToCurrency(this int? cents)
        => cents.HasValue ? cents.Value / 100m : (decimal?)null;
}
