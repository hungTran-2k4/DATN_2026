using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using DATN.Application.Interfaces.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace DATN.Infrastructure.Services.Shipping;

public class GHNSettings
{
    public string Token { get; set; } = string.Empty;
    public string ShopId { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = "https://dev-online-gateway.ghn.vn";
}

public class GHNProvider : IShippingProvider
{
    private readonly GHNSettings _settings;
    private readonly HttpClient _httpClient;
    private readonly ILogger<GHNProvider> _logger;

    public string ProviderName => "GHN";

    public GHNProvider(IConfiguration configuration, IHttpClientFactory httpClientFactory, ILogger<GHNProvider> logger)
    {
        _settings = new GHNSettings();
        configuration.GetSection("GHN").Bind(_settings);
        _httpClient = httpClientFactory.CreateClient("GHN");
        _logger = logger;
    }

    // ─── Tính phí vận chuyển ───────────────────────────────

    public async Task<ShippingFeeResult> CalculateFeeAsync(ShippingFeeRequest request)
    {
        try
        {
            var url = $"{_settings.BaseUrl}/shiip/public-api/v2/shipping-order/fee";

            var payload = new
            {
                from_district_id = request.FromDistrictId,
                from_ward_code = request.FromWardCode,
                to_district_id = request.ToDistrictId,
                to_ward_code = request.ToWardCode,
                weight = request.Weight,
                insurance_value = request.InsuranceValue,
                service_type_id = request.ServiceTypeId
            };

            var response = await SendRequestAsync<GhnFeeResponse>(url, payload);

            if (response?.Code == 200 && response.Data != null)
            {
                return new ShippingFeeResult
                {
                    Success = true,
                    TotalFee = response.Data.Total,
                    ServiceFee = response.Data.ServiceFee,
                    InsuranceFee = response.Data.InsuranceFee
                };
            }

            return new ShippingFeeResult
            {
                Success = false,
                Message = response?.Message ?? "Không thể tính phí vận chuyển từ GHN"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GHN CalculateFee error");
            return new ShippingFeeResult
            {
                Success = false,
                Message = $"Lỗi kết nối GHN: {ex.Message}"
            };
        }
    }

    // ─── Tạo vận đơn ──────────────────────────────────────

    public async Task<CreateShipmentResult> CreateShipmentAsync(CreateShipmentRequest request)
    {
        try
        {
            var url = $"{_settings.BaseUrl}/shiip/public-api/v2/shipping-order/create";

            var items = new List<object>();
            foreach (var item in request.Items)
            {
                items.Add(new
                {
                    name = item.Name,
                    quantity = item.Quantity,
                    weight = item.Weight
                });
            }

            var payload = new
            {
                payment_type_id = 1,
                required_note = "KHONGCHOXEMHANG",
                client_order_code = request.ClientOrderCode,
                from_name = request.FromName,
                from_phone = request.FromPhone,
                from_address = request.FromAddress,
                from_district_id = request.FromDistrictId,
                from_ward_code = request.FromWardCode,
                to_name = request.ToName,
                to_phone = request.ToPhone,
                to_address = request.ToAddress,
                to_district_id = request.ToDistrictId,
                to_ward_code = request.ToWardCode,
                cod_amount = (int)request.CodAmount,
                weight = request.Weight,
                insurance_value = request.InsuranceValue,
                service_type_id = 2,
                note = request.Note ?? "",
                items = items
            };

            var payloadJson = JsonSerializer.Serialize(payload);
            _logger.LogWarning("[GHN CreateShipment] Request payload: {Payload}", payloadJson);

            var response = await SendRequestAsync<GhnCreateOrderResponse>(url, payload);

            _logger.LogWarning("[GHN CreateShipment] Response: Code={Code}, Message='{Message}'", 
                response?.Code, response?.Message);

            if (response?.Code == 200 && response.Data != null)
            {
                return new CreateShipmentResult
                {
                    Success = true,
                    TrackingCode = response.Data.OrderCode,
                    GhnOrderCode = response.Data.OrderCode,
                    ExpectedDeliveryDate = response.Data.ExpectedDeliveryTime,
                    TotalFee = response.Data.TotalFee
                };
            }

            return new CreateShipmentResult
            {
                Success = false,
                Message = response?.Message ?? "Không thể tạo vận đơn GHN"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GHN CreateShipment error");
            return new CreateShipmentResult
            {
                Success = false,
                Message = $"Lỗi kết nối GHN: {ex.Message}"
            };
        }
    }

    public async Task<bool> CancelShipmentAsync(string ghnOrderCode)
    {
        try
        {
            var url = $"{_settings.BaseUrl}/shiip/public-api/v2/switch-status/cancel";
            var payload = new { order_codes = new[] { ghnOrderCode } };

            var response = await SendRequestAsync<GhnBaseResponse<object>>(url, payload);
            return response?.Code == 200;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GHN CancelShipment error for code: {Code}", ghnOrderCode);
            return false;
        }
    }

    // ─── GHN Address API (Proxy cho Frontend) ──────────────

    /// <summary>Lấy danh sách tỉnh/thành phố theo mã GHN</summary>
    public async Task<List<GhnProvince>> GetProvincesAsync()
    {
        try
        {
            var url = $"{_settings.BaseUrl}/shiip/public-api/master-data/province";
            var response = await SendGetRequestAsync<GhnBaseResponse<List<GhnProvince>>>(url);
            return response?.Data ?? new List<GhnProvince>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GHN GetProvinces error");
            return new List<GhnProvince>();
        }
    }

    /// <summary>Lấy danh sách quận/huyện theo ProvinceID GHN</summary>
    public async Task<List<GhnDistrict>> GetDistrictsAsync(int provinceId)
    {
        try
        {
            var url = $"{_settings.BaseUrl}/shiip/public-api/master-data/district";
            var response = await SendRequestAsync<GhnBaseResponse<List<GhnDistrict>>>(url, new { province_id = provinceId });
            return response?.Data ?? new List<GhnDistrict>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GHN GetDistricts error");
            return new List<GhnDistrict>();
        }
    }

    /// <summary>Lấy danh sách phường/xã theo DistrictID GHN</summary>
    public async Task<List<GhnWard>> GetWardsAsync(int districtId)
    {
        try
        {
            var url = $"{_settings.BaseUrl}/shiip/public-api/master-data/ward";
            var response = await SendRequestAsync<GhnBaseResponse<List<GhnWard>>>(url, new { district_id = districtId });
            return response?.Data ?? new List<GhnWard>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GHN GetWards error");
            return new List<GhnWard>();
        }
    }

    // ─── HTTP Helpers ──────────────────────────────────────

    private async Task<T?> SendRequestAsync<T>(string url, object payload) where T : class
    {
        var json = JsonSerializer.Serialize(payload);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Content = content;
        request.Headers.Add("Token", _settings.Token);
        request.Headers.Add("ShopId", _settings.ShopId);

        var response = await _httpClient.SendAsync(request);
        var responseBody = await response.Content.ReadAsStringAsync();

        _logger.LogInformation("GHN API [{Url}] Status={Status}", url, response.StatusCode);

        return JsonSerializer.Deserialize<T>(responseBody, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
    }

    private async Task<T?> SendGetRequestAsync<T>(string url) where T : class
    {
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Add("Token", _settings.Token);

        var response = await _httpClient.SendAsync(request);
        var responseBody = await response.Content.ReadAsStringAsync();

        _logger.LogInformation("GHN API GET [{Url}] Status={Status}", url, response.StatusCode);

        return JsonSerializer.Deserialize<T>(responseBody, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
    }
}

// ─── GHN Response Models ───────────────────────────────

internal class GhnBaseResponse<T>
{
    [JsonPropertyName("code")]
    public int Code { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }

    [JsonPropertyName("data")]
    public T? Data { get; set; }
}

internal class GhnFeeResponse : GhnBaseResponse<GhnFeeData> { }

internal class GhnFeeData
{
    [JsonPropertyName("total")]
    public decimal Total { get; set; }

    [JsonPropertyName("service_fee")]
    public decimal ServiceFee { get; set; }

    [JsonPropertyName("insurance_fee")]
    public decimal InsuranceFee { get; set; }
}

internal class GhnCreateOrderResponse : GhnBaseResponse<GhnCreateOrderData> { }

internal class GhnCreateOrderData
{
    [JsonPropertyName("order_code")]
    public string? OrderCode { get; set; }

    [JsonPropertyName("expected_delivery_time")]
    public DateTime? ExpectedDeliveryTime { get; set; }

    [JsonPropertyName("total_fee")]
    public decimal TotalFee { get; set; }
}

// ─── GHN Address Models ────────────────────────────────

public class GhnProvince
{
    [JsonPropertyName("ProvinceID")]
    public int ProvinceId { get; set; }

    [JsonPropertyName("ProvinceName")]
    public string? ProvinceName { get; set; }
}

public class GhnDistrict
{
    [JsonPropertyName("DistrictID")]
    public int DistrictId { get; set; }

    [JsonPropertyName("DistrictName")]
    public string? DistrictName { get; set; }

    [JsonPropertyName("ProvinceID")]
    public int ProvinceId { get; set; }
}

public class GhnWard
{
    [JsonPropertyName("WardCode")]
    public string? WardCode { get; set; }

    [JsonPropertyName("WardName")]
    public string? WardName { get; set; }

    [JsonPropertyName("DistrictID")]
    public int DistrictId { get; set; }
}
