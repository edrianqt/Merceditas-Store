using Microsoft.AspNetCore.Mvc;
using MerceditasStore.Models;
using MerceditasStore.Services;

namespace MerceditasStore.Controllers;

public sealed class CreditController(AppStore store) : AppController
{
    [HttpGet]
    public IActionResult Index(bool showAll = false)
    {
        var all = store.GetCustomerRows();
        var vm = new CreditIndexViewModel
        {
            ShowAll = showAll,
            TotalOutstanding = all.Sum(r => r.Balance),
            Rows = all
                .Where(r => showAll || r.Balance != 0)
                .OrderByDescending(r => r.Balance)
                .ThenBy(r => r.Customer.Name)
                .ToList()
        };
        return View(vm);
    }

    [HttpGet]
    public IActionResult Ledger(Guid customerId)
    {
        var c = store.FindCustomer(customerId);
        if (c is null) return RedirectToAction(nameof(Index));

        var running = 0m;
        var rows = store.CreditLedger
            .Where(x => x.CustomerId == customerId)
            .OrderBy(x => x.CreatedAt)
            .Select(e =>
            {
                running += e.Kind == CreditKind.Utang ? e.Amount : -e.Amount;
                return new LedgerRow { Entry = e, RunningBalance = running };
            })
            .ToList();
        rows.Reverse();

        return View(new CreditLedgerViewModel
        {
            CustomerId = c.Id,
            CustomerName = c.Name,
            Balance = running,
            Rows = rows
        });
    }

    [HttpGet]
    public IActionResult Create(Guid? customerId = null, CreditKind kind = CreditKind.Utang)
    {
        LoadCustomers();
        return View(new CreditEntryForm
        {
            CustomerId = customerId,
            Kind = kind,
            CreatedAt = DateTime.Now
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Create(CreditEntryForm model)
    {
        LoadCustomers();
        if (!ModelState.IsValid || model.CustomerId is null) return View(model);

        var result = store.RecordCredit(model.CustomerId.Value, model.Kind, model.Amount, model.Note, model.CreatedAt);
        if (!result.Ok)
        {
            ModelState.AddModelError(nameof(model.Amount), result.Message);
            return View(model);
        }

        FlashSuccess(result.Message);
        return RedirectToAction(nameof(Ledger), new { customerId = model.CustomerId });
    }

    /// <summary>Records a payment for the customer's whole remaining balance.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Settle(Guid customerId)
    {
        var balance = store.GetCustomerCreditBalance(customerId);
        if (balance <= 0)
        {
            FlashError("This customer has no outstanding balance.");
        }
        else
        {
            Flash(store.RecordCredit(customerId, CreditKind.Payment, balance, "Paid in full", DateTime.Now));
        }
        return RedirectToAction(nameof(Ledger), new { customerId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Delete(Guid id, Guid customerId)
    {
        Flash(store.DeleteCreditEntry(id));
        return RedirectToAction(nameof(Ledger), new { customerId });
    }

    private void LoadCustomers()
    {
        ViewBag.Customers = store.GetCustomerRows().OrderBy(r => r.Customer.Name).ToList();
    }
}
