using Microsoft.AspNetCore.Mvc;
using MerceditasStore.Models;
using MerceditasStore.Services;

namespace MerceditasStore.Controllers;

public sealed class TransactionsController(AppStore store) : AppController
{
    [HttpGet]
    public IActionResult Index(string? q = null, TransactionType? type = null, DateTime? dateFrom = null, DateTime? dateTo = null)
    {
        var items = store.Transactions.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(q))
        {
            var needle = q.Trim();
            items = items.Where(t => (t.CustomerName ?? "").Contains(needle, StringComparison.OrdinalIgnoreCase)
                || (t.Notes ?? "").Contains(needle, StringComparison.OrdinalIgnoreCase));
        }
        if (type is not null) items = items.Where(t => t.Type == type.Value);
        if (dateFrom is not null) items = items.Where(t => t.CreatedAt.Date >= dateFrom.Value.Date);
        if (dateTo is not null) items = items.Where(t => t.CreatedAt.Date <= dateTo.Value.Date);

        var list = items.OrderByDescending(t => t.CreatedAt).ToList();

        return View(new TransactionListViewModel
        {
            Items = list,
            Query = q?.Trim(),
            Type = type,
            From = dateFrom,
            To = dateTo,
            IsFiltered = !string.IsNullOrWhiteSpace(q) || type is not null || dateFrom is not null || dateTo is not null,
            CashSales = list.Where(t => t.Type == TransactionType.CashSale).Sum(t => t.Amount),
            CreditSales = list.Where(t => t.Type == TransactionType.CreditEntry).Sum(t => t.Amount),
            Payments = list.Where(t => t.Type == TransactionType.CreditPayment).Sum(t => t.Amount),
            GCash = list.Where(t => t.Type == TransactionType.GCashCashIn).Sum(t => t.Amount)
        });
    }

    [HttpGet]
    public IActionResult Create(TransactionType type = TransactionType.CashSale)
    {
        LoadCustomers();
        return View(new TransactionForm { Type = type, CreatedAt = DateTime.Now });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Create(TransactionForm model)
    {
        LoadCustomers();

        var needsCustomer = model.Type == TransactionType.CreditEntry || model.Type == TransactionType.CreditPayment;
        if (needsCustomer && model.CustomerId is null)
            ModelState.AddModelError(nameof(model.CustomerId), "Select a customer for utang and payment entries.");

        if (!ModelState.IsValid) return View(model);

        var result = store.RecordManualTransaction(model.Type, model.CreatedAt, model.Amount, model.Notes, model.CustomerId);
        if (!result.Ok)
        {
            ModelState.AddModelError(nameof(model.Amount), result.Message);
            return View(model);
        }

        FlashSuccess(result.Message);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Delete(Guid id)
    {
        Flash(store.DeleteTransaction(id));
        return RedirectToAction(nameof(Index));
    }

    private void LoadCustomers()
    {
        ViewBag.Customers = store.Customers.OrderBy(c => c.Name).ToList();
    }
}
