namespace OperatorsLab;

/// <summary>Задание 3. Премиальный счёт: при пополнении начисляется 5% кэшбэка.</summary>
public class PremiumAccount : AccountBalance
{
    private const decimal CashbackRate = 0.05m;

    public PremiumAccount(decimal initialBalance) : base(initialBalance)
    {
    }

    public override void operator +=(decimal amount)
    {
        TotalBalance += amount + amount * CashbackRate;
    }
}
