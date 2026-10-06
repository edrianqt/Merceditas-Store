using System.Text.Json;
using System.Text.Json.Serialization;
using MerceditasStore.Models;

namespace MerceditasStore.Services;

/// <summary>
/// Holds all store data and saves it to App_Data/store.json after every change,
/// so nothing is lost when the app is closed.
/// </summary>
public sealed class AppStore
{
    private static readonly JsonSerializerOptions JsonOptions = CreateJsonOptions();

    private readonly object _lock = new();
    private readonly ILogger<AppStore> _logger;
    private StoreData _data;

    public string DataFilePath { get; }

    public AppStore(IWebHostEnvironment env, ILogger<AppStore> logger)
    {
        _logger = logger;
        var folder = Path.Combine(env.ContentRootPath, "App_Data");
        Directory.CreateDirectory(folder);
        DataFilePath = Path.Combine(folder, "store.json");

        _data = Load() ?? CreateDemoData();
        Save();
    }

    private static JsonSerializerOptions CreateJsonOptions()
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true
        };
        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }

    // ---------- Reading ----------

    public StoreSettings Settings
    {
        get
        {
            lock (_lock)
            {
                return new StoreSettings
                {
                    StoreName = _data.Settings.StoreName,
                    DefaultLowStockThreshold = _data.Settings.DefaultLowStockThreshold,
                    DefaultRestockAmount = _data.Settings.DefaultRestockAmount
                };
            }
        }
    }

    public List<Customer> Customers { get { lock (_lock) { return _data.Customers.ToList(); } } }
    public List<InventoryProduct> Inventory { get { lock (_lock) { return _data.Inventory.ToList(); } } }
    public List<Transaction> Transactions { get { lock (_lock) { return _data.Transactions.ToList(); } } }
    public List<CreditLedgerEntry> CreditLedger { get { lock (_lock) { return _data.CreditLedger.ToList(); } } }

    public Customer? FindCustomer(Guid id)
    {
        lock (_lock) { return _data.Customers.FirstOrDefault(x => x.Id == id); }
    }

    public InventoryProduct? FindProduct(Guid id)
    {
        lock (_lock) { return _data.Inventory.FirstOrDefault(x => x.Id == id); }
    }

    public decimal GetCustomerCreditBalance(Guid customerId)
    {
        lock (_lock) { return BalanceOf(customerId); }
    }

    /// <summary>Every customer with their current balance and last ledger activity.</summary>
    public List<CustomerRow> GetCustomerRows()
    {
        lock (_lock)
        {
            return _data.Customers
                .Select(c =>
                {
                    var entries = _data.CreditLedger.Where(e => e.CustomerId == c.Id).ToList();
                    return new CustomerRow
                    {
                        Customer = c,
                        Balance = BalanceOf(c.Id),
                        LastActivity = entries.Count == 0 ? null : entries.Max(e => e.CreatedAt)
                    };
                })
                .ToList();
        }
    }

    public string ExportJson()
    {
        lock (_lock) { return JsonSerializer.Serialize(_data, JsonOptions); }
    }

    private decimal BalanceOf(Guid customerId)
    {
        var total = 0m;
        foreach (var e in _data.CreditLedger)
        {
            if (e.CustomerId != customerId) continue;
            total += e.Kind == CreditKind.Utang ? e.Amount : -e.Amount;
        }
        return total;
    }

    // ---------- Settings ----------

    public void UpdateSettings(StoreSettings updated)
    {
        lock (_lock)
        {
            _data.Settings.StoreName = updated.StoreName.Trim();
            _data.Settings.DefaultLowStockThreshold = updated.DefaultLowStockThreshold;
            _data.Settings.DefaultRestockAmount = updated.DefaultRestockAmount;
            Save();
        }
    }

    // ---------- Customers ----------

    public void AddCustomer(Customer customer)
    {
        lock (_lock)
        {
            customer.Name = customer.Name.Trim();
            _data.Customers.Add(customer);
            Save();
        }
    }

    public bool UpdateCustomer(Customer updated)
    {
        lock (_lock)
        {
            var existing = _data.Customers.FirstOrDefault(x => x.Id == updated.Id);
            if (existing is null) return false;

            existing.Name = updated.Name.Trim();
            existing.Phone = updated.Phone;
            existing.Address = updated.Address;

            // Keep the name shown on old records in step with the rename.
            foreach (var e in _data.CreditLedger.Where(e => e.CustomerId == existing.Id))
                e.CustomerName = existing.Name;
            foreach (var t in _data.Transactions.Where(t => t.CustomerId == existing.Id))
                t.CustomerName = existing.Name;

            Save();
            return true;
        }
    }

    public OpResult DeleteCustomer(Guid id)
    {
        lock (_lock)
        {
            var existing = _data.Customers.FirstOrDefault(x => x.Id == id);
            if (existing is null) return OpResult.Fail("Customer not found.");

            var balance = BalanceOf(id);
            if (balance != 0)
                return OpResult.Fail($"{existing.Name} still has a balance of {Fmt.Peso(balance)}. Settle the listahan before deleting.");

            _data.Customers.Remove(existing);
            _data.CreditLedger.RemoveAll(x => x.CustomerId == id);
            foreach (var t in _data.Transactions.Where(t => t.CustomerId == id))
                t.CustomerId = null;

            Save();
            return OpResult.Success($"Deleted {existing.Name}.");
        }
    }

    // ---------- Inventory ----------

    public void AddProduct(InventoryProduct product)
    {
        lock (_lock)
        {
            product.Name = product.Name.Trim();
            product.Unit = product.Unit.Trim();
            _data.Inventory.Add(product);
            Save();
        }
    }

    public bool UpdateProduct(InventoryProduct updated)
    {
        lock (_lock)
        {
            var existing = _data.Inventory.FirstOrDefault(x => x.Id == updated.Id);
            if (existing is null) return false;

            existing.Name = updated.Name.Trim();
            existing.Unit = updated.Unit.Trim();
            existing.Stock = updated.Stock;
            existing.BuyPrice = updated.BuyPrice;
            existing.SellPrice = updated.SellPrice;
            existing.LowStockThreshold = updated.LowStockThreshold;
            Save();
            return true;
        }
    }

    public bool DeleteProduct(Guid id)
    {
        lock (_lock)
        {
            var removed = _data.Inventory.RemoveAll(x => x.Id == id) > 0;
            if (removed) Save();
            return removed;
        }
    }

    public OpResult Restock(Guid id, decimal amount)
    {
        if (amount <= 0) return OpResult.Fail("Restock amount must be greater than 0.");

        lock (_lock)
        {
            var existing = _data.Inventory.FirstOrDefault(x => x.Id == id);
            if (existing is null) return OpResult.Fail("Product not found.");

            existing.Stock += amount;
            Save();
            return OpResult.Success($"Added {Fmt.Qty(amount)} {existing.Unit} to {existing.Name}. Stock is now {Fmt.Qty(existing.Stock)} {existing.Unit}.");
        }
    }

    // ---------- Sales, transactions and credit ----------

    /// <summary>
    /// Records a sale from the dashboard cart: takes the items out of stock, adds a
    /// transaction and, for utang, adds the amount to the customer's listahan.
    /// </summary>
    public OpResult RecordSale(IReadOnlyList<DashboardCartLine> lines, Guid? customerId, bool asCredit)
    {
        if (lines.Count == 0) return OpResult.Fail("The cart is empty.");

        lock (_lock)
        {
            Customer? customer = null;
            if (customerId is not null)
                customer = _data.Customers.FirstOrDefault(x => x.Id == customerId.Value);

            if (asCredit && customer is null)
                return OpResult.Fail("Select a customer before marking a sale as utang.");

            // Check everything first so a failed sale changes nothing.
            foreach (var group in lines.GroupBy(l => l.ProductId))
            {
                var product = _data.Inventory.FirstOrDefault(x => x.Id == group.Key);
                if (product is null)
                    return OpResult.Fail("An item in the cart no longer exists. Remove it and try again.");

                var wanted = group.Sum(l => l.Quantity);
                if (wanted > product.Stock)
                    return OpResult.Fail($"Not enough {product.Name}: {Fmt.Qty(product.Stock)} {product.Unit} in stock, {Fmt.Qty(wanted)} in the cart.");
            }

            var now = DateTime.Now;
            var tx = new Transaction
            {
                CreatedAt = now,
                Type = asCredit ? TransactionType.CreditEntry : TransactionType.CashSale,
                CustomerId = customer?.Id,
                CustomerName = customer?.Name ?? ""
            };

            foreach (var line in lines)
            {
                var product = _data.Inventory.First(x => x.Id == line.ProductId);
                product.Stock -= line.Quantity;
                tx.Amount += line.Quantity * line.UnitPrice;
                tx.Lines.Add(new TransactionLine
                {
                    ProductId = product.Id,
                    Name = product.Name,
                    Unit = product.Unit,
                    Quantity = line.Quantity,
                    UnitPrice = line.UnitPrice
                });
            }

            tx.Amount = Math.Round(tx.Amount, 2);
            tx.Notes = Shorten(string.Join(", ", tx.Lines.Select(l => $"{Fmt.Qty(l.Quantity)} {l.Unit} {l.Name}")), 140);
            _data.Transactions.Add(tx);

            if (asCredit && customer is not null)
            {
                _data.CreditLedger.Add(new CreditLedgerEntry
                {
                    CreatedAt = now,
                    CustomerId = customer.Id,
                    CustomerName = customer.Name,
                    Kind = CreditKind.Utang,
                    Amount = tx.Amount,
                    Note = tx.Notes,
                    TransactionId = tx.Id
                });
            }

            Save();

            return OpResult.Success(asCredit
                ? $"Added {Fmt.Peso(tx.Amount)} to {customer!.Name}'s listahan."
                : $"Cash sale of {Fmt.Peso(tx.Amount)} recorded.");
        }
    }

    /// <summary>A cash sale or GCash cash-in typed in by hand (no stock change).</summary>
    public OpResult RecordManualTransaction(TransactionType type, DateTime createdAt, decimal amount, string? notes, Guid? customerId)
    {
        if (type == TransactionType.CreditEntry || type == TransactionType.CreditPayment)
        {
            if (customerId is null) return OpResult.Fail("Select a customer for utang and payment entries.");
            var kind = type == TransactionType.CreditEntry ? CreditKind.Utang : CreditKind.Payment;
            return RecordCredit(customerId.Value, kind, amount, notes, createdAt);
        }

        lock (_lock)
        {
            Customer? customer = null;
            if (customerId is not null)
                customer = _data.Customers.FirstOrDefault(x => x.Id == customerId.Value);

            _data.Transactions.Add(new Transaction
            {
                CreatedAt = createdAt,
                Type = type,
                CustomerId = customer?.Id,
                CustomerName = customer?.Name ?? "",
                Notes = notes,
                Amount = Math.Round(amount, 2)
            });
            Save();
            return OpResult.Success($"{Fmt.Label(type)} of {Fmt.Peso(amount)} recorded.");
        }
    }

    /// <summary>Adds utang or a payment to a customer's listahan, with a matching transaction.</summary>
    public OpResult RecordCredit(Guid customerId, CreditKind kind, decimal amount, string? note, DateTime createdAt)
    {
        if (amount <= 0) return OpResult.Fail("Enter an amount greater than 0.");
        amount = Math.Round(amount, 2);

        lock (_lock)
        {
            var customer = _data.Customers.FirstOrDefault(x => x.Id == customerId);
            if (customer is null) return OpResult.Fail("Customer not found.");

            if (kind == CreditKind.Payment)
            {
                var balance = BalanceOf(customerId);
                if (balance <= 0)
                    return OpResult.Fail($"{customer.Name} has no outstanding balance.");
                if (amount > balance)
                    return OpResult.Fail($"Payment is more than {customer.Name}'s balance of {Fmt.Peso(balance)}.");
            }

            var tx = new Transaction
            {
                CreatedAt = createdAt,
                Type = kind == CreditKind.Utang ? TransactionType.CreditEntry : TransactionType.CreditPayment,
                CustomerId = customer.Id,
                CustomerName = customer.Name,
                Notes = note,
                Amount = amount
            };
            _data.Transactions.Add(tx);

            _data.CreditLedger.Add(new CreditLedgerEntry
            {
                CreatedAt = createdAt,
                CustomerId = customer.Id,
                CustomerName = customer.Name,
                Kind = kind,
                Amount = amount,
                Note = note,
                TransactionId = tx.Id
            });

            Save();

            var newBalance = BalanceOf(customerId);
            return OpResult.Success(kind == CreditKind.Utang
                ? $"Added {Fmt.Peso(amount)} utang for {customer.Name}. Balance: {Fmt.Peso(newBalance)}."
                : $"Recorded {Fmt.Peso(amount)} payment from {customer.Name}. Balance: {Fmt.Peso(newBalance)}.");
        }
    }

    /// <summary>Deletes a transaction, puts any sold items back in stock and removes its listahan entry.</summary>
    public OpResult DeleteTransaction(Guid id)
    {
        lock (_lock)
        {
            var result = DeleteTransactionCore(id);
            if (result.Ok) Save();
            return result;
        }
    }

    public OpResult DeleteCreditEntry(Guid id)
    {
        lock (_lock)
        {
            var entry = _data.CreditLedger.FirstOrDefault(x => x.Id == id);
            if (entry is null) return OpResult.Fail("Ledger entry not found.");

            if (entry.TransactionId is not null && _data.Transactions.Any(t => t.Id == entry.TransactionId.Value))
            {
                var result = DeleteTransactionCore(entry.TransactionId.Value);
                if (result.Ok) Save();
                return result;
            }

            _data.CreditLedger.Remove(entry);
            Save();
            return OpResult.Success("Ledger entry deleted.");
        }
    }

    private OpResult DeleteTransactionCore(Guid id)
    {
        var tx = _data.Transactions.FirstOrDefault(x => x.Id == id);
        if (tx is null) return OpResult.Fail("Transaction not found.");

        var restored = 0;
        foreach (var line in tx.Lines)
        {
            var product = _data.Inventory.FirstOrDefault(x => x.Id == line.ProductId);
            if (product is null) continue;
            product.Stock += line.Quantity;
            restored++;
        }

        _data.CreditLedger.RemoveAll(x => x.TransactionId == id);
        _data.Transactions.Remove(tx);

        return OpResult.Success(restored > 0
            ? $"{Fmt.Label(tx.Type)} of {Fmt.Peso(tx.Amount)} deleted and its items returned to stock."
            : $"{Fmt.Label(tx.Type)} of {Fmt.Peso(tx.Amount)} deleted.");
    }

    // ---------- Backup, restore and reset ----------

    public OpResult ImportJson(string json)
    {
        StoreData? imported;
        try
        {
            imported = JsonSerializer.Deserialize<StoreData>(json, JsonOptions);
        }
        catch (JsonException)
        {
            return OpResult.Fail("That file is not a valid backup.");
        }

        if (imported is null) return OpResult.Fail("That file is not a valid backup.");

        Normalize(imported);
        if (imported.Customers.Count == 0 && imported.Inventory.Count == 0 && imported.Transactions.Count == 0)
            return OpResult.Fail("That backup has no customers, products or transactions in it.");

        lock (_lock)
        {
            _data = imported;
            Save();
        }
        return OpResult.Success("Backup restored.");
    }

    public void ResetToDemoData()
    {
        lock (_lock)
        {
            var settings = _data.Settings;
            _data = CreateDemoData();
            _data.Settings = settings;
            Save();
        }
    }

    public void ClearAllRecords()
    {
        lock (_lock)
        {
            _data = new StoreData { Settings = _data.Settings };
            Save();
        }
    }

    // ---------- File handling ----------

    private StoreData? Load()
    {
        if (!File.Exists(DataFilePath)) return null;

        try
        {
            var data = JsonSerializer.Deserialize<StoreData>(File.ReadAllText(DataFilePath), JsonOptions);
            if (data is null) return null;
            Normalize(data);
            return data;
        }
        catch (Exception ex) when (ex is JsonException || ex is IOException || ex is UnauthorizedAccessException)
        {
            // Never overwrite a file we could not read: keep a copy of it first.
            _logger.LogError(ex, "Could not read {Path}. Keeping a copy and starting with demo data.", DataFilePath);
            try
            {
                var copy = Path.Combine(Path.GetDirectoryName(DataFilePath)!, $"store.unreadable-{DateTime.Now:yyyyMMdd-HHmmss}.json");
                File.Copy(DataFilePath, copy, overwrite: true);
            }
            catch (Exception copyEx) when (copyEx is IOException || copyEx is UnauthorizedAccessException)
            {
                _logger.LogError(copyEx, "Could not copy the unreadable data file.");
            }
            return null;
        }
    }

    private void Save()
    {
        try
        {
            var temp = DataFilePath + ".tmp";
            File.WriteAllText(temp, JsonSerializer.Serialize(_data, JsonOptions));
            File.Move(temp, DataFilePath, overwrite: true);
        }
        catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
        {
            _logger.LogError(ex, "Could not save store data to {Path}.", DataFilePath);
        }
    }

    private static void Normalize(StoreData data)
    {
        data.Settings ??= new StoreSettings();
        data.Customers ??= new List<Customer>();
        data.Inventory ??= new List<InventoryProduct>();
        data.Transactions ??= new List<Transaction>();
        data.CreditLedger ??= new List<CreditLedgerEntry>();
        if (string.IsNullOrWhiteSpace(data.Settings.StoreName)) data.Settings.StoreName = "MERCEDITA'S STORE";
        if (data.Settings.DefaultRestockAmount <= 0) data.Settings.DefaultRestockAmount = 10;
        foreach (var t in data.Transactions) t.Lines ??= new List<TransactionLine>();
    }

    private static string Shorten(string text, int max) =>
        text.Length <= max ? text : text[..(max - 1)] + "…";

    private static StoreData CreateDemoData()
    {
        var jose = new Customer { Name = "Mang Jose" };
        var tess = new Customer { Name = "Aling Tess" };

        var data = new StoreData();
        data.Customers.AddRange([jose, tess]);

        data.Inventory.AddRange(
        [
            new InventoryProduct { Name = "Rice (Sinandomeng)", Unit = "kg", Stock = 45, BuyPrice = 47.0m, SellPrice = 58.0m, LowStockThreshold = 8 },
            new InventoryProduct { Name = "Lucky Me! Pancit Canton", Unit = "pc", Stock = 12, BuyPrice = 12.50m, SellPrice = 16.00m, LowStockThreshold = 6 },
            new InventoryProduct { Name = "Nescafe Original", Unit = "sachet", Stock = 18, BuyPrice = 8.0m, SellPrice = 10.0m, LowStockThreshold = 20 }
        ]);

        data.Transactions.Add(new Transaction
        {
            CreatedAt = DateTime.Today.AddHours(9).AddMinutes(45),
            Type = TransactionType.CashSale,
            CustomerName = "",
            Notes = "1 kg Rice, 3 Eggs",
            Amount = 82m
        });

        AddDemoCredit(data, jose, DateTime.Today.AddDays(-2).AddHours(16), 450m, "Opening balance");
        AddDemoCredit(data, tess, DateTime.Today.AddHours(10).AddMinutes(10), 125m, "Sunsilk sachet, Surf pouch, rice");

        return data;
    }

    private static void AddDemoCredit(StoreData data, Customer customer, DateTime when, decimal amount, string note)
    {
        var tx = new Transaction
        {
            CreatedAt = when,
            Type = TransactionType.CreditEntry,
            CustomerId = customer.Id,
            CustomerName = customer.Name,
            Notes = note,
            Amount = amount
        };
        data.Transactions.Add(tx);
        data.CreditLedger.Add(new CreditLedgerEntry
        {
            CreatedAt = when,
            CustomerId = customer.Id,
            CustomerName = customer.Name,
            Kind = CreditKind.Utang,
            Amount = amount,
            Note = note,
            TransactionId = tx.Id
        });
    }
}
