using System.ComponentModel.DataAnnotations;

namespace ProjectPortfolio2026.Server.Domain.WorkHistory;

public sealed class Employer
{
    public int Id { get; set; }

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(200)]
    public string? StreetAddress1 { get; set; }

    [MaxLength(200)]
    public string? StreetAddress2 { get; set; }

    [MaxLength(100)]
    public string? City { get; set; }

    [MaxLength(100)]
    public string? Region { get; set; }

    [MaxLength(30)]
    public string? PostalCode { get; set; }

    [MaxLength(100)]
    public string? Country { get; set; }

    public bool IsPublished { get; set; }

    public List<JobRole> JobRoles { get; set; } = [];
}
