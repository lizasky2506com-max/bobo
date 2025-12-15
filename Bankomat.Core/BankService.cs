using System.Globalization;
using System.Text.RegularExpressions;

namespace Bankomat.Core;

/// <summary>
/// Сервис с бизнес-логикой банкомата. Только проверки и изменение данных.
/// </summary>
public class BankService
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

        if (!Regex.IsMatch(newPin, @"^\d{4}$"))
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
