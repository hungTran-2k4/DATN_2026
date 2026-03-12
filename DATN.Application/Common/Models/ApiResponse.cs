namespace DATN.Application.Common.Models;

public class ApiResponse<T>
{
    /// <summary>
    /// HTTP Status Code (ví dụ: 200, 400, 404, 500)
    /// </summary>
    public int StatusCode { get; set; }

    /// <summary>
    /// Trạng thái của request (true: thành công, false: thất bại)
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Thông báo từ server (ví dụ: "Thêm mới thành công", "Lỗi xác thực")
    /// </summary>
    public string? Message { get; set; }

    /// <summary>
    /// Mã lỗi tùy chỉnh của ứng dụng (ví dụ: "USER_NOT_FOUND", "INVALID_PASSWORD")
    /// </summary>
    public string? ErrorCode { get; set; }

    /// <summary>
    /// Giữ log lỗi chi tiết, đặc biệt hữu ích cho validation form (Tên trường: danh sách lỗi)
    /// </summary>
    public Dictionary<string, string[]>? Errors { get; set; }

    /// <summary>
    /// TraceId từ HttpContext để dễ dàng tra cứu log trên server
    /// </summary>
    public string? TraceId { get; set; }

    /// <summary>
    /// Dữ liệu trả về
    /// </summary>
    public T? Data { get; set; }

    public ApiResponse()
    {
    }

    public ApiResponse(T data, string? message = null, int statusCode = 200)
    {
        Success = true;
        StatusCode = statusCode;
        Message = message;
        Data = data;
    }

    public ApiResponse(string message, int statusCode = 400, string? errorCode = null)
    {
        Success = false;
        StatusCode = statusCode;
        Message = message;
        ErrorCode = errorCode;
    }

    // Các factory method giúp code gọn hơn khi return
    public static ApiResponse<T> Succeed(T data, string? message = null, int statusCode = 200)
    {
        return new ApiResponse<T>(data, message, statusCode);
    }

    public static ApiResponse<T> Fail(
        string message, 
        int statusCode = 400, 
        string? errorCode = null, 
        Dictionary<string, string[]>? errors = null,
        string? traceId = null)
    {
        return new ApiResponse<T> 
        { 
            Success = false, 
            StatusCode = statusCode,
            Message = message, 
            ErrorCode = errorCode,
            Errors = errors,
            TraceId = traceId
        };
    }
}
