using Newtonsoft.Json;

namespace MvcTest.Models;

public class LoadDto
{
    [JsonProperty("DT_RowId")]
    public required long RowId { get; init; }

    public required long Id { get; init; }

    public required string FirstName { get; init; }

    public required string LastName { get; init; }

    public required string BirthDate { get; init; }

    public required string HireDate { get; init; }
}
