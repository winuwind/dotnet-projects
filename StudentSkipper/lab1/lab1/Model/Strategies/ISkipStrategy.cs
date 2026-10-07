using lab1.Model.Students;

namespace lab1.Model.Strategies;

public interface ISkipStrategy

{

    string Name { get; }



    /// <summary>

    /// Решение на день: для каждого предмета — идти на пару (true) или прогулять (false).

    /// Вызывается один раз в начале каждого дня, до того как станут известны сегодняшние исходы.

    /// </summary>

    bool[] DecideDay(int day, IReadOnlyStudentHistory history);

}