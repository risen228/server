namespace Argon.Api.Grains.Interfaces;

public record SmtpConfig
{
    public string Host     { get; set; }
    public int    Port     { get; set; }
    public string User     { get; set; }
    public string Password { get; set; }
}

public interface IEmailManager : IGrainWithGuidKey
{
    Task SendEmailAsync(string email, string subject, string message, string template = "none");
}