using lab1.Model.Students;

namespace lab1.Model.Teachers.Rules;

public class RuleRandom : IRule
{
    public bool WillBeAsked(Teacher teacher, Student student)
    {
        return new Random().Next(2) != 0;
    }
}