namespace Mahfoud.Identity.Services
{
    public interface IEmailSender
    {
        Task SendEmailAsync(string email, string subject, string htmlMessage);
    }

    public class SimpleEmailSender : IEmailSender
    {
        private ILogger _logger;

        public SimpleEmailSender(ILoggerFactory f)
        {
            _logger = f.CreateLogger<SimpleEmailSender>();
        }

        public Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            _logger.LogInformation("To: <{email}>\nSubject:{subject}\n{htmlMessage}", email, subject, htmlMessage);
            return Task.CompletedTask;
        }
    }
}
