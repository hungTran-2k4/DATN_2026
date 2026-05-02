using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using DATN.Application.Interfaces.Services;
using DATN.Domain.Entities.Orders;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace DATN.Infrastructure.Services.Payment;

public class VNPaySettings
{
    public string TmnCode { get; set; } = string.Empty;
    public string HashSecret { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = string.Empty;
    public string ReturnUrl { get; set; } = string.Empty;
}

public class VNPayProvider : IPaymentProvider
{
    private readonly VNPaySettings _settings;
    private readonly ILogger<VNPayProvider> _logger;
    public string ProviderName => PaymentMethod.VnPay;

    public VNPayProvider(IConfiguration configuration, ILogger<VNPayProvider> logger)
    {
        _settings = new VNPaySettings();
        configuration.GetSection("VNPay").Bind(_settings);
        _logger = logger;
    }

    public string CreatePaymentUrl(Guid orderId, decimal amount, string orderInfo, string ipAddress)
    {
        var now = GetVietnamNow();
        
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
            { "vnp_CreateDate", now.ToString("yyyyMMddHHmmss", System.Globalization.CultureInfo.InvariantCulture) },
            { "vnp_ExpireDate", now.AddMinutes(15).ToString("yyyyMMddHHmmss", System.Globalization.CultureInfo.InvariantCulture) }
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

                //var key = Uri.EscapeDataString(kv.Key);
                //var value = Uri.EscapeDataString(kv.Value);
                var key = WebUtility.UrlEncode(kv.Key);
                var value = WebUtility.UrlEncode(kv.Value);

                hashData.Append(key + "=" + value);
                query.Append(key + "=" + value);
                
                isFirst = false;
            }
        }

        var rawData = hashData.ToString();
        var secureHash = HmacSha512(_settings.HashSecret, rawData);
        
        // Log dữ liệu để debug trên Azure
        _logger.LogInformation("[VNPay] HashData: {RawData}", rawData);
        _logger.LogInformation("[VNPay] SecureHash: {SecureHash}", secureHash);

        query.Append("&vnp_SecureHash=" + secureHash);
        var finalUrl = _settings.BaseUrl + "?" + query.ToString();
        
        _logger.LogInformation("[VNPay] Final URL: {Url}", finalUrl);

        return finalUrl;
    }

    public PaymentResult HandleIpn(IDictionary<string, string> data)
    {
        var result = new PaymentResult
        {
            RawResponse = JsonSerializer.Serialize(data),
            Signature = data.TryGetValue("vnp_SecureHash", out var sig) ? sig : null
        };

        if (!ValidateSignature(data))
        {
            result.IsSuccess = false;
            result.Message = "Invalid signature";
            result.ResponseCode = "97";
            return result;
        }

        data.TryGetValue("vnp_TxnRef", out var txnRef);
        data.TryGetValue("vnp_Amount", out var amountStr);
        data.TryGetValue("vnp_ResponseCode", out var responseCode);
        data.TryGetValue("vnp_TransactionStatus", out var transactionStatus);
        data.TryGetValue("vnp_TransactionNo", out var transactionNo);
        data.TryGetValue("vnp_BankCode", out var bankCode);
        data.TryGetValue("vnp_CardType", out var cardType);
        data.TryGetValue("vnp_PayDate", out var payDate);

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

    public PaymentResult HandleReturn(IDictionary<string, string> data)
    {
        return HandleIpn(data);
    }

    private DateTime GetVietnamNow()
    {
        try 
        {
            var timezoneId = OperatingSystem.IsWindows() ? "SE Asia Standard Time" : "Asia/Ho_Chi_Minh";
            var timezone = TimeZoneInfo.FindSystemTimeZoneById(timezoneId);
            return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, timezone);
        }
        catch 
        {
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
