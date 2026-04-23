namespace DATN.Domain.Enums;

/// <summary>
/// Trạng thái sản phẩm trong luồng duyệt của Marketplace.
/// Giá trị string được lưu trực tiếp vào cột products.status (varchar).
/// </summary>
public enum ProductStatus
{
    /// <summary>Nháp — Seller đang soạn thảo, chưa gửi duyệt.</summary>
    Draft,

    /// <summary>Chờ duyệt — Seller đã gửi, Admin chưa xem xét.</summary>
    Pending,

    /// <summary>Đang bán — Admin đã duyệt, hiển thị cho khách hàng.</summary>
    Active,

    /// <summary>Bị từ chối — Admin từ chối, Seller cần chỉnh sửa và gửi lại.</summary>
    Rejected,

    /// <summary>Ẩn — Seller tự ẩn hoặc Admin tạm ẩn, không hiển thị.</summary>
    Inactive,
}

/// <summary>Extension methods để chuyển đổi giữa enum và string DB.</summary>
public static class ProductStatusExtensions
{
    public static string ToStatusString(this ProductStatus status) => status.ToString();

    public static ProductStatus ToProductStatus(this string? value) =>
        value?.Trim() switch
        {
            "Draft" => ProductStatus.Draft,
            "Pending" => ProductStatus.Pending,
            "Active" => ProductStatus.Active,
            "Rejected" => ProductStatus.Rejected,
            "Inactive" => ProductStatus.Inactive,
            _ => ProductStatus.Draft,
        };

    public static string ToDisplayName(this ProductStatus status) =>
        status switch
        {
            ProductStatus.Draft => "Nháp",
            ProductStatus.Pending => "Chờ duyệt",
            ProductStatus.Active => "Đang bán",
            ProductStatus.Rejected => "Bị từ chối",
            ProductStatus.Inactive => "Ẩn",
            _ => "Không xác định",
        };
}
