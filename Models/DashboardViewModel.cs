namespace MerceditasStore.Models;

public sealed class DashboardViewModel
{
    public string StoreName { get; set; } = "";
    public string SearchPlaceholder { get; set; } = "Search inventory or customers";

    public List<StatCard> StatCards { get; set; } = [];

    public List<CartItem> CartSummary { get; set; } = [];
    public decimal CartTotal { get; set; }
    public Guid? SelectedCustomerId { get; set; }
    public List<CustomerOption> Customers { get; set; } = [];
    public List<ProductOption> Products { get; set; } = [];

    public List<CreditEntry> CreditTracker { get; set; } = [];
    public List<InventoryItem> Inventory { get; set; } = [];
    public int InventoryTotalCount { get; set; }
    public decimal RestockAmount { get; set; } = 10;
    public List<RecentTransaction> RecentTransactions { get; set; } = [];
}

public sealed class StatCard
{
    public string Title { get; set; } = "";
    public string Value { get; set; } = "";
    public string? SubValue { get; set; }
    public StatTone Tone { get; set; } = StatTone.Neutral;
    public string Icon { get; set; } = "dot";
    public bool IsClock { get; set; }
}

public enum StatTone
{
    Neutral = 0,
    Success = 1,
    Warning = 2,
    Danger = 3
}

public sealed class CartItem
{
    public Guid LineId { get; set; }
    public Guid ProductId { get; set; }
    public decimal Qty { get; set; }
    public string Name { get; set; } = "";
    public string Unit { get; set; } = "";
    public decimal UnitPrice { get; set; }
    public decimal LineTotal { get; set; }
}

public sealed class CreditEntry
{
    public string Customer { get; set; } = "";
    public decimal TotalBalance { get; set; }
    public string LastActivity { get; set; } = "";
    public Guid CustomerId { get; set; }
}

public sealed class InventoryItem
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public string Stock { get; set; } = "";
    public string BuyPrice { get; set; } = "";
    public string SellPrice { get; set; } = "";
    public InventoryStatus Status { get; set; } = InventoryStatus.Healthy;
}

public enum InventoryStatus
{
    Healthy = 0,
    LowStock = 1,
    OutOfStock = 2
}

public sealed class RecentTransaction
{
    public string Time { get; set; } = "";
    public string Title { get; set; } = "";
    public string Subtitle { get; set; } = "";
    public string AmountLabel { get; set; } = "";
    public TransactionKind Kind { get; set; } = TransactionKind.Cash;
}

public enum TransactionKind
{
    Cash = 0,
    Credit = 1,
    GCash = 2,
    Payment = 3
}

public sealed class CustomerOption
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
}

public sealed class ProductOption
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public string Unit { get; set; } = "";
    public decimal DefaultPrice { get; set; }
    public decimal Stock { get; set; }
}
