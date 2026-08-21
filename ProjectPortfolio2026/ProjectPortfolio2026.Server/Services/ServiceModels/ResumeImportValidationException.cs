namespace ProjectPortfolio2026.Server.Services.ServiceModels;

public sealed class ResumeImportValidationException : Exception
{
    public ResumeImportValidationException(string message)
        : base(message)
    {
    }

    public ResumeImportValidationException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
