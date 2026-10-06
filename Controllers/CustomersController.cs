using Microsoft.AspNetCore.Mvc;
using MerceditasStore.Models;
using MerceditasStore.Services;

namespace MerceditasStore.Controllers;

public sealed class CustomersController(AppStore store) : AppController
{
    [HttpGet]
    public IActionResult Index(string? q = null)
    {
        var rows = store.GetCustomerRows().AsEnumerable();
        if (!string.IsNullOrWhiteSpace(q))
        {
            var needle = q.Trim();
            rows = rows.Where(r => r.Customer.Name.Contains(needle, StringComparison.OrdinalIgnoreCase)
                || (r.Customer.Phone ?? "").Contains(needle, StringComparison.OrdinalIgnoreCase)
                || (r.Customer.Address ?? "").Contains(needle, StringComparison.OrdinalIgnoreCase));
        }

        ViewBag.Query = q?.Trim() ?? "";
        return View(rows.OrderBy(r => r.Customer.Name).ToList());
    }

    [HttpGet]
    public IActionResult Create() => View(new Customer());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Create(Customer model)
    {
        CheckDuplicateName(model);
        if (!ModelState.IsValid) return View(model);

        model.Id = Guid.NewGuid();
        store.AddCustomer(model);
        FlashSuccess($"Added {model.Name}.");
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public IActionResult Edit(Guid id)
    {
        var c = store.FindCustomer(id);
        if (c is null) return RedirectToAction(nameof(Index));
        return View(new Customer { Id = c.Id, Name = c.Name, Phone = c.Phone, Address = c.Address });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Edit(Customer model)
    {
        CheckDuplicateName(model);
        if (!ModelState.IsValid) return View(model);

        if (store.UpdateCustomer(model)) FlashSuccess($"Saved {model.Name}.");
        else FlashError("Customer not found.");
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Delete(Guid id)
    {
        Flash(store.DeleteCustomer(id));
        return RedirectToAction(nameof(Index));
    }

    private void CheckDuplicateName(Customer model)
    {
        var name = (model.Name ?? "").Trim();
        if (name.Length == 0) return;

        var taken = store.Customers.Any(c => c.Id != model.Id && string.Equals(c.Name.Trim(), name, StringComparison.OrdinalIgnoreCase));
        if (taken) ModelState.AddModelError(nameof(model.Name), "A customer with this name already exists.");
    }
}
