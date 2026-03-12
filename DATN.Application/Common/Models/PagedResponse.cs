namespace DATN.Application.Common.Models;

/// <summary>
/// Sử dụng khi T là một dạng List hoặc IEnumerable
/// </summary>
public class PagedResponse<T> : ApiResponse<T>
{
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
    public int TotalRecords { get; set; }

    public PagedResponse(T data, int pageNumber, int pageSize, int totalRecords, string? message = null) 
        : base(data, message, 200) // Gọi base(data, message, 200) để set Success = true, StatusCode 200 và Data
    {
        PageNumber = pageNumber;
        PageSize = pageSize;
        TotalRecords = totalRecords;
        TotalPages = (int)Math.Ceiling(totalRecords / (double)pageSize);
    }
    
    public static PagedResponse<T> SucceedDefault(T data, int pageNumber, int pageSize, int totalRecords, string? message = null)
    {
        return new PagedResponse<T>(data, pageNumber, pageSize, totalRecords, message);
    }
}
