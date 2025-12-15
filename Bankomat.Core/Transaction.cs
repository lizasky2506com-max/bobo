namespace Bankomat.Core;

public class Transaction
{
    public DateTime Time { get; set; }
    public int? FromId { get; set; }
    public int? ToId { get; set; }
    public string Type { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public decimal BalanceAfter { get; set; }
    public string Note { get; set; } = string.Empty;
}
