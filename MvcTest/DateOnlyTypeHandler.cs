using FreeSql.Internal.Model;

namespace MvcTest;

public class DateOnlyTypeHandler : TypeHandler<DateOnly>
{
    public override object Serialize(DateOnly value) => value.ToString("yyyy-MM-dd");

    public override DateOnly Deserialize(object value) =>
        DateOnly.TryParse(string.Concat(value), out var trydo) ? trydo : DateOnly.MinValue;
}
