using System.Collections.ObjectModel;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Windows.Input;
using Bankomat.Core;

namespace BANKOMAT.Wpf.ViewModels;

public class MainViewModel : ObservableObject
{
    private readonly BankRepository _repo;
    private readonly BankService _service;
    private Account? _currentAccount;
    private int _loginTries = 3;

    public MainViewModel()
    {
        _repo = new BankRepository();
        _repo.Init();
        _service = new BankService(_repo);

        LoginCommand = new RelayCommand(_ => Login());
        LogoutCommand = new RelayCommand(_ => Logout(), _ => IsLoggedIn);
        DepositCommand = new RelayCommand(_ => Deposit(), _ => IsLoggedIn);
        WithdrawCommand = new RelayCommand(_ => Withdraw(), _ => IsLoggedIn);
        TransferCommand = new RelayCommand(_ => Transfer(), _ => IsLoggedIn);
        ChangePinCommand = new RelayCommand(_ => ChangePin(), _ => IsLoggedIn);

        History = new ObservableCollection<Transaction>();
    }

    public ICommand LoginCommand { get; }
    public ICommand LogoutCommand { get; }
    public ICommand DepositCommand { get; }
    public ICommand WithdrawCommand { get; }
    public ICommand TransferCommand { get; }
    public ICommand ChangePinCommand { get; }

    private string _cardNumber = string.Empty;
    public string CardNumber
    {
        get => _cardNumber;
        set => SetField(ref _cardNumber, value);
    }

    private string _pin = string.Empty;
    public string Pin
    {
        get => _pin;
        set => SetField(ref _pin, value);
    }

    private string _loginMessage = string.Empty;
    public string LoginMessage
    {
        get => _loginMessage;
        set => SetField(ref _loginMessage, value);
    }

    private bool _isLoggedIn;
    public bool IsLoggedIn
    {
        get => _isLoggedIn;
        private set
        {
            if (SetField(ref _isLoggedIn, value))
            {
                OnPropertyChanged(nameof(Greeting));
                OnPropertyChanged(nameof(BalanceDisplay));
                CommandManager.InvalidateRequerySuggested();
            }
        }
    }

    public string Greeting => _currentAccount != null
        ? $"Witaj, {_currentAccount.Owner}!"
        : "Zaloguj się, aby kontynuować.";

    public string BalanceDisplay => _currentAccount != null
        ? $"{_currentAccount.Balance.ToString("0.00", CultureInfo.InvariantCulture)} PLN"
        : "--";

    private string _depositAmount = string.Empty;
    public string DepositAmount
    {
        get => _depositAmount;
        set => SetField(ref _depositAmount, value);
    }

    private string _withdrawAmount = string.Empty;
    public string WithdrawAmount
    {
        get => _withdrawAmount;
        set => SetField(ref _withdrawAmount, value);
    }

    private string _transferAmount = string.Empty;
    public string TransferAmount
    {
        get => _transferAmount;
        set => SetField(ref _transferAmount, value);
    }

    private string _transferCard = string.Empty;
    public string TransferCard
    {
        get => _transferCard;
        set => SetField(ref _transferCard, value);
    }

    private string _depositMessage = string.Empty;
    public string DepositMessage
    {
        get => _depositMessage;
        set => SetField(ref _depositMessage, value);
    }

    private string _withdrawMessage = string.Empty;
    public string WithdrawMessage
    {
        get => _withdrawMessage;
        set => SetField(ref _withdrawMessage, value);
    }

    private string _transferMessage = string.Empty;
    public string TransferMessage
    {
        get => _transferMessage;
        set => SetField(ref _transferMessage, value);
    }

    private string _changeOldPin = string.Empty;
    public string ChangeOldPin
    {
        get => _changeOldPin;
        set => SetField(ref _changeOldPin, value);
    }

    private string _changeNewPin = string.Empty;
    public string ChangeNewPin
    {
        get => _changeNewPin;
        set => SetField(ref _changeNewPin, value);
    }

    private string _changePinMessage = string.Empty;
    public string ChangePinMessage
    {
        get => _changePinMessage;
        set => SetField(ref _changePinMessage, value);
    }

    public ObservableCollection<Transaction> History { get; }

    private bool ValidateCardAndPin(out string error)
    {
        if (!Regex.IsMatch(CardNumber ?? string.Empty, @"^\d{16}$"))
        {
            error = "Numer karty musi mieć 16 cyfr.";
            return false;
        }

        if (!Regex.IsMatch(Pin ?? string.Empty, @"^\d{4}$"))
        {
            error = "PIN musi mieć 4 cyfry.";
            return false;
        }

        error = string.Empty;
        return true;
    }

    private bool TryParseAmount(string value, out decimal amount, out string error)
    {
        if (!decimal.TryParse(value.Replace(',', '.'), NumberStyles.Number, CultureInfo.InvariantCulture, out amount))
        {
            error = "Niepoprawna kwota.";
            return false;
        }

        amount = Math.Round(amount, 2);
        if (amount <= 0)
        {
            error = "Kwota musi być > 0.";
            return false;
        }

        error = string.Empty;
        return true;
    }

    private void Login()
    {
        LoginMessage = string.Empty;

        if (!ValidateCardAndPin(out var error))
        {
            LoginMessage = error;
            return;
        }

        bool blocked;
        if (_service.TryLogin(CardNumber.Trim(), Pin.Trim(), ref _loginTries, out var account, out var message, out blocked))
        {
            _currentAccount = account;
            IsLoggedIn = true;
            LoginMessage = message;
            RefreshHistory();
            _loginTries = 3;
            OnPropertyChanged(nameof(BalanceDisplay));
        }
        else
        {
            LoginMessage = message;
            if (blocked)
            {
                _currentAccount = null;
                IsLoggedIn = false;
            }
        }
    }

    private void Logout()
    {
        _currentAccount = null;
        IsLoggedIn = false;
        CardNumber = string.Empty;
        Pin = string.Empty;
        _loginTries = 3;
        LoginMessage = "Wylogowano.";
        History.Clear();
        OnPropertyChanged(nameof(BalanceDisplay));
    }

    private void Deposit()
    {
        if (!EnsureAccount(out var acc)) return;
        if (!TryParseAmount(DepositAmount, out var amount, out var error))
        {
            DepositMessage = error;
            return;
        }

        var (ok, err, info) = _service.Deposit(acc, amount);
        DepositMessage = ok ? info : err ?? string.Empty;
        NotifyBalanceChanged();
    }

    private void Withdraw()
    {
        if (!EnsureAccount(out var acc)) return;
        if (!TryParseAmount(WithdrawAmount, out var amount, out var error))
        {
            WithdrawMessage = error;
            return;
        }

        var (ok, err, info) = _service.Withdraw(acc, amount);
        WithdrawMessage = ok ? info : err ?? string.Empty;
        NotifyBalanceChanged();
    }

    private void Transfer()
    {
        if (!EnsureAccount(out var acc)) return;
        if (!Regex.IsMatch(TransferCard ?? string.Empty, @"^\d{16}$"))
        {
            TransferMessage = "Numer karty odbiorcy musi mieć 16 cyfr.";
            return;
        }

        if (!TryParseAmount(TransferAmount, out var amount, out var error))
        {
            TransferMessage = error;
            return;
        }

        var (ok, err, info) = _service.Transfer(acc, TransferCard.Trim(), amount);
        TransferMessage = ok ? info : err ?? string.Empty;
        NotifyBalanceChanged();
    }

    private void ChangePin()
    {
        if (!EnsureAccount(out var acc)) return;
        if (string.IsNullOrWhiteSpace(ChangeOldPin) || string.IsNullOrWhiteSpace(ChangeNewPin))
        {
            ChangePinMessage = "Wpisz aktualny i nowy PIN.";
            return;
        }

        var (ok, err, info) = _service.ChangePin(acc, ChangeOldPin.Trim(), ChangeNewPin.Trim());
        ChangePinMessage = ok ? info : err ?? string.Empty;
    }

    private bool EnsureAccount(out Account acc)
    {
        if (_currentAccount == null)
        {
            DepositMessage = WithdrawMessage = TransferMessage = ChangePinMessage = "Zaloguj się, aby wykonać operację.";
            acc = null!;
            return false;
        }

        acc = _currentAccount;
        return true;
    }

    private void NotifyBalanceChanged()
    {
        OnPropertyChanged(nameof(BalanceDisplay));
        RefreshHistory();
    }

    private void RefreshHistory()
    {
        History.Clear();
        if (_currentAccount == null) return;

        foreach (var tx in _service.GetLastTransactions(_currentAccount, 10))
        {
            History.Add(tx);
        }
    }
}
