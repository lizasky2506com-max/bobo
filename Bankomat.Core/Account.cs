namespace Bankomat.Core;

public class Account
{
    public int Id { get; set; }
    public string Owner { get; set; } = string.Empty;
    public string Card { get; set; } = string.Empty;
    public string Pin { get; set; } = string.Empty;
    public decimal Balance { get; set; }
    public bool Blocked { get; set; }
}
