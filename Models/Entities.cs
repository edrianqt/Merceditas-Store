using System.ComponentModel.DataAnnotations;
using System.Globalization;

namespace MerceditasStore.Models;

public sealed class Customer
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required(ErrorMessage = "Name is required."), StringLength(80)]
    public string Name { get; set; } = "";

    [StringLength(40)]
    public string? Phone { get; set; }

    [StringLength(120)]
    public string? Address { get; set; }
}

public sealed class InventoryProduct
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required(ErrorMessage = "Name is required."), StringLength(90)]
    public string Name { get; set; } = "";

    [Required(ErrorMessage = "Unit is required."), StringLength(20)]
    public string Unit { get; set; } = "pc";

    [Range(0, 1_000_000)]
    public decimal Stock { get; set; }

    [Range(0, 1_000_000)]
    public decimal BuyPrice { get; set; }

    [Range(0, 1_000_000)]
    public decimal SellPrice { get; set; }

    [Range(0, 1_000_000)]
    public decimal LowStockThreshold { get; set; } = 5;
}

public enum TransactionType
{
    CashSale = 0,
    CreditEntry = 1,
    GCashCashIn = 2,
    CreditPayment = 3
}

public sealed class TransactionLine
{
    public Guid ProductId { get; set; }
    public string Name { get; set; } = "";
    public string Unit { get; set; } = "";
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}

public sealed class Transaction
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public TransactionType Type { get; set; } = TransactionType.CashSale;

    public Guid? CustomerId { get; set; }
    public string? CustomerName { get; set; }
    public string? Notes { get; set; }
    public decimal Amount { get; set; }

    /// <summary>Items sold. Empty for manually entered transactions.</summary>
    public List<TransactionLine> Lines { get; set; } = [];
}

public enum CreditKind
{
    Utang = 0,
    Payment = 1
}

public sealed class CreditLedgerEntry
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public Guid CustomerId { get; set; }
    public string CustomerName { get; set; } = "";
    public CreditKind Kind { get; set; } = CreditKind.Utang;
    public decimal Amount { get; set; }
    public string? Note { get; set; }

    /// <summary>The matching row on the Transactions page, if any.</summary>
    public Guid? TransactionId { get; set; }
}

public sealed class StoreSettings
{
    [Required(ErrorMessage = "Store name is required."), StringLength(60)]
    public string StoreName { get; set; } = "MERCEDITA'S STORE";

    [Range(0, 1_000_000)]
    public decimal DefaultLowStockThreshold { get; set; } = 5;

    [Range(0.01, 1_000_000)]
    public decimal DefaultRestockAmount { get; set; } = 10;
}

/// <summary>Everything the app saves to disk.</summary>
public sealed class StoreData
{
    public StoreSettings Settings { get; set; } = new();
    public List<Customer> Customers { get; set; } = [];
    public List<InventoryProduct> Inventory { get; set; } = [];
    public List<Transaction> Transactions { get; set; } = [];
    public List<CreditLedgerEntry> CreditLedger { get; set; } = [];
}

public sealed record OpResult(bool Ok, string Message)
{
    public static OpResult Success(string message) => new(true, message);
    public static OpResult Fail(string message) => new(false, message);
}

/// <summary>Display helpers shared by controllers and views.</summary>
public static class Fmt
{
    public static string Peso(decimal value) => "₱" + value.ToString("N2", CultureInfo.InvariantCulture);

    public static string Qty(decimal value) => value.ToString("0.##", CultureInfo.InvariantCulture);

    public static string Label(TransactionType type) => type switch
    {
        TransactionType.CreditEntry => "Utang",
        TransactionType.GCashCashIn => "GCash Cash-In",
        TransactionType.CreditPayment => "Utang Payment",
        _ => "Cash Sale"
    };

    public static string Label(CreditKind kind) => kind == CreditKind.Payment ? "Payment" : "Utang";
}
