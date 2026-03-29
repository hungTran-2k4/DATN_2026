using DATN.Domain.Common.Models;

namespace DATN.Application.Common.Models;

public class PagedRequest
{
    public string? Search { get; set; }
    public FilterDescriptor? Filter { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;

    public PagedRequest() { }

    public PagedRequest(string? search = null, FilterDescriptor? filter = null, int page = 1, int pageSize = 10)
    {
        Search = search;
        Filter = filter;
        Page = page;
        PageSize = pageSize;
    }
}
