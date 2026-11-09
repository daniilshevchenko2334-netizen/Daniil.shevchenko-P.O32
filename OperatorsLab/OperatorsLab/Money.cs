namespace OperatorsLab;

/// <summary>
/// Задание 1 и 2. Денежная сумма: целые рубли и копейки (0..99).
/// Статические операторы не мутируют аргументы и всегда возвращают новый объект.
/// </summary>
public class Money : IEquatable<Money>
{
    public int Rubles { get; }
    public int Kopeks { get; }

    public Money(int rubles, int kopeks) : this((long)rubles * 100 + kopeks)
    {
    }

    // Основной конструктор: нормализация общего числа копеек в рубли и копейки
    public Money(long totalKopeks)
    {
        long r = totalKopeks / 100;
        long k = totalKopeks % 100;
        if (k < 0)
        {
            k += 100;
            r--;
        }
        Rubles = checked((int)r);
        Kopeks = (int)k;
    }

    /// <summary>Общий баланс в копейках.</summary>
    public long TotalKopeks => (long)Rubles * 100 + Kopeks;

    // ---------- Задание 1: арифметика ----------
    public static Money operator +(Money a, Money b) => new Money(a.Rubles + b.Rubles, a.Kopeks + b.Kopeks);

    public static Money operator -(Money a, Money b) => new Money(a.Rubles - b.Rubles, a.Kopeks - b.Kopeks);

    // Money + int (добавление целого числа рублей)
    public static Money operator +(Money a, int rubles) => new Money(a.Rubles + rubles, a.Kopeks);

    // ---------- Задание 1: сравнение (строго парами) ----------
    public static bool operator <(Money a, Money b) => a.TotalKopeks < b.TotalKopeks;

    public static bool operator >(Money a, Money b) => a.TotalKopeks > b.TotalKopeks;

    public static bool operator ==(Money? a, Money? b)
    {
        if (a is null || b is null) return a is null && b is null;
        return a.TotalKopeks == b.TotalKopeks;
    }

    public static bool operator !=(Money? a, Money? b) => !(a == b);

    // ---------- Задание 1: операторы истинности и отрицания ----------
    // Объект истинен, если баланс строго больше 0
    public static bool operator true(Money m) => m.TotalKopeks > 0;

    public static bool operator false(Money m) => m.TotalKopeks <= 0;

    public static bool operator !(Money m) => m.TotalKopeks <= 0;

    // ---------- Задание 2: приведение к элементарному типу ----------
    // Явное: Money -> long (общий баланс в копейках)
    public static explicit operator long(Money m) => m.TotalKopeks;

    // Неявное: long (копейки) -> Money
    public static implicit operator Money(long totalKopeks) => new Money(totalKopeks);

    // ---------- Задание 2: приведение между составными типами ----------
    // Явное, так как при переводе теряется (подменяется) тип валюты
    public static explicit operator CurrencyRecord(Money m) =>
        new CurrencyRecord { Code = "RUB", Amount = m.Rubles + m.Kopeks / 100m };

    // Явное: decimal -> рубли и копейки с округлением до копеек
    public static explicit operator Money(CurrencyRecord cr)
    {
        long total = (long)Math.Round(cr.Amount * 100m, MidpointRounding.AwayFromZero);
        return new Money(total);
    }

    // ---------- Служебное (нужно для корректной работы == и !=) ----------
    public bool Equals(Money? other) => other is not null && TotalKopeks == other.TotalKopeks;

    public override bool Equals(object? obj) => obj is Money m && Equals(m);

    public override int GetHashCode() => TotalKopeks.GetHashCode();

    public override string ToString() => $"{Rubles} руб. {Kopeks:D2} коп.";
}
