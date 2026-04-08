using ProjectPortfolio2026.Server.Contracts;

namespace ProjectPortfolio2026.Server.Contracts.WorkHistory;

public sealed class WorkHistoryResponse : ApiResponseDto
{
    public List<EmployerResponse> Items { get; set; } = [];
}
