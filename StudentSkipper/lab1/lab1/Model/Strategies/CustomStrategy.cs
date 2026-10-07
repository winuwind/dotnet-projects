using lab1.Model.Students;

namespace lab1.Model.Strategies;

public class CustomStrategy : ISkipStrategy
{
    public string Name => "99 посещений, 1 вылет";

    private int _dayLimit = 99;

    public bool[] DecideDay(int day, IReadOnlyStudentHistory history)
    {
        if (day < _dayLimit)
        {
            var result = new bool[StaticValues.StaticValues.CountSubjects];
            for (var i = 0; i < result.Length; i++)
            {
                result[i] = true;
            }
            return result;
        }
        var decide =  new bool[StaticValues.StaticValues.CountSubjects];
        for (var i = 0; i < decide.Length; i++)
        {
            decide[i] = false;
        }
        return decide;
    }
}