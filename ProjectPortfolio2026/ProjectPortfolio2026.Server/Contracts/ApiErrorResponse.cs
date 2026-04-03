namespace ProjectPortfolio2026.Server.Contracts;

public sealed class ApiErrorResponse : ApiResponseDto
{
    public string ErrorCode { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;
}
