using lab1.Model.Subjects;

namespace lab1.Model.Students;

public class StudentHistory : IReadOnlyStudentHistory
{
    private readonly int _countSubjects = StaticValues.StaticValues.CountSubjects;
    
    private readonly bool[][] _browsingHistory = new bool[StaticValues.StaticValues.CountDays][];
    private readonly bool?[][] _demandHistory = new bool?[StaticValues.StaticValues.CountDays][];


    public StudentHistory()
    {
        for (var i = 0; i < StaticValues.StaticValues.CountDays; i++)
        {
            _browsingHistory[i] = new bool[_countSubjects];
            _demandHistory[i] = new bool?[_countSubjects];

            for (var j = 0; j < _countSubjects; j++)
            {
                _browsingHistory[i][j] = false;
                _demandHistory[i][j] = null;
            }
        }
    }
    
    public bool Attended(int day, Subject subject)
    {
        if (day < 0 || day >= StaticValues.StaticValues.CountDays)
        {
            return false;
        }
        return _browsingHistory[day][(int) subject];
    }

    public bool? WasAsked(int day, Subject subject)
    {
        if (day < 0 || day >= StaticValues.StaticValues.CountDays)
        {
            return false;
        }
        return _demandHistory[day][(int) subject];
    }

    public void SetVisited(int day, Subject subject)
    {
        if (day < 0 || day >= StaticValues.StaticValues.CountDays)
        {
            return;
        }
        _browsingHistory[day][(int) subject] = true;
        _demandHistory[day][(int) subject] = false;
    }

    public void SetAsked(int day, Subject subject)
    {
        if (day < 0 || day >= StaticValues.StaticValues.CountDays)
        {
            return;
        }
        _demandHistory[day][(int) subject] = true;
    }
}