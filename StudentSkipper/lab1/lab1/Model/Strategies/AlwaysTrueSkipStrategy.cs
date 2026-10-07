using lab1.Model.Students;

namespace lab1.Model.Strategies;

public class AlwaysTrueSkipStrategy : ISkipStrategy
{
    public string Name { get; } = "Всегда посещать";

    public bool[] DecideDay(int day, IReadOnlyStudentHistory history)
    {
        var decide = new bool[StaticValues.StaticValues.CountSubjects];
        for (var i = 0; i < StaticValues.StaticValues.CountSubjects; i++)
        {
            decide[i] = true;
        }
        return decide;
    }
}