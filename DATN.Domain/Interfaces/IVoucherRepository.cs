using DATN.Domain.Entities.Marketing;

namespace DATN.Domain.Interfaces;

public interface IVoucherRepository
{
    // Voucher Management
    Task<(IEnumerable<Voucher> Items, int Total)> GetPagedAsync(
        string? search = null, 
        Guid? shopId = null,
        int page = 1, 
        int pageSize = 20, 
        CancellationToken cancellationToken = default);

    Task<IEnumerable<Voucher>> GetActiveVouchersAsync(Guid? shopId = null, CancellationToken cancellationToken = default);
    Task<Voucher?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Voucher?> GetByCodeAsync(string code, Guid? shopId = null, CancellationToken cancellationToken = default);

    Task<Voucher> AddAsync(Voucher voucher, CancellationToken cancellationToken = default);
    Task<bool> UpdateAsync(Voucher voucher, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    // User Voucher interactions
    Task<bool> SaveVoucherForUserAsync(UserVoucher userVoucher, CancellationToken cancellationToken = default);
    Task<bool> HasUserSavedVoucherAsync(Guid userId, Guid voucherId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Voucher>> GetUserSavedVouchersAsync(Guid userId, bool isUsed = false, CancellationToken cancellationToken = default);
    Task<bool> MarkVoucherAsUsedAsync(Guid userId, Guid voucherId, CancellationToken cancellationToken = default);
}
