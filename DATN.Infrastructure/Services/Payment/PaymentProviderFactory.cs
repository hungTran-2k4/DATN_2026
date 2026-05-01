using DATN.Application.Interfaces.Services;
using DATN.Domain.Entities.Orders;

namespace DATN.Infrastructure.Services.Payment;

/// <summary>
/// Factory để resolve đúng IPaymentProvider theo paymentMethod.
/// Khi thêm cổng mới (MoMo, ZaloPay):
/// 1. Tạo class MoMoProvider : IPaymentProvider
/// 2. Đăng ký vào DI
/// 3. Done — không cần sửa factory hay controller
/// </summary>
public class PaymentProviderFactory : IPaymentProviderFactory
{
    private readonly Dictionary<string, IPaymentProvider> _providers;

    /// <summary>
    /// Inject tất cả IPaymentProvider đã đăng ký → tự động build dictionary.
    /// </summary>
    public PaymentProviderFactory(IEnumerable<IPaymentProvider> providers)
    {
        _providers = providers.ToDictionary(
            p => p.ProviderName,
            p => p,
            StringComparer.OrdinalIgnoreCase);
    }

    public IPaymentProvider? GetProvider(string paymentMethod)
    {
        _providers.TryGetValue(paymentMethod, out var provider);
        return provider;
    }

    /// <summary>
    /// Online payment = có provider đã đăng ký.
    /// COD, BANK_TRANSFER → không phải online payment.
    /// </summary>
    public bool IsOnlinePayment(string paymentMethod)
    {
        return _providers.ContainsKey(paymentMethod);
    }
}
