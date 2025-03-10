using System.Web.Mvc;

namespace MvcTest.Models;

public class DataTablesRequestBinder : IModelBinder
{
    public object BindModel(ControllerContext controllerContext, ModelBindingContext bindingContext)
    {
        var request = controllerContext.HttpContext.Request;

        // Retrieve request data from Form
        var draw = Convert.ToInt32(request.Form["draw"]);
        var start = Convert.ToInt32(request.Form["start"]);
        var length = Convert.ToInt32(request.Form["length"]);

        // Search
        var search = new Search
        {
            Value = request.Form["search[value]"],
            Regex = Convert.ToBoolean(request.Form["search[regex]"]),
        };

        // Order
        var order = new List<ColumnOrder>();
        var o = 0;
        while (!string.IsNullOrEmpty(request.Form[$"order[{o}][column]"]))
        {
            order.Add(
                new ColumnOrder
                {
                    Column = Convert.ToInt32(request.Form[$"order[{o}][column]"]),
                    Dir = request.Form[$"order[{o}][dir]"],
                }
            );
            o++;
        }

        // Columns
        var columns = new List<Column>();
        var c = 0;
        while (!string.IsNullOrEmpty(request.Form[$"columns[{c}][name]"]))
        {
            columns.Add(
                new Column
                {
                    Data = request.Form[$"columns[{c}][data]"],
                    Name = request.Form[$"columns[{c}][name]"],
                    Orderable = Convert.ToBoolean(request.Form[$"columns[{c}][orderable]"]),
                    Searchable = Convert.ToBoolean(request.Form[$"columns[{c}][searchable]"]),
                    Search = new Search
                    {
                        Value = request.Form[$"columns[{c}][search][value]"],
                        Regex = Convert.ToBoolean(request.Form[$"columns[{c}][search][regex]"]),
                    },
                }
            );
            c++;
        }

        var result = new DataTablesRequest
        {
            Draw = draw,
            Start = start,
            Length = length,
            Search = search,
            Order = order,
            Columns = columns,
        };

        bindingContext.Model = result;
        return result;
    }
}
