using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FinancesApp_Module_Account.Domain.ValueObjects;

//Rounding is not realistic here.
public readonly record struct Money
{
    public decimal Amount { get; }
    public string Currency { get; } 
    public bool IsZero => Amount == 0m;

    public Money(decimal amount, string currency)
    {
        if (string.IsNullOrWhiteSpace(currency))
            throw new ArgumentException("Currency is required.");

        currency = currency.Trim().ToUpperInvariant();
        if (currency.Length != 3)
            throw new ArgumentException("Currency must be a 3-letter ISO code (e.g., BRL).");

        Amount = Round(amount);
        Currency = currency;
    }

    public Money Add(Money other)
    {
        EnsureSameCurrency(other);
        return new Money(Amount + other.Amount, Currency);
    }

    public Money Subtract(Money other)
    {
        EnsureSameCurrency(other);
        return new Money(Amount - other.Amount, Currency);
    }

    public Money Negate() => new(-Amount, Currency);

    private void EnsureSameCurrency(Money other)
    {
        if (!string.Equals(Currency, other.Currency, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException($"Currency mismatch: {Currency} vs {other.Currency}");
    }

    private static decimal Round(decimal value)
        => Math.Round(value, 2, MidpointRounding.AwayFromZero);

    public override string ToString() => $"{Amount:0.00} {Currency}";
}
