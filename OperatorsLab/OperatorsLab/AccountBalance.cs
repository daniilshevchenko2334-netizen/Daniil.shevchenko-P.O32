namespace OperatorsLab;

/// <summary>
/// Задание 3. Счёт с составными операторами присваивания C# 14.
/// Нестатические операторы (+=, ++) изменяют объект на месте, без аллокаций в куче.
/// </summary>
public class AccountBalance
{
    private static int _idCounter;

    // Уникальный неизменяемый идентификатор, генерируется статическим счётчиком
    public int Id { get; }

    public decimal TotalBalance { get; set; }

    public AccountBalance(decimal initialBalance)
    {
        Id = ++_idCounter;
        TotalBalance = initialBalance;
    }

    // Нестатический (без static), void, один параметр, может быть virtual
    public virtual void operator +=(decimal amount)
    {
        TotalBalance += amount;
    }

    // Объединение счетов
    public void operator +=(AccountBalance other)
    {
        TotalBalance += other.TotalBalance;
    }

    // Нестатический инкремент: начисление фиксированного бонуса в 1 рубль (на месте)
    public void operator ++()
    {
        TotalBalance += 1m;
    }

    // Статический инкремент для синтаксиса выражений: AccountBalance next = ++acc;
    // Аргумент не мутируется: возвращается новый экземпляр.
    public static AccountBalance operator ++(AccountBalance a)
    {
        return new AccountBalance(a.TotalBalance + 1m);
    }

    public override string ToString() => $"Счёт #{Id}: {TotalBalance:0.00} руб.";
}
