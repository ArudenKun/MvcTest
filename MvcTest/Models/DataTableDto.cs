using System.Collections;
using Newtonsoft.Json;

namespace MvcTest.Models;

public class DataTableDto
{
    private DataTableDto(int draw, long recordsTotal, long recordsFiltered, IEnumerable data)
    {
        Draw = draw;
        RecordsTotal = recordsTotal;
        RecordsFiltered = recordsFiltered;
        Data = data;
    }

    [JsonProperty("draw")]
    public int Draw { get; }

    [JsonProperty("recordsTotal")]
    public long RecordsTotal { get; }

    [JsonProperty("recordsFiltered")]
    public long RecordsFiltered { get; }

    [JsonProperty("data")]
    public IEnumerable Data { get; }

    public static DataTableDto Create<TData>(
        int draw,
        long recordsTotal,
        long recordsFiltered,
        TData data
    )
        where TData : IEnumerable
    {
        return new DataTableDto(draw, recordsTotal, recordsFiltered, data);
    }
}
