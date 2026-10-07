using lab1.Model.Students;

namespace lab1.Model.Strategies;

public class RandomSkipStrategy : ISkipStrategy
{
    public string Name { get; } = "Случайный выбор";

    private readonly int _chance;
    private readonly Random _random = new Random();

    public RandomSkipStrategy(int chance)
    {
        _chance = chance;
    }

    public RandomSkipStrategy()
    {
        _chance = _random.Next(0, 100);
    }

    public bool[] DecideDay(int day, IReadOnlyStudentHistory history)
    {
        var decide = new bool[StaticValues.StaticValues.CountSubjects];
        for (var i = 0; i < StaticValues.StaticValues.CountSubjects; i++)
        {
            if (_random.Next(100) < _chance)
            {
                decide[i] = true;
            }
            else
            {
                decide[i] = false;
            }
        }

        return decide;
    }
}