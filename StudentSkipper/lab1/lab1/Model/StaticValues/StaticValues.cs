using lab1.Model.Subjects;

namespace lab1.Model.StaticValues;

public static class StaticValues
{
    private static int _numberDay = 0;
    
    public static int Day => _numberDay;

    public static readonly int CountDays = 100;
    
    public static readonly int CountSubjects = Enum.GetValues<Subject>().Length;

    public static void IncrementDay()
    {
        _numberDay++;
    }

    public static void ResetDay()
    {
        _numberDay = 0;
    }
}