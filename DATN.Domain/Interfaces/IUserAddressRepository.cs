using DATN.Domain.Entities.Identity;

namespace DATN.Domain.Interfaces;

public interface IUserAddressRepository
{
    /// <summary>Lấy tất cả địa chỉ của user</summary>
    Task<IEnumerable<UserAddress>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>Lấy 1 địa chỉ theo Id, kiểm tra ownership</summary>
    Task<UserAddress?> GetByIdAsync(Guid id, Guid userId, CancellationToken cancellationToken = default);

    /// <summary>Thêm địa chỉ mới</summary>
    Task<UserAddress> AddAsync(UserAddress address, CancellationToken cancellationToken = default);

    /// <summary>Cập nhật địa chỉ</summary>
    Task<bool> UpdateAsync(UserAddress address, CancellationToken cancellationToken = default);

    /// <summary>Xóa địa chỉ (chỉ xóa được của chính mình)</summary>
    Task<bool> DeleteAsync(Guid id, Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Đặt địa chỉ mặc định: bỏ default của tất cả địa chỉ khác rồi set địa chỉ target = true
    /// </summary>
    Task<bool> SetDefaultAsync(Guid id, Guid userId, CancellationToken cancellationToken = default);
}
