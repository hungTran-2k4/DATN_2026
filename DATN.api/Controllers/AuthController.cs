using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using DATN.Application.Features.Auth.Commands;
using DATN.Application.DTOs.Auth;
using DATN.Application.Interfaces.Services;
using DATN.Application.Common.Models;

namespace DATN.api.Controllers;

/// <summary>
/// Controller xử lý Authentication
/// </summary>
[Route("api/[controller]")]
[ApiController]
public class AuthController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<AuthController> _logger;
    private readonly ICurrentUserService _currentUserService;

    public AuthController(IMediator mediator, ILogger<AuthController> logger, ICurrentUserService currentUserService)
    {
        _mediator = mediator;
        _logger = logger;
        _currentUserService = currentUserService;
    }

    /// <summary>
    /// Đăng nhập
    /// </summary>
    /// <param name="request">Email và Password</param>
    /// <returns>JWT token nếu thành công</returns>
    [HttpPost("login")]
    [AllowAnonymous]
    [Produces("application/json")]
    [ProducesResponseType(typeof(ApiResponse<AuthResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<AuthResponse>), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<AuthResponse>>> Login([FromBody] LoginRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ApiResponse<AuthResponse>.Fail("Dữ liệu không hợp lệ", 400, "INVALID_DATA"));
        }

        var command = new LoginCommand(request.Email, request.Password);
        var result = await _mediator.Send(command);

        if (!result.Success)
        {
            return StatusCode(result.StatusCode, result);
        }

        SetTokenCookies(result.Data!.AccessToken!, result.Data!.RefreshToken!);
        
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>
    /// Đăng nhập bằng Firebase ID Token
    /// </summary>
    /// <param name="request">Firebase ID Token</param>
    /// <returns>JWT access token và refresh token</returns>
    [HttpPost("login-firebase")]
    [AllowAnonymous]
    [Produces("application/json")]
    [ProducesResponseType(typeof(ApiResponse<AuthResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<AuthResponse>), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<AuthResponse>>> LoginWithFirebase([FromBody] LoginWithFirebaseRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ApiResponse<AuthResponse>.Fail("Dữ liệu không hợp lệ", 400, "INVALID_DATA"));
        }

        var command = new LoginWithFirebaseCommand(request.Email, request.Password);
        var result = await _mediator.Send(command);

        if (!result.Success)
        {
            return StatusCode(result.StatusCode, result);
        }

        SetTokenCookies(result.Data!.AccessToken!, result.Data!.RefreshToken!);
        
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>
    /// Đăng ký tài khoản mới qua Firebase
    /// </summary>
    /// <param name="request">Thông tin đăng ký</param>
    /// <returns>Kết quả đăng ký</returns>
    [HttpPost("register-firebase")]
    [AllowAnonymous]
    [Produces("application/json")]
    [ProducesResponseType(typeof(ApiResponse<AuthResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<AuthResponse>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<AuthResponse>>> RegisterWithFirebase([FromBody] RegisterWithFirebaseRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ApiResponse<AuthResponse>.Fail("Dữ liệu không hợp lệ", 400, "INVALID_DATA"));
        }

        var command = new RegisterWithFirebaseCommand(request.Email, request.Password, request.FullName);
        var result = await _mediator.Send(command);

        if (!result.Success)
        {
            return StatusCode(result.StatusCode, result);
        }

        return StatusCode(201, result);
    }

    /// <summary>
    /// Đăng ký tài khoản mới (Local)
    /// </summary>
    /// <param name="request">Thông tin đăng ký</param>
    /// <returns>User info nếu thành công</returns>
    [HttpPost("register")]
    [AllowAnonymous]
    [Produces("application/json")]
    [ProducesResponseType(typeof(ApiResponse<AuthResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<AuthResponse>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<AuthResponse>>> Register([FromBody] RegisterRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ApiResponse<AuthResponse>.Fail("Dữ liệu không hợp lệ", 400, "INVALID_DATA"));
        }

        var command = new RegisterCommand(request.Email, request.Password, request.FullName);
        var result = await _mediator.Send(command);

        if (!result.Success)
        {
            return StatusCode(result.StatusCode, result);
        }

        SetTokenCookies(result.Data!.AccessToken!, result.Data!.RefreshToken!);
        
        return StatusCode(201, result);
    }

    /// <summary>
    /// Refresh Access Token tự động bằng Cookie
    /// </summary>
    [HttpPost("refresh-token")]
    [AllowAnonymous]
    [Produces("application/json")]
    [ProducesResponseType(typeof(ApiResponse<AuthResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<AuthResponse>), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<AuthResponse>>> RefreshToken()
    {
        var refreshToken = Request.Cookies["refresh_token"];

        if (string.IsNullOrEmpty(refreshToken))
        {
            return Unauthorized(ApiResponse<AuthResponse>.Fail("Refresh Token is missing", 401, "MISSING_TOKEN"));
        }

        var command = new RefreshTokenCommand(refreshToken);
        var result = await _mediator.Send(command);

        if (!result.Success)
        {
            return StatusCode(result.StatusCode, result);
        }

        SetTokenCookies(result.Data!.AccessToken!, result.Data!.RefreshToken!);
        
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>
    /// Nâng cấp tài khoản thành Seller: tạo Shop + gán role Seller
    /// </summary>
    [HttpPost("register-as-seller")]
    [Authorize]
    [Produces("application/json")]
    [ProducesResponseType(typeof(ApiResponse<Guid>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<Guid>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<Guid>>> RegisterAsSeller([FromBody] RegisterAsSellerCommand command)
    {
        if (!_currentUserService.IsAuthenticated || _currentUserService.UserId == null)
        {
            return Unauthorized(ApiResponse<Guid>.Fail("Không có quyền truy cập", 401, "UNAUTHORIZED"));
        }

        command.UserId = _currentUserService.UserId.Value;
        var result = await _mediator.Send(command);

        return StatusCode(result.StatusCode, result);
    }

    private void SetTokenCookies(string accessToken, string refreshToken)
    {
        var cookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = true, // Set to true in Production
            SameSite = SameSiteMode.None, // Required for cross-site cookie usage
            Expires = DateTime.UtcNow.AddMinutes(30)
        };

        Response.Cookies.Append("access_token", accessToken, cookieOptions);

        var refreshCookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.None,
            Expires = DateTime.UtcNow.AddDays(7)
        };

        Response.Cookies.Append("refresh_token", refreshToken, refreshCookieOptions);
    }

    /// <summary>
    /// Lấy thông tin user hiện tại (yêu cầu đăng nhập)
    /// </summary>
    [HttpGet("current-user")]
    [Authorize]
    [Produces("application/json")]
    [ProducesResponseType(typeof(ApiResponse<UserDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public ActionResult<ApiResponse<UserDto>> GetCurrentUser()
    {
        if (!_currentUserService.IsAuthenticated || _currentUserService.UserId == null)
        {
            return Unauthorized(ApiResponse<UserDto>.Fail("Không có quyền truy cập", 401, "UNAUTHORIZED"));
        }

        var response = ApiResponse<UserDto>.Succeed(new UserDto
        {
            Id = _currentUserService.UserId.Value,
            Email = _currentUserService.Email ?? "",
            FullName = User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value, 
            Roles = _currentUserService.Roles.ToList()
        }, "Lấy thông tin người dùng thành công");

        return StatusCode(response.StatusCode, response);
    }

    /// <summary>
    /// Yêu cầu đặt lại mật khẩu (gửi email link reset)
    /// </summary>
    [HttpPost("forgot-password")]
    [AllowAnonymous]
    [Produces("application/json")]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<string>>> ForgotPassword([FromBody] ForgotPasswordRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ApiResponse<string>.Fail("Dữ liệu không hợp lệ", 400, "INVALID_DATA"));
        }

        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var command = new ForgotPasswordCommand(request.Email, ipAddress);
        var result = await _mediator.Send(command);

        return StatusCode(result.StatusCode, result);
    }

    /// <summary>
    /// Đặt lại mật khẩu bằng token từ email
    /// </summary>
    [HttpPost("reset-password")]
    [AllowAnonymous]
    [Produces("application/json")]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<string>>> ResetPassword([FromBody] ResetPasswordRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ApiResponse<string>.Fail("Dữ liệu không hợp lệ", 400, "INVALID_DATA"));
        }

        var command = new ResetPasswordCommand(request.Email, request.Token, request.NewPassword);
        var result = await _mediator.Send(command);

        return StatusCode(result.StatusCode, result);
    }

    /// <summary>
    /// Đăng xuất
    /// </summary>
    [HttpPost("logout")]
    [AllowAnonymous]
    [Produces("application/json")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<bool>>> Logout()
    {
        var refreshToken = Request.Cookies["refresh_token"];
        
        if (!string.IsNullOrEmpty(refreshToken))
        {
            var command = new LogoutCommand(refreshToken);
            await _mediator.Send(command);
        }

        // Xoá cookies
        var cookieOptions = new CookieOptions { Secure = true, SameSite = SameSiteMode.None };
        Response.Cookies.Delete("access_token", cookieOptions);
        Response.Cookies.Delete("refresh_token", cookieOptions);

        return Ok(ApiResponse<bool>.Succeed(true, "Đăng xuất thành công"));
    }
}
