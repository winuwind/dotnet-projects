using lab1.Model.Subjects;

namespace lab1.Model.Students;

public interface IReadOnlyStudentHistory

{

    /// <summary>Был ли студент на паре по предмету в указанный день.</summary>

    bool Attended(int day, Subject subject);



    /// <summary>

    /// Спросили ли по предмету в указанный день — известно только если Attended(day, subject) == true.

    /// Если студент прогулял и его не спросили — здесь null.

    /// </summary>

    bool? WasAsked(int day, Subject subject);

}