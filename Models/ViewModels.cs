using System.ComponentModel.DataAnnotations;

namespace MerceditasStore.Models;

public sealed class CustomerRow
{
    public Customer Customer { get; set; } = new();
    public decimal Balance { get; set; }
    public DateTime? LastActivity { get; set; }
}

public sealed class CreditIndexViewModel
{
    public List<CustomerRow> Rows { get; set; } = [];
    public decimal TotalOutstanding { get; set; }
    public bool ShowAll { get; set; }
}

public sealed class LedgerRow
{
    public CreditLedgerEntry Entry { get; set; } = new();
    public decimal RunningBalance { get; set; }
}

public sealed class CreditLedgerViewModel
{
    public Guid CustomerId { get; set; }
    public string CustomerName { get; set; } = "";
    public decimal Balance { get; set; }
    public List<LedgerRow> Rows { get; set; } = [];
}

public sealed class CreditEntryForm
{
    [Required(ErrorMessage = "Select a customer.")]
    public Guid? CustomerId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public CreditKind Kind { get; set; } = CreditKind.Utang;

    [Range(0.01, 1_000_000, ErrorMessage = "Enter an amount greater than 0.")]
    public decimal Amount { get; set; }

    [StringLength(140)]
    public string? Note { get; set; }
}

public sealed class TransactionForm
{
    public TransactionType Type { get; set; } = TransactionType.CashSale;

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public Guid? CustomerId { get; set; }

    [Range(0.01, 1_000_000, ErrorMessage = "Enter an amount greater than 0.")]
    public decimal Amount { get; set; }

    [StringLength(140)]
    public string? Notes { get; set; }
}

public sealed class TransactionListViewModel
{
    public List<Transaction> Items { get; set; } = [];
    public string? Query { get; set; }
    public TransactionType? Type { get; set; }
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
    public bool IsFiltered { get; set; }

    public decimal CashSales { get; set; }
    public decimal CreditSales { get; set; }
    public decimal Payments { get; set; }
    public decimal GCash { get; set; }
}

public sealed class InventoryListViewModel
{
    public List<InventoryProduct> Items { get; set; } = [];
    public string? Query { get; set; }
    public bool LowOnly { get; set; }
    public int TotalCount { get; set; }
    public int LowCount { get; set; }
    public decimal StockValue { get; set; }
    public decimal RestockAmount { get; set; } = 10;
}

public sealed class SearchViewModel
{
    public string Query { get; set; } = "";
    public List<InventoryProduct> Products { get; set; } = [];
    public List<CustomerRow> Customers { get; set; } = [];
}

public sealed class SettingsViewModel
{
    public StoreSettings Settings { get; set; } = new();
    public string DataFilePath { get; set; } = "";
    public int CustomerCount { get; set; }
    public int ProductCount { get; set; }
    public int TransactionCount { get; set; }
    public int LedgerCount { get; set; }
}
