using lab1.Model.Students;

namespace lab1.Model.Strategies;

public class AlwaysFalseSkipStrategy : ISkipStrategy
{
    public string Name { get; } = "Всегда пропускать";

    public bool[] DecideDay(int day, IReadOnlyStudentHistory history)
    {
        var decide = new bool[StaticValues.StaticValues.CountSubjects];
        for (var i = 0; i < StaticValues.StaticValues.CountSubjects; i++)
        {
            decide[i] = false;
        }
        return decide;
    }
}