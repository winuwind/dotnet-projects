using lab1.Model.Students;

namespace lab1.Model.Strategies;

public class KateStrategy : ISkipStrategy
{
    public string Name => "Стратегия Кати";

    public bool[] DecideDay(int day, IReadOnlyStudentHistory history)
    {
        var decide = new bool[StaticValues.StaticValues.CountSubjects];
        for (var i = 0; i < decide.Length - 2; i++)
            decide[i] = true;
        for (var i = decide.Length - 2; i < decide.Length; i++)
        {
            if (i < 0)
            {
                continue;
            }
            decide[i] = false;
        }
        return decide;
    }
}