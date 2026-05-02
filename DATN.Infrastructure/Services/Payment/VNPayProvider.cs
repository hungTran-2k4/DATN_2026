using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using DATN.Application.Interfaces.Services;
using DATN.Domain.Entities.Orders;
using Microsoft.Extensions.Configuration;

namespace DATN.Infrastructure.Services.Payment;

/// <summary>
/// Cấu hình VNPay được bind từ appsettings.json section "VNPay".
/// </summary>
public class VNPaySettings
{
    public string TmnCode { get; set; } = string.Empty;
    public string HashSecret { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = string.Empty;
    public string ReturnUrl { get; set; } = string.Empty;
}

/// <summary>
/// VNPay Provider — implement IPaymentProvider cho cổng VNPay.
/// Thuật toán: HMACSHA512, Version: 2.1.0
/// Tham chiếu: https://sandbox.vnpayment.vn/apis/docs/thanh-toan-pay/pay.html
/// </summary>
public class VNPayProvider : IPaymentProvider
{
    private readonly VNPaySettings _settings;
    public string ProviderName => PaymentMethod.VnPay;

    public VNPayProvider(IConfiguration configuration)
    {
        _settings = new VNPaySettings();
        configuration.GetSection("VNPay").Bind(_settings);
    }

    /// <inheritdoc/>
    public string CreatePaymentUrl(Guid orderId, decimal amount, string orderInfo, string ipAddress)
    {
        var now = GetVietnamNow();
        
        // Đảm bảo IP là IPv4 hợp lệ (VNPay Sandbox không thích IPv6 hoặc chuỗi lạ)
        if (string.IsNullOrEmpty(ipAddress) || ipAddress.Contains(":")) 
        {
            ipAddress = "127.0.0.1"; 
        }

        var vnpParams = new SortedDictionary<string, string>
        {
            { "vnp_Version", "2.1.0" },
            { "vnp_Command", "pay" },
            { "vnp_TmnCode", _settings.TmnCode },
            { "vnp_Amount", ((long)(amount * 100)).ToString() },
            { "vnp_CurrCode", "VND" },
            { "vnp_TxnRef", orderId.ToString() },
            { "vnp_OrderInfo", RemoveDiacritics(orderInfo) },
            { "vnp_OrderType", "other" },
            { "vnp_Locale", "vn" },
            { "vnp_ReturnUrl", _settings.ReturnUrl },
            { "vnp_IpAddr", ipAddress },
            { "vnp_CreateDate", now.ToString("yyyyMMddHHmmss") },
            { "vnp_ExpireDate", now.AddMinutes(15).ToString("yyyyMMddHHmmss") }
        };

        var hashData = new StringBuilder();
        var query = new StringBuilder();
        var isFirst = true;

        foreach (var kv in vnpParams)
        {
            if (!string.IsNullOrEmpty(kv.Value))
            {
                if (!isFirst) 
                { 
                    hashData.Append('&'); 
                    query.Append('&'); 
                }
                
                // VNPay 2.1.0: Cả chuỗi hash và query đều CẦN UrlEncode
                var key = WebUtility.UrlEncode(kv.Key);
                var value = WebUtility.UrlEncode(kv.Value);

                hashData.Append(key + "=" + value);
                query.Append(key + "=" + value);
                
                isFirst = false;
            }
        }

        var secureHash = HmacSha512(_settings.HashSecret, hashData.ToString());
        query.Append("&vnp_SecureHash=" + secureHash);

        return _settings.BaseUrl + "?" + query.ToString();
    }

    /// <inheritdoc/>
    public PaymentResult HandleIpn(IDictionary<string, string> data)
    {
        var result = new PaymentResult
        {
            RawResponse = JsonSerializer.Serialize(data),
            Signature = data.TryGetValue("vnp_SecureHash", out var sig) ? sig : null
        };

        // 1. Validate signature
        if (!ValidateSignature(data))
        {
            result.IsSuccess = false;
            result.Message = "Invalid signature";
            result.ResponseCode = "97";
            return result;
        }

        // 2. Parse response data
        data.TryGetValue("vnp_TxnRef", out var txnRef);
        data.TryGetValue("vnp_Amount", out var amountStr);
        data.TryGetValue("vnp_ResponseCode", out var responseCode);
        data.TryGetValue("vnp_TransactionStatus", out var transactionStatus);
        data.TryGetValue("vnp_TransactionNo", out var transactionNo);
        data.TryGetValue("vnp_BankCode", out var bankCode);
        data.TryGetValue("vnp_CardType", out var cardType);
        data.TryGetValue("vnp_PayDate", out var payDate);

        // Parse OrderId
        if (!Guid.TryParse(txnRef, out var orderId))
        {
            result.IsSuccess = false;
            result.Message = "Invalid order reference";
            result.ResponseCode = "01";
            return result;
        }

        result.OrderId = orderId;
        result.Amount = long.TryParse(amountStr, out var amt) ? amt / 100m : 0;
        result.TransactionId = transactionNo;
        result.ResponseCode = responseCode;
        result.BankCode = bankCode;
        result.CardType = cardType;
        result.PayDate = payDate;
        result.IsSuccess = responseCode == "00" && transactionStatus == "00";
        result.Message = result.IsSuccess ? "Giao dịch thành công" : $"Giao dịch thất bại (mã: {responseCode})";

        return result;
    }

    /// <inheritdoc/>
    public PaymentResult HandleReturn(IDictionary<string, string> data)
    {
        // Return URL chỉ để hiển thị, logic giống HandleIpn nhưng KHÔNG update DB
        return HandleIpn(data);
    }

    // ──── Private helpers ────

    private DateTime GetVietnamNow()
    {
        try 
        {
            // Windows: "SE Asia Standard Time", Linux/Azure: "Asia/Ho_Chi_Minh"
            var timezoneId = OperatingSystem.IsWindows() ? "SE Asia Standard Time" : "Asia/Ho_Chi_Minh";
            var timezone = TimeZoneInfo.FindSystemTimeZoneById(timezoneId);
            return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, timezone);
        }
        catch 
        {
            // Fallback nếu không tìm thấy timezone
            return DateTime.UtcNow.AddHours(7);
        }
    }

    private bool ValidateSignature(IDictionary<string, string> queryParams)
    {
        if (!queryParams.TryGetValue("vnp_SecureHash", out var vnpSecureHash) || string.IsNullOrEmpty(vnpSecureHash))
            return false;

        var inputData = new SortedDictionary<string, string>();
        foreach (var kv in queryParams)
        {
            if (!string.IsNullOrEmpty(kv.Key) && kv.Key.StartsWith("vnp_")
                && kv.Key != "vnp_SecureHash" && kv.Key != "vnp_SecureHashType")
            {
                inputData[kv.Key] = kv.Value;
            }
        }

        var hashData = new StringBuilder();
        var isFirst = true;
        foreach (var kv in inputData)
        {
            if (!isFirst) hashData.Append('&');
            // VNPay 2.1.0: Cần UrlEncode cả Key và Value khi kiểm tra chữ ký
            hashData.Append(WebUtility.UrlEncode(kv.Key) + "=" + WebUtility.UrlEncode(kv.Value));
            isFirst = false;
        }

        var computedHash = HmacSha512(_settings.HashSecret, hashData.ToString());
        return string.Equals(computedHash, vnpSecureHash, StringComparison.InvariantCultureIgnoreCase);
    }

    private static string HmacSha512(string key, string data)
    {
        var keyBytes = Encoding.UTF8.GetBytes(key);
        var dataBytes = Encoding.UTF8.GetBytes(data);
        using var hmac = new HMACSHA512(keyBytes);
        var hashBytes = hmac.ComputeHash(dataBytes);
        return BitConverter.ToString(hashBytes).Replace("-", "").ToUpper();
    }

    private static string RemoveDiacritics(string text)
    {
        if (string.IsNullOrEmpty(text)) return text;
        var normalizedString = text.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder();
        foreach (var c in normalizedString)
        {
            var unicodeCategory = System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c);
            if (unicodeCategory != System.Globalization.UnicodeCategory.NonSpacingMark)
                sb.Append(c);
        }
        return sb.ToString().Normalize(NormalizationForm.FormC)
            .Replace("đ", "d").Replace("Đ", "D");
    }
}
