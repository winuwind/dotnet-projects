using lab1.Model.Strategies;
using lab1.Model.Subjects;

namespace lab1.Model.Students;

public class Student
{
    private readonly ISkipStrategy _skipStrategy;
    private readonly StudentHistory _studentHistory;

    private int _pleasure = 0;
    
    public int Pleasure => _pleasure;

    public Student(ISkipStrategy skipStrategy, StudentHistory studentHistory)
    {
        _skipStrategy = skipStrategy;
        _studentHistory = studentHistory;
    }
    
    public bool WasAsked(Subject subject)
    {
        return _studentHistory.Attended(StaticValues.StaticValues.Day - 1, subject) && (_studentHistory.WasAsked(StaticValues.StaticValues.Day - 1, subject) ?? false);
    }

    public bool[] DecideDay()
    {
        return _skipStrategy.DecideDay(StaticValues.StaticValues.Day, _studentHistory);
    }

    public void ResetPleasure()
    {
        _pleasure = 0;
    }

    public void IncrementPleasure()
    {
        _pleasure++;
    }

    public void SetVisited(int day, Subject subject)
    {
        _studentHistory.SetVisited(day, subject);
    }

    public void SetAsked(int day, Subject subject)
    {
        _studentHistory.SetAsked(day, subject);
    }
}