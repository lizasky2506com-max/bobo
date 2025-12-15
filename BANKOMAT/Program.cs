using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.Json;

// ---------------------- ТОЧКА ВХОДА ПРОГРАММЫ ----------------------
// Здесь только соединяем части (репозиторий + сервис + UI).
// Это как "композиция" в архитектуре MVC.

var repo = new BankRepository();
repo.Init();              // загрузка/создание файлов JSON

var service = new BankService(repo);   // слой логики
var ui = new ConsoleUi(service);       // слой интерфейса
ui.Run();                              // запуск приложения



// ============================= MODEL ================================
// Модель счёта (один аккаунт в банке)
class Account
{
    public int Id { get; set; }              // внутренний ID
    public string Owner { get; set; } = "";  // владелец
    public string Card { get; set; } = "";   // номер карты (16 цифр)
    public string Pin { get; set; } = "";    // PIN (4 цифры)
    public decimal Balance { get; set; }     // баланс
    public bool Blocked { get; set; }        // заблокирован ли счёт
}

// Модель транзакции (одна операция)
class Transaction
{
    public DateTime Time { get; set; }       // время операции
    public int? FromId { get; set; }         // ID счёта-отправителя
    public int? ToId { get; set; }           // ID счёта-получателя
    public string Type { get; set; } = "";   // тип: DEPOSIT / WITHDRAW / TRANSFER / CHANGE_PIN
    public decimal Amount { get; set; }      // сумма
    public decimal BalanceAfter { get; set; }// баланс отправителя после операции
    public string Note { get; set; } = "";   // примечание
}



// ============================ REPOSITORY ===========================
// Класс, который отвечает ТОЛЬКО за работу с файлами и данными.
// Здесь мы ничего не выводим в консоль, только читаем/пишем JSON.

class BankRepository
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

    public void SaveAccounts()
    {
        string json = JsonSerializer.Serialize(
            Accounts,
            new JsonSerializerOptions { WriteIndented = true }
        );
        File.WriteAllText(_accountsPath, json);
    }

    public void SaveTransactions()
    { 
        string json = JsonSerializer.Serialize(
            Transactions,
            new JsonSerializerOptions { WriteIndented = true }
        );
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



// ===================== SERVICE / LOGIKA (Controller) ===============
// Класс с ЛОГИКОЙ банкомата. Здесь нет Console.WriteLine.
// Только проверки и изменение данных.

class BankService
{
    private readonly BankRepository _repo;
    private readonly CultureInfo _ci = CultureInfo.InvariantCulture;

    public BankService(BankRepository repo)
    {
        _repo = repo;
    }

    // Попытка входа в систему
    public bool TryLogin(
        string card,
        string pin,
        ref int triesLeft,
        out Account? account,
        out string message,
        out bool cardBlocked)
    {
        account = null;
        cardBlocked = false;

        var acc = _repo.FindByCard(card);
        if (acc == null || acc.Blocked)
        {
            message = "Karta nie istnieje lub jest zablokowana.";
            return false;
        }

        if (acc.Pin != pin)
        {
            triesLeft--;
            if (triesLeft <= 0)
            {
                acc.Blocked = true;
                _repo.SaveAccounts();
                cardBlocked = true;
                message = "Zły PIN. Karta została ZABLOKOWANA.";
                return false;
            }

            message = $"Zły PIN. Pozostało prób: {triesLeft}";
            return false;
        }

        account = acc;
        message = $"Zalogowano. Witaj, {acc.Owner}!";
        return true;
    }

    // Внесение средств
    public (bool ok, string? error, string info) Deposit(Account acc, decimal amount)
    {
        if (amount <= 0)
            return (false, "Kwota musi być > 0.", "");

        acc.Balance += amount;
        _repo.SaveAccounts();
        _repo.AddTransaction(new Transaction
        {
            Time = DateTime.Now,
            FromId = acc.Id,
            ToId = null,
            Type = "DEPOSIT",
            Amount = amount,
            BalanceAfter = acc.Balance,
            Note = "Wpłata gotówki"
        });

        string info = $"Wpłacono {amount.ToString("0.00", _ci)} PLN. " +
                      $"Nowe saldo: {acc.Balance.ToString("0.00", _ci)} PLN";
        return (true, null, info);
    }

    // Снятие средств
    public (bool ok, string? error, string info) Withdraw(Account acc, decimal amount)
    {
        if (amount <= 0)
            return (false, "Kwota musi być > 0.", "");
        if (acc.Balance < amount)
            return (false, "Za mało środków.", "");

        acc.Balance -= amount;
        _repo.SaveAccounts();
        _repo.AddTransaction(new Transaction
        {
            Time = DateTime.Now,
            FromId = acc.Id,
            ToId = null,
            Type = "WITHDRAW",
            Amount = amount,
            BalanceAfter = acc.Balance,
            Note = "Wypłata gotówki"
        });

        string info = $"Wypłacono {amount.ToString("0.00", _ci)} PLN. " +
                      $"Nowe saldo: {acc.Balance.ToString("0.00", _ci)} PLN";
        return (true, null, info);
    }

    // Перевод между счетами
    public (bool ok, string? error, string info) Transfer(Account from, string toCard, decimal amount)
    {
        var to = _repo.FindByCard(toCard);
        if (to == null)
            return (false, "Odbiorca nie istnieje.", "");
        if (to.Id == from.Id)
            return (false, "Nie można zrobić przelewu do siebie.", "");
        if (amount <= 0)
            return (false, "Kwota musi być > 0.", "");
        if (from.Balance < amount)
            return (false, "Za mało środków.", "");

        from.Balance -= amount;
        to.Balance += amount;
        _repo.SaveAccounts();
        _repo.AddTransaction(new Transaction
        {
            Time = DateTime.Now,
            FromId = from.Id,
            ToId = to.Id,
            Type = "TRANSFER",
            Amount = amount,
            BalanceAfter = from.Balance,
            Note = $"do karty {to.Card}"
        });

        string info =
            $"Przelano {amount.ToString("0.00", _ci)} PLN do {to.Owner}. " +
            $"Twoje saldo: {from.Balance.ToString("0.00", _ci)} PLN";
        return (true, null, info);
    }

    // Смена PIN
    public (bool ok, string? error, string info) ChangePin(Account acc, string oldPin, string newPin)
    {
        if (oldPin != acc.Pin)
            return (false, "Zły PIN.", "");

        if (!System.Text.RegularExpressions.Regex.IsMatch(newPin, @"^\d{4}$"))
            return (false, "PIN musi składać się z 4 cyfr.", "");

        acc.Pin = newPin;
        _repo.SaveAccounts();
        _repo.AddTransaction(new Transaction
        {
            Time = DateTime.Now,
            FromId = acc.Id,
            ToId = null,
            Type = "CHANGE_PIN",
            Amount = 0m,
            BalanceAfter = acc.Balance,
            Note = "Zmiana PIN"
        });

        return (true, null, "PIN został zmieniony.");
    }

    public IEnumerable<Transaction> GetLastTransactions(Account acc, int count) =>
        _repo.GetLastTransactionsFor(acc.Id, count);
}



// =========================== VIEW / UI =============================
// Класс, который отвечает только за текстовый интерфейс.
// Здесь все Console.WriteLine, цвета, меню со стрелками и т.п.

class ConsoleUi
{
    private readonly BankService _service;
    private readonly CultureInfo _ci = CultureInfo.InvariantCulture;

    public ConsoleUi(BankService service)
    {
        _service = service;
    }

    // "Тема" для банкомата – розовый фон + чёрный текст
    private void ApplyTheme()
    {
        Console.BackgroundColor = ConsoleColor.DarkMagenta; // фон
        Console.ForegroundColor = ConsoleColor.Black;       // основной текст
    }

    public void Run()
    {
        Console.Title = "Bankomat";

        // включаем тему во всём приложении
        ApplyTheme();
        Console.Clear();

        while (true)
        {
            var acc = Login();
            if (acc == null)
                break; // выход из программы (пустой номер карты)
            MainMenu(acc);
        }

        // При окончательном выходе сбрасываем цвета
        Console.ResetColor();
        Console.Clear();
        Console.WriteLine("Do zobaczenia!");
    }

    // ---------- Окно логина ----------
    private Account? Login()
    {
        int tries = 3;

        while (true)
        {
            Header("LOGOWANIE");

            Console.WriteLine("Pusty numer karty → wyjście z programu.");
            Console.WriteLine();
            Console.Write("Numer karty (16 cyfr): ");
            string card = (Console.ReadLine() ?? "").Trim();
            if (string.IsNullOrEmpty(card))
                return null; // пользователь вышел

            Console.Write("PIN (4 cyfry): ");
            string pin = (Console.ReadLine() ?? "").Trim();

            bool blocked;
            if (_service.TryLogin(card, pin, ref tries,
                    out var acc, out string msg, out blocked))
            {
                Info(msg);
                Pause();
                return acc;
            }
            else
            {
                if (blocked)
                {
                    Error(msg);
                    Pause();
                    return null;
                }

                Error(msg);
                Pause();
            }
        }
    }

    // ---------- Главное меню (стрелками) ----------
    private void MainMenu(Account acc)
    {
        string[] options =
        {
            "Saldo",
            "Wpłata",
            "Wypłata",
            "Przelew",
            "Historia (ostatnie 10)",
            "Zmiana PIN",
            "Wyloguj"
        };

        while (true)
        {
            int selected = SelectFromMenu(acc, options);

            switch (selected)
            {
                case 0: ShowBalance(acc); break;
                case 1: DoDeposit(acc); break;
                case 2: DoWithdraw(acc); break;
                case 3: DoTransfer(acc); break;
                case 4: ShowHistory(acc); break;
                case 5: DoChangePin(acc); break;
                case 6: return; // Wyloguj
            }
        }
    }

    // Меню со стрелками ↑ ↓
    private int SelectFromMenu(Account acc, string[] options)
    {
        int index = 0;

        while (true)
        {
            Header("MENU GŁÓWNE");
            Console.WriteLine($"Użytkownik: {acc.Owner}   Saldo: {acc.Balance.ToString("0.00", _ci)} PLN\n");

            for (int i = 0; i < options.Length; i++)
            {
                string prefix = (i == index) ? "> " : "  ";
                Console.WriteLine(prefix + options[i]);
            }

            Console.WriteLine();
            Console.WriteLine("Użyj strzałek ↑ ↓, Enter = wybierz");

            var key = Console.ReadKey(true);
            if (key.Key == ConsoleKey.UpArrow)
            {
                index = (index - 1 + options.Length) % options.Length;
            }
            else if (key.Key == ConsoleKey.DownArrow)
            {
                index = (index + 1) % options.Length;
            }
            else if (key.Key == ConsoleKey.Enter)
            {
                return index;
            }
        }
    }

    // ---------- Операции ----------
    private void ShowBalance(Account acc)
    {
        Header("SALDO");
        Console.WriteLine($"Dostępne środki: {acc.Balance.ToString("0.00", _ci)} PLN");
        Pause();
    }

    private void DoDeposit(Account acc)
    {
        Header("WPŁATA");
        decimal? amt = ReadAmount("Kwota wpłaty");
        if (amt == null) return;

        var (ok, error, info) = _service.Deposit(acc, amt.Value);
        if (!ok) Error(error!); else Info(info);
        Pause();
    }

    private void DoWithdraw(Account acc)
    {
        Header("WYPŁATA");
        decimal? amt = ReadAmount("Kwota wypłaty");
        if (amt == null) return;

        var (ok, error, info) = _service.Withdraw(acc, amt.Value);
        if (!ok) Error(error!); else Info(info);
        Pause();
    }

    private void DoTransfer(Account acc)
    {
        Header("PRZELEW");
        Console.Write("Karta odbiorcy (16 cyfr): ");
        string card = (Console.ReadLine() ?? "").Trim();

        decimal? amt = ReadAmount("Kwota przelewu");
        if (amt == null) return;

        var (ok, error, info) = _service.Transfer(acc, card, amt.Value);
        if (!ok) Error(error!); else Info(info);
        Pause();
    }

    private void ShowHistory(Account acc)
    {
        Header("HISTORIA");
        int count = 0;
        foreach (var t in _service.GetLastTransactions(acc, 10))
        {
            Console.WriteLine(
                $"{t.Time:G} | {t.Type,-10} | {t.Amount.ToString("0.00", _ci),8} PLN | {t.Note} | saldo: {t.BalanceAfter.ToString("0.00", _ci)}");
            count++;
        }

        if (count == 0)
            Console.WriteLine("Brak operacji.");

        Pause();
    }

    private void DoChangePin(Account acc)
    {
        Header("ZMIANA PIN");
        Console.Write("Aktualny PIN: ");
        string oldPin = (Console.ReadLine() ?? "").Trim();
        Console.Write("Nowy PIN (4 cyfry): ");
        string newPin = (Console.ReadLine() ?? "").Trim();

        var (ok, error, info) = _service.ChangePin(acc, oldPin, newPin);
        if (!ok) Error(error!); else Info(info);
        Pause();
    }

    // ---------- Вспомогательные методы UI ----------
    private decimal? ReadAmount(string label)
    {
        Console.Write($"{label} (np. 100 lub 99,99): ");
        string s = (Console.ReadLine() ?? "").Trim().Replace(',', '.');

        if (!decimal.TryParse(s, NumberStyles.Number, CultureInfo.InvariantCulture, out var value))
        {
            Error("Niepoprawna kwota.");
            return null;
        }
        return Math.Round(value, 2);
    }

    // Заголовок экрана – использует нашу "тему"
    private void Header(string title)
    {
        ApplyTheme();   // гарантируем розовый фон и чёрный текст
        Console.Clear();

        Console.WriteLine("=================================");
        Console.WriteLine("            BANKOMAT             ");
        Console.WriteLine("=================================");
        Console.WriteLine(title);
        Console.WriteLine();
    }

    private void Info(string msg)
    {
        Console.ForegroundColor = ConsoleColor.Black;
        Console.WriteLine(msg);
        // фон остаётся розовым
    }

    private void Error(string msg)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine(msg);
        Console.ForegroundColor = ConsoleColor.Black; // возвращаем обычный цвет текста
    }

    private void Pause()
    {
        Console.ForegroundColor = ConsoleColor.Black;
        Console.WriteLine();
        Console.Write("Naciśnij Enter, aby kontynuować...");
        Console.ReadLine();
    }
}
