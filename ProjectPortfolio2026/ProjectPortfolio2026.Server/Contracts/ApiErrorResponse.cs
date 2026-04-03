namespace ProjectPortfolio2026.Server.Contracts;

public sealed class ApiErrorResponse : ApiResponseDto
{
    public int StatusCode { get; set; }

    public string ErrorCode { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;

    public IDictionary<string, string[]>? ValidationErrors { get; set; }
}
