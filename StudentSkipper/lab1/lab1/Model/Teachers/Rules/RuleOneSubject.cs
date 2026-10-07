using lab1.Model.Students;

namespace lab1.Model.Teachers.Rules;

public class RuleOneSubject : IRule
{
    public bool WillBeAsked(Teacher teacher, Student student)
    {
        return student.WasAsked(teacher.SubjectA);
    }
}