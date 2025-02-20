using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MvcTest.Models;

[Table("employee", Schema = "employees")]
public partial class Employee
{
    [Key]
    [Column("id")]
    public long Id { get; set; }

    [Column("birth_date")]
    public DateTimeOffset BirthDate { get; set; }

    [Column("first_name")]
    [StringLength(14)]
    public string FirstName { get; set; } = null!;

    [Column("last_name")]
    [StringLength(16)]
    public string LastName { get; set; } = null!;

    [Column("hire_date")]
    public DateTimeOffset HireDate { get; set; }
}
