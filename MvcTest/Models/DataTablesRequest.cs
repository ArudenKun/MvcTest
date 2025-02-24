namespace MvcTest.Models;

public class DataTablesRequest
{
    public int Draw { get; set; }
    public int Start { get; set; }
    public int Length { get; set; }
    public Search Search { get; set; } = new();
    public List<ColumnOrder> Order { get; set; } = [];
    public List<Column> Columns { get; set; } = [];
}

public class Search
{
    public string Value { get; set; } = string.Empty;
    public bool Regex { get; set; }
}

public class Column
{
    public string Data { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool Searchable { get; set; }
    public bool Orderable { get; set; }
    public Search Search { get; set; } = new();
}

public class ColumnOrder
{
    public int Column { get; set; }
    public string Dir { get; set; } = string.Empty;
}
