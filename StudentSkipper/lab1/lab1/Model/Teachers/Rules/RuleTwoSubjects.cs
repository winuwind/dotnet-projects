using lab1.Model.Students;

namespace lab1.Model.Teachers.Rules;

public class RuleTwoSubjects : IRule
{
    public bool WillBeAsked(Teacher teacher, Student student)
    {
        return student.WasAsked(teacher.SubjectA) && !student.WasAsked(teacher.SubjectB) 
               || !student.WasAsked(teacher.SubjectA) && student.WasAsked(teacher.SubjectB);
    }
}