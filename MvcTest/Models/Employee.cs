using FreeSql.DataAnnotations;

namespace MvcTest.Models;

[Table(Name = "employees.employee")]
public class Employee
{
    [Column(IsPrimary = true, Name = "id")]
    public int Id { get; set; }

    [Column(Name = "first_name", StringLength = 14)]
    public string FirstName { get; set; } = null!;

    [Column(Name = "last_name", StringLength = 16)]
    public string LastName { get; set; } = null!;

    [Column(Name = "birth_date")]
    public DateTime BirthDate { get; set; }

    [Column(Name = "hire_date")]
    public DateTime HireDate { get; set; }

    [Column(Name = "is_match")]
    public bool IsMatch { get; set; }
}
