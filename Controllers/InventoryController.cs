using Microsoft.AspNetCore.Mvc;
using MerceditasStore.Models;
using MerceditasStore.Services;

namespace MerceditasStore.Controllers;

public sealed class InventoryController(AppStore store) : AppController
{
    [HttpGet]
    public IActionResult Index(string? q = null, bool lowOnly = false)
    {
        var all = store.Inventory;
        var items = all.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(q))
        {
            var needle = q.Trim();
            items = items.Where(p => p.Name.Contains(needle, StringComparison.OrdinalIgnoreCase));
        }
        if (lowOnly) items = items.Where(p => p.Stock <= p.LowStockThreshold);

        var vm = new InventoryListViewModel
        {
            Items = items.OrderBy(p => p.Name).ToList(),
            Query = q?.Trim(),
            LowOnly = lowOnly,
            TotalCount = all.Count,
            LowCount = all.Count(p => p.Stock <= p.LowStockThreshold),
            StockValue = all.Sum(p => p.Stock * p.BuyPrice),
            RestockAmount = store.Settings.DefaultRestockAmount
        };
        return View(vm);
    }

    [HttpGet]
    public IActionResult Create() =>
        View(new InventoryProduct { LowStockThreshold = store.Settings.DefaultLowStockThreshold });

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Create(InventoryProduct model)
    {
        CheckDuplicateName(model);
        if (!ModelState.IsValid) return View(model);

        model.Id = Guid.NewGuid();
        store.AddProduct(model);
        FlashSuccess($"Added {model.Name}.");
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public IActionResult Edit(Guid id)
    {
        var p = store.FindProduct(id);
        if (p is null) return RedirectToAction(nameof(Index));
        return View(new InventoryProduct
        {
            Id = p.Id,
            Name = p.Name,
            Unit = p.Unit,
            Stock = p.Stock,
            BuyPrice = p.BuyPrice,
            SellPrice = p.SellPrice,
            LowStockThreshold = p.LowStockThreshold
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Edit(InventoryProduct model)
    {
        CheckDuplicateName(model);
        if (!ModelState.IsValid) return View(model);

        if (store.UpdateProduct(model)) FlashSuccess($"Saved {model.Name}.");
        else FlashError("Product not found.");
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Delete(Guid id)
    {
        if (store.DeleteProduct(id)) FlashSuccess("Product deleted.");
        else FlashError("Product not found.");
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Restock(Guid id, decimal? amount = null, string? returnTo = null)
    {
        Flash(store.Restock(id, amount ?? store.Settings.DefaultRestockAmount));

        if (string.Equals(returnTo, "dashboard", StringComparison.OrdinalIgnoreCase))
            return RedirectToAction("Index", "Dashboard");
        return RedirectToAction(nameof(Index));
    }

    private void CheckDuplicateName(InventoryProduct model)
    {
        var name = (model.Name ?? "").Trim();
        if (name.Length == 0) return;

        var taken = store.Inventory.Any(p => p.Id != model.Id && string.Equals(p.Name.Trim(), name, StringComparison.OrdinalIgnoreCase));
        if (taken) ModelState.AddModelError(nameof(model.Name), "A product with this name already exists.");
    }
}
