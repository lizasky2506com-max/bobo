# BANKOMAT

Nowe rozwiązanie zawiera aplikację WPF oraz konsolową, korzystające z tej samej logiki w bibliotece **Bankomat.Core**.

## Projekty
- **Bankomat.Core** – modele (`Account`, `Transaction`), repozytorium plikowe i serwis z logiką operacji.
- **BANKOMAT** – pierwotny interfejs konsolowy, nadal korzysta z Core.
- **BANKOMAT.Wpf** – nowy interfejs graficzny (glamour UI, gradienty, zaokrąglone rogi, cienie) z zakładkami: Logowanie, Wpłata, Wypłata, Przelew, Historia, Zmiana PIN.

## Uruchamianie (Windows / Visual Studio)
1. Otwórz `BANKOMAT.sln` w Visual Studio 2022+ na Windows.
2. Upewnij się, że projektem startowym jest **BANKOMAT.Wpf** (ustawione w `.sln`).
3. Przy pierwszym uruchomieniu przyciskiem **Start** zostaną utworzone pliki danych, jeśli nie istnieją.

## Dane i pliki
- Dane są zapisywane w katalogu `data` obok pliku wykonywalnego danego projektu (np. `BANKOMAT.Wpf/bin/Debug/net8.0-windows/data`).
- Tworzone pliki: `accounts.json` i `transactions.json`.
- Dane startowe:
  - Liza – karta `1111222233334444`, PIN `1234`, saldo 1500 PLN.
  - Anna – karta `5555666677778888`, PIN `5678`, saldo 500 PLN.
  - Ola – karta `9999000011112222`, PIN `0000`, saldo 200 PLN.

## Sterowanie
- WPF: pełna obsługa myszą i klawiaturą (pola tekstowe + PasswordBox, przyciski, skrót Enter po wprowadzeniu PIN).
- Konsola: klawiatura zgodnie z oryginalnym interfejsem.
