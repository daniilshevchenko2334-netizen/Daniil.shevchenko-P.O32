namespace OperatorsLab;

/// <summary>Задание 2. Запись о сумме в валюте.</summary>
public class CurrencyRecord
{
    public string Code { get; set; } = "RUB";
    public decimal Amount { get; set; }

    public override string ToString() => $"{Amount:0.00} {Code}";
}
