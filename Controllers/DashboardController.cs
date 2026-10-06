using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using MerceditasStore.Models;
using MerceditasStore.Services;

namespace MerceditasStore.Controllers;

public sealed class DashboardController(AppStore store) : AppController
{
    private const string CartSessionKey = "dashboard.cart";

    private DashboardCart GetCart() => HttpContext.Session.GetJson<DashboardCart>(CartSessionKey) ?? new DashboardCart();

    private void SaveCart(DashboardCart cart) => HttpContext.Session.SetJson(CartSessionKey, cart);

    [HttpGet]
    public IActionResult Index()
    {
        var now = DateTime.Now;
        var settings = store.Settings;
        var cart = GetCart();

        var products = store.Inventory.OrderBy(x => x.Name).ToList();
        var customerRows = store.GetCustomerRows();
        var transactions = store.Transactions;

        var today = transactions.Where(t => t.CreatedAt.Date == now.Date).ToList();
        var cashToday = today.Where(t => t.Type == TransactionType.CashSale).Sum(t => t.Amount);
        var utangToday = today.Where(t => t.Type == TransactionType.CreditEntry).Sum(t => t.Amount);
        var paymentsToday = today.Where(t => t.Type == TransactionType.CreditPayment).Sum(t => t.Amount);

        var totalCredit = customerRows.Sum(r => r.Balance);
        var owingCount = customerRows.Count(r => r.Balance > 0);
        var lowStock = products.Where(p => p.Stock <= p.LowStockThreshold).ToList();

        // Drop cart lines whose product was deleted in the meantime.
        var validLines = cart.Lines.Where(l => products.Any(p => p.Id == l.ProductId)).ToList();
        if (validLines.Count != cart.Lines.Count)
        {
            cart.Lines = validLines;
            SaveCart(cart);
        }

        var cartSummary = cart.Lines
            .Select(line =>
            {
                var p = products.First(x => x.Id == line.ProductId);
                return new CartItem
                {
                    LineId = line.LineId,
                    ProductId = line.ProductId,
                    Qty = line.Quantity,
                    Name = p.Name,
                    Unit = p.Unit,
                    UnitPrice = line.UnitPrice,
                    LineTotal = Math.Round(line.Quantity * line.UnitPrice, 2)
                };
            })
            .ToList();

        var vm = new DashboardViewModel
        {
            StoreName = settings.StoreName,
            RestockAmount = settings.DefaultRestockAmount,
            StatCards =
            [
                new StatCard
                {
                    Title = "Today's Cash Sales",
                    Value = Fmt.Peso(cashToday),
                    SubValue = $"Utang {Fmt.Peso(utangToday)} · Payments {Fmt.Peso(paymentsToday)}",
                    Tone = StatTone.Success,
                    Icon = "cash"
                },
                new StatCard
                {
                    Title = "Total Credit (Utang)",
                    Value = Fmt.Peso(totalCredit),
                    SubValue = owingCount == 1 ? "1 customer owing" : $"{owingCount} customers owing",
                    Tone = StatTone.Warning,
                    Icon = "credit-card"
                },
                new StatCard
                {
                    Title = "Low Stock Items",
                    Value = lowStock.Count.ToString(CultureInfo.InvariantCulture),
                    SubValue = lowStock.Count == 0 ? "All items stocked" : string.Join(", ", lowStock.Take(2).Select(p => p.Name)) + (lowStock.Count > 2 ? "…" : ""),
                    Tone = lowStock.Count == 0 ? StatTone.Success : StatTone.Danger,
                    Icon = "down"
                },
                new StatCard
                {
                    Title = "Time",
                    Value = now.ToString("h:mm tt", CultureInfo.InvariantCulture),
                    SubValue = now.ToString("MMMM d, yyyy", CultureInfo.InvariantCulture),
                    Tone = StatTone.Neutral,
                    Icon = "clock",
                    IsClock = true
                }
            ],
            CartSummary = cartSummary,
            CartTotal = cartSummary.Sum(x => x.LineTotal),
            SelectedCustomerId = cart.CustomerId,
            Customers = customerRows
                .OrderBy(r => r.Customer.Name)
                .Select(r => new CustomerOption { Id = r.Customer.Id, Name = r.Customer.Name })
                .ToList(),
            Products = products
                .Select(p => new ProductOption { Id = p.Id, Name = p.Name, Unit = p.Unit, DefaultPrice = p.SellPrice, Stock = p.Stock })
                .ToList(),
            CreditTracker = customerRows
                .Where(r => r.Balance != 0)
                .OrderByDescending(r => r.Balance)
                .Take(6)
                .Select(r => new CreditEntry
                {
                    CustomerId = r.Customer.Id,
                    Customer = r.Customer.Name,
                    TotalBalance = r.Balance,
                    LastActivity = r.LastActivity is null ? "-" : r.LastActivity.Value.ToString("MMM dd", CultureInfo.InvariantCulture)
                })
                .ToList(),
            InventoryTotalCount = products.Count,
            // Show the items that need attention first.
            Inventory = products
                .OrderBy(p => p.Stock <= p.LowStockThreshold ? 0 : 1)
                .ThenBy(p => p.Name)
                .Take(8)
                .Select(p => new InventoryItem
                {
                    Id = p.Id,
                    Name = p.Name,
                    Stock = $"{Fmt.Qty(p.Stock)} {p.Unit}",
                    BuyPrice = $"{Fmt.Peso(p.BuyPrice)}/{p.Unit}",
                    SellPrice = $"{Fmt.Peso(p.SellPrice)}/{p.Unit}",
                    Status = p.Stock <= 0
                        ? InventoryStatus.OutOfStock
                        : (p.Stock <= p.LowStockThreshold ? InventoryStatus.LowStock : InventoryStatus.Healthy)
                })
                .ToList(),
            RecentTransactions = transactions
                .OrderByDescending(t => t.CreatedAt)
                .Take(6)
                .Select(t => new RecentTransaction
                {
                    Time = t.CreatedAt.Date == now.Date
                        ? t.CreatedAt.ToString("h:mm tt", CultureInfo.InvariantCulture)
                        : t.CreatedAt.ToString("MMM d", CultureInfo.InvariantCulture),
                    Title = Fmt.Label(t.Type),
                    Subtitle = string.Join(" · ", new[] { t.CustomerName ?? "", t.Notes ?? "" }.Where(s => !string.IsNullOrWhiteSpace(s))),
                    AmountLabel = Fmt.Peso(t.Amount),
                    Kind = t.Type switch
                    {
                        TransactionType.CreditEntry => TransactionKind.Credit,
                        TransactionType.GCashCashIn => TransactionKind.GCash,
                        TransactionType.CreditPayment => TransactionKind.Payment,
                        _ => TransactionKind.Cash
                    }
                })
                .ToList()
        };

        return View(vm);
    }

    [HttpGet]
    public IActionResult Search(string? q = null)
    {
        var needle = (q ?? "").Trim();
        var vm = new SearchViewModel { Query = needle };

        if (needle.Length > 0)
        {
            vm.Products = store.Inventory
                .Where(p => p.Name.Contains(needle, StringComparison.OrdinalIgnoreCase))
                .OrderBy(p => p.Name)
                .ToList();

            vm.Customers = store.GetCustomerRows()
                .Where(r => r.Customer.Name.Contains(needle, StringComparison.OrdinalIgnoreCase)
                    || (r.Customer.Phone ?? "").Contains(needle, StringComparison.OrdinalIgnoreCase)
                    || (r.Customer.Address ?? "").Contains(needle, StringComparison.OrdinalIgnoreCase))
                .OrderBy(r => r.Customer.Name)
                .ToList();
        }

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult AddToCart(AddToCartRequest req)
    {
        if (req.ProductId is null)
        {
            FlashError("Select an item to add.");
            return RedirectToAction(nameof(Index));
        }

        if (!ModelState.IsValid)
        {
            FlashError("Enter a quantity greater than 0 and a valid price.");
            return RedirectToAction(nameof(Index));
        }

        var product = store.FindProduct(req.ProductId.Value);
        if (product is null)
        {
            FlashError("That item no longer exists.");
            return RedirectToAction(nameof(Index));
        }

        var cart = GetCart();
        var unitPrice = req.UnitPrice ?? product.SellPrice;
        var alreadyInCart = cart.Lines.Where(l => l.ProductId == product.Id).Sum(l => l.Quantity);

        if (alreadyInCart + req.Quantity > product.Stock)
        {
            var left = Math.Max(0, product.Stock - alreadyInCart);
            FlashError(product.Stock <= 0
                ? $"{product.Name} is out of stock."
                : $"Only {Fmt.Qty(left)} {product.Unit} of {product.Name} left to add ({Fmt.Qty(product.Stock)} in stock).");
            return RedirectToAction(nameof(Index));
        }

        var sameLine = cart.Lines.FirstOrDefault(l => l.ProductId == product.Id && l.UnitPrice == unitPrice);
        if (sameLine is not null)
        {
            sameLine.Quantity += req.Quantity;
        }
        else
        {
            cart.Lines.Add(new DashboardCartLine
            {
                ProductId = product.Id,
                Quantity = req.Quantity,
                UnitPrice = unitPrice
            });
        }

        SaveCart(cart);
        return RedirectToAction(nameof(Index));
    }

    /// <summary>Sells the chosen item immediately. The cart is left untouched.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult QuickSale(QuickSaleRequest req)
    {
        if (req.ProductId is null)
        {
            FlashError("Select an item to sell.");
            return RedirectToAction(nameof(Index));
        }

        if (!ModelState.IsValid)
        {
            FlashError("Enter a quantity greater than 0 and a valid price.");
            return RedirectToAction(nameof(Index));
        }

        var product = store.FindProduct(req.ProductId.Value);
        if (product is null)
        {
            FlashError("That item no longer exists.");
            return RedirectToAction(nameof(Index));
        }

        var lines = new List<DashboardCartLine>
        {
            new DashboardCartLine
            {
                ProductId = product.Id,
                Quantity = req.Quantity,
                UnitPrice = req.UnitPrice ?? product.SellPrice
            }
        };

        Flash(store.RecordSale(lines, req.CustomerId, req.AsCredit));
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult RemoveCartLine(Guid lineId)
    {
        var cart = GetCart();
        cart.Lines.RemoveAll(x => x.LineId == lineId);
        SaveCart(cart);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult ClearCart()
    {
        SaveCart(new DashboardCart());
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult CompleteCart(CompleteCartRequest req)
    {
        var cart = GetCart();

        // Remember the chosen customer so it is not lost if the sale cannot go through.
        cart.CustomerId = req.CustomerId;
        SaveCart(cart);

        var result = store.RecordSale(cart.Lines, req.CustomerId, req.AsCredit);
        if (result.Ok) SaveCart(new DashboardCart());

        Flash(result);
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error() => View();
}
