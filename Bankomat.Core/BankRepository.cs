using System.Text.Json;

namespace Bankomat.Core;

/// <summary>
/// Репозиторий, отвечающий только за работу с файлами и хранение данных.
/// </summary>
public class BankRepository
{
    private readonly string _dataDir =
        Path.Combine(AppContext.BaseDirectory, "data");

    private readonly string _accountsPath;
    private readonly string _txPath;

    public List<Account> Accounts { get; private set; } = new();
    public List<Transaction> Transactions { get; private set; } = new();

    public BankRepository()
    {
        _accountsPath = Path.Combine(_dataDir, "accounts.json");
        _txPath = Path.Combine(_dataDir, "transactions.json");
    }

    // Инициализация файлов и стартовых данных
    public void Init()
    {
        if (!Directory.Exists(_dataDir))
            Directory.CreateDirectory(_dataDir);

        // Счета
        if (!File.Exists(_accountsPath))
        {
            // Стартовые аккаунты (можно поменять под себя)
            Accounts = new List<Account>
            {
                new Account
                {
                    Id = 1,
                    Owner = "Liza",
                    Card = "1111222233334444",
                    Pin = "1234",
                    Balance = 1500m,
                    Blocked = false
                },
                new Account
                {
                    Id = 2,
                    Owner = "Anna",
                    Card = "5555666677778888",
                    Pin = "5678",
                    Balance = 500m,
                    Blocked = false
                },
                new Account
                {
                    Id = 3,
                    Owner = "Ola",
                    Card = "9999000011112222",
                    Pin = "0000",
                    Balance = 200m,
                    Blocked = false
                }
            };
            SaveAccounts();
        }
        else
        {
            string json = File.ReadAllText(_accountsPath);
            Accounts = JsonSerializer.Deserialize<List<Account>>(json)
                       ?? new List<Account>();
        }

        // Транзакции
        if (!File.Exists(_txPath))
        {
            Transactions = new List<Transaction>();
            SaveTransactions();
        }
        else
        {
            string json = File.ReadAllText(_txPath);
            Transactions = JsonSerializer.Deserialize<List<Transaction>>(json)
                           ?? new List<Transaction>();
        }
    }

    // Сохранение счетов в файл
    public void SaveAccounts()
    {
        string json = JsonSerializer.Serialize(Accounts, new JsonSerializerOptions
        {
            WriteIndented = true
        });
        File.WriteAllText(_accountsPath, json);
    }

    // Сохранение транзакций в файл
    public void SaveTransactions()
    {
        string json = JsonSerializer.Serialize(Transactions, new JsonSerializerOptions
        {
            WriteIndented = true
        });
        File.WriteAllText(_txPath, json);
    }

    // Поиск счёта по номеру карты
    public Account? FindByCard(string card) =>
        Accounts.Find(a => a.Card == card);

    // Поиск счёта по ID
    public Account? FindById(int id) =>
        Accounts.Find(a => a.Id == id);

    // Добавить транзакцию и сразу сохранить
    public void AddTransaction(Transaction tx)
    {
        Transactions.Add(tx);
        SaveTransactions();
    }

    // Получить последние N транзакций по счёту
    public IEnumerable<Transaction> GetLastTransactionsFor(int accountId, int count)
    {
        int shown = 0;
        for (int i = Transactions.Count - 1; i >= 0 && shown < count; i--)
        {
            var t = Transactions[i];
            bool related =
                (t.FromId == accountId) ||
                (t.ToId == accountId);

            if (!related) continue;
            shown++;
            yield return t;
        }
    }
}
