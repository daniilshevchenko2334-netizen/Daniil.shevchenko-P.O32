using System.Text;

namespace OperatorsLab;

public class Program
{
    public static void Main()
    {
        Console.OutputEncoding = Encoding.UTF8;

        Task1Money();
        Task2Conversions();
        Task3CompoundOperators();

        Console.WriteLine();
        Console.WriteLine("Все сценарии выполнены.");
    }

    private static void Header(string title)
    {
        Console.WriteLine();
        Console.WriteLine(new string('=', 70));
        Console.WriteLine(title);
        Console.WriteLine(new string('=', 70));
    }

    // ------------------------------------------------------------------
    // Задание 1: арифметика, сравнение, true/false/!
    // ------------------------------------------------------------------
    private static void Task1Money()
    {
        Header("ЗАДАНИЕ 1. Money: арифметика, сравнение, true/false/!");

        Money m1 = new Money(150, 80);
        Money m2 = new Money(50, 40);
        Console.WriteLine($"m1 = {m1}; m2 = {m2}");

        Console.WriteLine("\n[Нормализация копеек]");
        Console.WriteLine($"new Money(10, 150) -> {new Money(10, 150)} (ожидается 11 руб. 50 коп.)");
        Console.WriteLine($"new Money(5, -30)  -> {new Money(5, -30)} (ожидается 4 руб. 70 коп.)");

        Console.WriteLine("\n[Оператор + (Money + Money)]");
        Money sum = m1 + m2;
        Console.WriteLine($"m1 + m2 = {sum} (ожидается 201 руб. 20 коп.)");

        Console.WriteLine("\n[Оператор - (Money - Money)]");
        Console.WriteLine($"m1 - m2 = {m1 - m2} (ожидается 100 руб. 40 коп.)");
        Console.WriteLine($"m2 - m1 = {m2 - m1} (отрицательный результат нормализуется)");

        Console.WriteLine("\n[Оператор + (Money + int рублей)]");
        Money plusInt = m1 + 100;
        Console.WriteLine($"m1 + 100 = {plusInt} (ожидается 250 руб. 80 коп.)");
        Console.WriteLine($"Исходный m1 не изменился: {m1} (оператор возвращает новый объект)");

        Console.WriteLine("\n[Операторы < и >]");
        if (m1 > m2) Console.WriteLine("m1 больше m2");
        if (m2 < m1) Console.WriteLine("m2 меньше m1");

        Console.WriteLine("\n[Операторы == и !=]");
        Money copy = new Money(150, 80);
        Console.WriteLine($"m1 == copy: {m1 == copy} (разные объекты, равные суммы)");
        Console.WriteLine($"m1 != m2: {m1 != m2}");

        Console.WriteLine("\n[Операторы true / false / !]");
        Money zero = new Money(0, 0);
        if (m1) Console.WriteLine("Счет m1 активен (баланс > 0)");
        if (!zero) Console.WriteLine("Счет zero пуст (баланс не больше 0): !zero = true");
        if (zero) Console.WriteLine("Эта строка не должна выводиться");
    }

    // ------------------------------------------------------------------
    // Задание 2: implicit / explicit
    // ------------------------------------------------------------------
    private static void Task2Conversions()
    {
        Header("ЗАДАНИЕ 2. Преобразование типов: Money, копейки, CurrencyRecord");

        Console.WriteLine("[implicit long -> Money]");
        Money m = 12550L;
        Console.WriteLine($"Money m = 12550L; -> {m} (ожидается 125 руб. 50 коп.)");

        Console.WriteLine("\n[explicit Money -> long]");
        long allKopeks = (long)m;
        Console.WriteLine($"(long)m = {allKopeks} (ожидается 12550)");

        Console.WriteLine("\n[explicit Money -> CurrencyRecord]");
        CurrencyRecord cr = (CurrencyRecord)m;
        Console.WriteLine($"(CurrencyRecord)m -> Code = {cr.Code}, Amount = {cr.Amount}");

        Console.WriteLine("\n[explicit CurrencyRecord -> Money]");
        Money fromCr = (Money)cr;
        Console.WriteLine($"(Money)cr -> {fromCr}");
        Console.WriteLine($"Туда-обратно совпадает с исходным: {fromCr == m}");

        Console.WriteLine("\n[Округление при обратном преобразовании]");
        var rounded = new CurrencyRecord { Code = "RUB", Amount = 10.567m };
        Console.WriteLine($"(Money) от {rounded.Amount} -> {(Money)rounded} (ожидается 10 руб. 57 коп.)");
    }

    // ------------------------------------------------------------------
    // Задание 3: составные операторы C# 14
    // ------------------------------------------------------------------
    private static void Task3CompoundOperators()
    {
        Header("ЗАДАНИЕ 3. Составные операторы присваивания C# 14");

        Console.WriteLine("[+= на месте: 10000 итераций без создания новых объектов]");
        AccountBalance acc = new AccountBalance(1000m);
        Console.WriteLine($"Начальный Id: {acc.Id}");
        for (int i = 0; i < 10000; i++)
            acc += 10m;
        Console.WriteLine($"Итоговый Id: {acc.Id} (должен остаться прежним)");
        Console.WriteLine($"Баланс: {acc.TotalBalance} (ожидается 101000)");

        Console.WriteLine("\n[+= (AccountBalance other): объединение счетов]");
        AccountBalance other = new AccountBalance(250m);
        int idBefore = acc.Id;
        acc += other;
        Console.WriteLine($"После объединения: баланс = {acc.TotalBalance}, Id = {acc.Id} (был {idBefore})");

        Console.WriteLine("\n[Нестатический ++: бонус 1 руб. на месте]");
        decimal before = acc.TotalBalance;
        acc++;
        Console.WriteLine($"Было {before}, стало {acc.TotalBalance}, Id = {acc.Id}");

        Console.WriteLine("\n[Статический ++ для выражений: AccountBalance next = ++acc;]");
        AccountBalance next = ++acc;
        Console.WriteLine($"acc : {acc}");
        Console.WriteLine($"next: {next}");

        Console.WriteLine("\n[PremiumAccount: переопределённый += (кэшбэк 5%)]");
        PremiumAccount prem = new PremiumAccount(500m);
        Console.WriteLine($"Начальный баланс: {prem.TotalBalance}");
        prem += 100m;
        Console.WriteLine($"После prem += 100m: {prem.TotalBalance} (ожидается 605: +100 и +5 кэшбэка)");

        Console.WriteLine("\n[Полиморфизм: virtual / override]");
        AccountBalance poly = new PremiumAccount(500m);
        poly += 100m;
        Console.WriteLine($"Тип переменной AccountBalance, объект PremiumAccount: {poly.TotalBalance} (вызван override)");
    }
}
