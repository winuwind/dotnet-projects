using System.Data;
using lab1.Model.StaticValues;
using lab1.Model.Strategies;
using lab1.Model.Students;
using lab1.Model.Subjects;
using lab1.Model.Teachers;

namespace lab1.Controller;

public class Controller
{
    private readonly Dictionary<Subject, Teacher> _teachers = new();
    private readonly Student _student;

    public Controller(ISkipStrategy skipStrategy)
    {
        _student = new Student(skipStrategy, new StudentHistory());
        foreach (var subject in Enum.GetValues<Subject>())
        {
            _teachers[subject] = new Teacher(subject);
        }
    }

    private bool Day()
    {
        var studentDay = _student.DecideDay();

        foreach (var teacher in _teachers.Values)
        {
            var willBeAsked = teacher.WillBeAsked(_student);
            if (!studentDay[(int)teacher.Subject])
            {
                if (willBeAsked)
                {
                    _student.ResetPleasure();
                    return true;
                }
                else
                {
                    _student.IncrementPleasure();
                }
            }
            else
            {
                _student.SetVisited(StaticValues.Day, teacher.Subject);
                if (willBeAsked)
                {
                    _student.SetAsked(StaticValues.Day, teacher.Subject);
                }
            }
        }
        
        //Пирожок
        _student.IncrementPleasure();
        return false;
    }

    public bool Simulate()
    {
        while (StaticValues.Day < StaticValues.CountDays)
        {
            if (Day())
            {
                return true;
            }
            
            StaticValues.IncrementDay();
        }
        return false;
    }

    public int PrintResult()
    {
        Console.WriteLine("General Pleasure: " + _student.Pleasure);
        Console.WriteLine("Average Pleasure: " + ((double) _student.Pleasure) / (double) StaticValues.CountDays);
        return _student.Pleasure;
    }
    
    public int GetResult()
    {
        return _student.Pleasure;
    }
}