using System.Text;
using Microsoft.AspNetCore.Mvc;
using MerceditasStore.Models;
using MerceditasStore.Services;

namespace MerceditasStore.Controllers;

public sealed class SettingsController(AppStore store) : AppController
{
    private const long MaxBackupBytes = 20 * 1024 * 1024;

    [HttpGet]
    public IActionResult Index() => View(BuildViewModel(store.Settings));

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Save([Bind(Prefix = "Settings")] StoreSettings settings)
    {
        if (!ModelState.IsValid) return View(nameof(Index), BuildViewModel(settings));

        store.UpdateSettings(settings);
        FlashSuccess("Settings saved.");
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public IActionResult Backup()
    {
        var bytes = Encoding.UTF8.GetBytes(store.ExportJson());
        var name = $"merceditas-store-backup-{DateTime.Now:yyyyMMdd-HHmm}.json";
        return File(bytes, "application/json", name);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Restore(IFormFile? file)
    {
        if (file is null || file.Length == 0)
        {
            FlashError("Choose a backup file first.");
            return RedirectToAction(nameof(Index));
        }
        if (file.Length > MaxBackupBytes)
        {
            FlashError("That file is too large to be a store backup.");
            return RedirectToAction(nameof(Index));
        }

        string json;
        using (var reader = new StreamReader(file.OpenReadStream(), Encoding.UTF8))
        {
            json = await reader.ReadToEndAsync();
        }

        Flash(store.ImportJson(json));
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult ResetDemo()
    {
        store.ResetToDemoData();
        FlashSuccess("Store data replaced with the sample data.");
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult ClearAll()
    {
        store.ClearAllRecords();
        FlashSuccess("All customers, products, transactions and listahan entries were removed.");
        return RedirectToAction(nameof(Index));
    }

    private SettingsViewModel BuildViewModel(StoreSettings settings) => new()
    {
        Settings = settings,
        DataFilePath = store.DataFilePath,
        CustomerCount = store.Customers.Count,
        ProductCount = store.Inventory.Count,
        TransactionCount = store.Transactions.Count,
        LedgerCount = store.CreditLedger.Count
    };
}
