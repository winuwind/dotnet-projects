using lab1.Model.Students;
using lab1.Model.Subjects;

namespace lab1.Model.Strategies;

public class MyNewSkipStrategy : ISkipStrategy
{
    public string Name { get; } = "Анализ посещений v2";

    private readonly List<Subject[]>[] _subjectsRuleTwoSubjects =
        new List<Subject[]>[StaticValues.StaticValues.CountSubjects];

    private readonly bool[][] _subjectsRuleOneSubject = new bool[StaticValues.StaticValues.CountSubjects][];
    private readonly bool[] _flagRuleOneSubject = new bool[StaticValues.StaticValues.CountSubjects];
    private readonly bool[] _flagRuleTwoSubjects = new bool[StaticValues.StaticValues.CountSubjects];
    private readonly bool[] _flagRandomRule = new bool[StaticValues.StaticValues.CountSubjects];

    private readonly int _dayLimit = 10;
    private readonly double _percentLimit = 1.0;

    private bool _flagWasAskedFirstly = false;
    
    public MyNewSkipStrategy()
    {
        for (var i = 0; i < StaticValues.StaticValues.CountSubjects; i++)
        {
            _subjectsRuleOneSubject[i] = new bool[StaticValues.StaticValues.CountSubjects];
            _subjectsRuleTwoSubjects[i] = [];
            for (var j = 0; j < StaticValues.StaticValues.CountSubjects; j++)
            {
                _subjectsRuleOneSubject[i][j] = true;
                for (var k = j + 1; k < StaticValues.StaticValues.CountSubjects; k++)
                {
                    _subjectsRuleTwoSubjects[i].Add([(Subject)j, (Subject)k]);
                }
            }

            _flagRuleOneSubject[i] = true;
            _flagRuleTwoSubjects[i] = true;
            _flagRandomRule[i] = false;
        }
    }
    
    public MyNewSkipStrategy(int dayLimit, double percentLimit)
    {
        for (var i = 0; i < StaticValues.StaticValues.CountSubjects; i++)
        {
            _subjectsRuleOneSubject[i] = new bool[StaticValues.StaticValues.CountSubjects];
            _subjectsRuleTwoSubjects[i] = [];
            for (var j = 0; j < StaticValues.StaticValues.CountSubjects; j++)
            {
                _subjectsRuleOneSubject[i][j] = true;
                for (var k = j + 1; k < StaticValues.StaticValues.CountSubjects; k++)
                {
                    _subjectsRuleTwoSubjects[i].Add([(Subject)j, (Subject)k]);
                }
            }

            _flagRuleOneSubject[i] = true;
            _flagRuleTwoSubjects[i] = true;
            _flagRandomRule[i] = false;
        }
        _dayLimit = dayLimit;
        _percentLimit = percentLimit;
    }

    public bool[] DecideDay(int day, IReadOnlyStudentHistory history)
    {
        var decide = new bool[StaticValues.StaticValues.CountSubjects];
        CheckRandom(day, history);
        for (var i = 0; i < StaticValues.StaticValues.CountSubjects; i++)
        {
            decide[i] = DecideSubject(day, history, (Subject)i);
        }

        return decide;
    }

    private bool DecideSubject(int day, IReadOnlyStudentHistory history, Subject subject)
    {
        if (_flagWasAskedFirstly && _flagRandomRule[(int)subject])
        {
            return true;
        }
        
        var flag1 = CheckOneSubjectRule(day, history, subject);
        var flag2 = CheckTwoSubjectsRule(day, history, subject);

        if (day < _dayLimit)
        {
            return true;
        }

        if (!flag1 && !flag2)
        {
            _flagRandomRule[(int) subject] = true;
            return true;
        }

        var probabilityWillAsked = (1.0 / 3.0) * 0.5;
        var probabilityWillNotAsked = (1.0 / 3.0) * 0.5;
        
        var countWillAsked = 0.0;
        var countWillNotAsked = 0.0;
        if (flag1)
        {
            var countCorrect = 0.0;
            for (var i = 0; i < StaticValues.StaticValues.CountSubjects; i++)
            {
                if (_subjectsRuleOneSubject[(int)subject][i])
                {
                    if (history.WasAsked(day - 1, (Subject)i) ?? false)
                    {
                        countWillAsked++;
                    }
                    else
                    {
                        countWillNotAsked++;
                    }
                    countCorrect++;
                }
            }

            if (countCorrect != 0.0)
            {
                countWillAsked /= countCorrect;
                countWillNotAsked /= countCorrect;
            }
        }

        if (flag2)
        {
            var scalar = 1.0 / (double) _subjectsRuleTwoSubjects.Length;
            foreach (var list in _subjectsRuleTwoSubjects[(int)subject])
            {
                var subjectA = list[0];
                var subjectB = list[1];

                var attendedSubjectA = history.Attended(day - 1, subjectA);
                var wasAskedSubjectA = history.WasAsked(day - 1, subjectA) ?? false;
                var attendedSubjectB = history.Attended(day - 1, subjectB);
                var wasAckedSubjectB = history.WasAsked(day - 1, subjectB) ?? false;

                if (!attendedSubjectA || !attendedSubjectB)
                {
                    countWillAsked += scalar;
                }
                else if (wasAskedSubjectA && !wasAckedSubjectB || !wasAskedSubjectA && wasAckedSubjectB)
                {
                    countWillAsked += scalar;
                }
                else
                {
                    countWillNotAsked += scalar;
                }
            }
        }

        probabilityWillAsked += (1.0 / 3.0) * countWillAsked;
        probabilityWillNotAsked += (1.0 / 3.0) * countWillNotAsked;

        if (probabilityWillAsked / probabilityWillNotAsked >= _percentLimit)
        {
            return true;
        }

        return false;
    }

    private void CheckRandom(int day, IReadOnlyStudentHistory history)
    {
        if (!_flagWasAskedFirstly && day > 0)
        {
            foreach (var subject in Enum.GetValues(typeof(Subject)))
            {
                if (subject == null)
                {
                    continue;
                }
                var wasAsked = history.WasAsked(day - 1, (Subject) subject) ?? false;
                if (wasAsked)
                {
                    _flagRandomRule[(int) subject] = true;
                    _flagWasAskedFirstly = true;
                }
            }
        }
    }

    private bool CheckOneSubjectRule(int day, IReadOnlyStudentHistory history, Subject subject)
    {
        if (!_flagRuleOneSubject[(int)subject] || day < 2)
        {
            return false;
        }

        var count = 0;
        for (var i = 0; i < StaticValues.StaticValues.CountSubjects; i++)
        {
            if (_subjectsRuleOneSubject[(int)subject][i])
            {
                count++;
                var wasAskedTwoDaysAgo = history.WasAsked(day - 2, (Subject)i) ?? false;
                var wasAskedOneDayAgo = history.WasAsked(day - 1, subject) ?? false;

                if (wasAskedTwoDaysAgo != wasAskedOneDayAgo)
                {
                    _subjectsRuleOneSubject[(int)subject][i] = false;
                    count--;
                }
            }
        }

        if (count == 0)
        {
            _flagRuleOneSubject[(int)subject] = false;
            return false;
        }

        return true;
    }

    private bool CheckTwoSubjectsRule(int day, IReadOnlyStudentHistory history, Subject subject)
    {
        if (!_flagRuleTwoSubjects[(int)subject] || day < 2)
        {
            return false;
        }

        var newList = new List<Subject[]>();
        foreach (var list in _subjectsRuleTwoSubjects[(int)subject])
        {
            var attendedTwoDaysAgoSubjectA = history.Attended(day - 2, list[0]);
            var wasAskedTwoDaysAgoSubjectA = history.WasAsked(day - 2, list[0]);
            var attendedTwoDaysAgoSubjectB = history.Attended(day - 2, list[1]);
            var wasAskedTwoDaysAgoSubjectB = history.WasAsked(day - 2, list[1]);
            var attendedOneDayAgo = history.Attended(day - 1, subject);
            var wasAskedOneDayAgo = history.WasAsked(day - 1, subject);

            if (!attendedOneDayAgo || !attendedTwoDaysAgoSubjectA || !attendedTwoDaysAgoSubjectB)
            {
                continue;
            }

            if (
                ((wasAskedTwoDaysAgoSubjectA ?? false) && !(wasAskedTwoDaysAgoSubjectB ?? true) ||
                 !(wasAskedTwoDaysAgoSubjectA ?? true) && (wasAskedTwoDaysAgoSubjectB ?? false)) &&
                !(wasAskedOneDayAgo ?? false)
                ||
                !((wasAskedTwoDaysAgoSubjectA ?? false) && !(wasAskedTwoDaysAgoSubjectB ?? true) ||
                 !(wasAskedTwoDaysAgoSubjectA ?? true) && (wasAskedTwoDaysAgoSubjectB ?? false)) &&
                (wasAskedOneDayAgo ?? false)
            )
            {
                continue;
            }
            newList.Add(list.ToArray());
        }

        _subjectsRuleTwoSubjects[(int)subject] = newList;
        
        if (newList.Count == 0)
        {
            _flagRuleTwoSubjects[(int)subject] = false;
            return false;
        }
        return true;
    }
}