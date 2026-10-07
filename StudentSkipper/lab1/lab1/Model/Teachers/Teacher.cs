using lab1.Model.Students;
using lab1.Model.Subjects;
using lab1.Model.Teachers.Rules;

namespace lab1.Model.Teachers;

public class Teacher
{
    public Subject Subject { get; }
    public Subject SubjectA { get; }
    public Subject SubjectB { get; }
    
    private readonly IRule _rule;

    public Teacher(Subject subject)
    {
        var random = new Random();
        
        Subject = subject;
        
        SubjectA = (Subject) random.Next(StaticValues.StaticValues.CountSubjects);

        SubjectB = (Subject) (
            (
                random.Next(StaticValues.StaticValues.CountSubjects - 1)
                + 1 + (int) SubjectA
            ) % StaticValues.StaticValues.CountSubjects
        );
        
        var ruleNumber = random.Next(3);
        _rule = ruleNumber switch
        {
            0 => new RuleRandom(),
            1 => new RuleOneSubject(),
            _ => new RuleTwoSubjects(),
        };
    }

    public bool WillBeAsked(Student student)
    {
        return _rule.WillBeAsked(this, student);
    }
}