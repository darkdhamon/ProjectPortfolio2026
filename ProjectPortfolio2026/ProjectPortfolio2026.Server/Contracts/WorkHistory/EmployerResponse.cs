namespace ProjectPortfolio2026.Server.Contracts.WorkHistory;

public sealed class EmployerResponse
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? StreetAddress1 { get; set; }

    public string? StreetAddress2 { get; set; }

    public string? City { get; set; }

    public string? Region { get; set; }

    public string? PostalCode { get; set; }

    public string? Country { get; set; }

    public List<JobRoleResponse> JobRoles { get; set; } = [];
}
