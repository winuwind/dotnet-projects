using lab1.Model.Students;

namespace lab1.Model.Teachers.Rules;

public interface IRule
{
    bool WillBeAsked(Teacher teacher, Student student);
}