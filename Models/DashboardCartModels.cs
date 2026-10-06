using System.ComponentModel.DataAnnotations;

namespace MerceditasStore.Models;

public sealed class DashboardCart
{
    public List<DashboardCartLine> Lines { get; set; } = [];
    public Guid? CustomerId { get; set; }
}

public sealed class DashboardCartLine
{
    public Guid LineId { get; set; } = Guid.NewGuid();
    public Guid ProductId { get; set; }
    public decimal Quantity { get; set; } = 1;
    public decimal UnitPrice { get; set; }
}

public sealed class AddToCartRequest
{
    [Required]
    public Guid? ProductId { get; set; }

    [Range(0.01, 1_000_000)]
    public decimal Quantity { get; set; } = 1;

    [Range(0, 1_000_000)]
    public decimal? UnitPrice { get; set; }
}

/// <summary>Sells one item straight away, without going through the cart.</summary>
public sealed class QuickSaleRequest
{
    [Required]
    public Guid? ProductId { get; set; }

    [Range(0.01, 1_000_000)]
    public decimal Quantity { get; set; } = 1;

    [Range(0, 1_000_000)]
    public decimal? UnitPrice { get; set; }

    public Guid? CustomerId { get; set; }
    public bool AsCredit { get; set; }
}

public sealed class CompleteCartRequest
{
    public Guid? CustomerId { get; set; }
    public bool AsCredit { get; set; }
}
