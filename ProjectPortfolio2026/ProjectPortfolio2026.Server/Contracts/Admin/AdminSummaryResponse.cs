using ProjectPortfolio2026.Server.Contracts;

namespace ProjectPortfolio2026.Server.Contracts.Admin;

public sealed class AdminSummaryResponse : ApiResponseDto
{
    public string Message { get; set; } = string.Empty;

    public string UserName { get; set; } = string.Empty;
}
