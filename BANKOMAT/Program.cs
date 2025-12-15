using System.Globalization;
using Bankomat.Core;

// ---------------------- ТОЧКА ВХОДА ПРОГРАММЫ ----------------------
// Здесь только соединяем части (репозиторий + сервис + UI).
// Это как "композиция" в архитектуре MVC.

var repo = new BankRepository();
repo.Init();              // загрузка/создание файлов JSON

var service = new BankService(repo);   // слой логики
var ui = new ConsoleUi(service);       // слой интерфейса
ui.Run();                              // запуск приложения

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

            Console.Write("PIN (4 cyfры): ");
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
