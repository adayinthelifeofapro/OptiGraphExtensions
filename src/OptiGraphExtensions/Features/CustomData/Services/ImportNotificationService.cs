using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OptiGraphExtensions.Entities;
using OptiGraphExtensions.Features.CustomData.Models;
using OptiGraphExtensions.Features.CustomData.Services.Abstractions;

namespace OptiGraphExtensions.Features.CustomData.Services
{
    /// <summary>
    /// Service for sending email notifications about import execution status.
    /// </summary>
    public class ImportNotificationService : IImportNotificationService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<ImportNotificationService> _logger;

        public ImportNotificationService(
            IConfiguration configuration,
            ILogger<ImportNotificationService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        /// <inheritdoc />
        public async Task SendFailureNotificationAsync(ImportConfiguration config, ImportResult result, int totalAttempts)
        {
            if (string.IsNullOrWhiteSpace(config.NotificationEmail))
            {
                _logger.LogDebug("No notification email configured for {ConfigName}, skipping failure notification", config.Name);
                return;
            }

            var subject = $"[OptiGraph Import Failed] {config.Name}";
            var body = BuildFailureEmailBody(config, result, totalAttempts);

            await SendEmailAsync(config.NotificationEmail, subject, body);
        }

        /// <inheritdoc />
        public async Task SendRecoveryNotificationAsync(ImportConfiguration config, ImportResult result)
        {
            if (string.IsNullOrWhiteSpace(config.NotificationEmail))
            {
                _logger.LogDebug("No notification email configured for {ConfigName}, skipping recovery notification", config.Name);
                return;
            }

            var subject = $"[OptiGraph Import Recovered] {config.Name}";
            var body = BuildRecoveryEmailBody(config, result);

            await SendEmailAsync(config.NotificationEmail, subject, body);
        }

        private async Task SendEmailAsync(string toEmail, string subject, string body)
        {
            try
            {
                var smtpHost = _configuration["Smtp:Host"];
                var smtpPortStr = _configuration["Smtp:Port"];
                var smtpUser = _configuration["Smtp:Username"];
                var smtpPassword = _configuration["Smtp:Password"];
                var fromEmail = _configuration["Smtp:FromEmail"] ?? "noreply@optimizely.com";

                if (string.IsNullOrWhiteSpace(smtpHost))
                {
                    _logger.LogWarning("SMTP not configured. Would have sent email to {Email}: {Subject}", toEmail, subject);
                    _logger.LogDebug("Email body: {Body}", body);
                    return;
                }

                var smtpPort = int.TryParse(smtpPortStr, out var port) ? port : 587;

                using var client = new SmtpClient(smtpHost, smtpPort);

                if (!string.IsNullOrWhiteSpace(smtpUser) && !string.IsNullOrWhiteSpace(smtpPassword))
                {
                    client.Credentials = new NetworkCredential(smtpUser, smtpPassword);
                }

                client.EnableSsl = true;

                var message = new MailMessage(fromEmail, toEmail, subject, body)
                {
                    IsBodyHtml = true
                };

                await client.SendMailAsync(message);

                _logger.LogInformation("Sent notification email to {Email}: {Subject}", toEmail, subject);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send notification email to {Email}: {Subject}", toEmail, subject);
            }
        }

        private static string BuildFailureEmailBody(ImportConfiguration config, ImportResult result, int totalAttempts)
        {
            var errors = string.Join("<br/>", result.Errors.Select(e => $"&bull; {WebUtility.HtmlEncode(e)}"));

            return $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .header {{ background-color: #dc3545; color: white; padding: 20px; }}
        .content {{ padding: 20px; }}
        .details {{ background-color: #f8f9fa; padding: 15px; border-radius: 5px; margin: 15px 0; }}
        .label {{ font-weight: bold; color: #666; }}
        .error {{ color: #dc3545; }}
        .footer {{ padding: 20px; font-size: 12px; color: #666; border-top: 1px solid #ddd; }}
    </style>
</head>
<body>
    <div class='header'>
        <h2>Import Failed: {WebUtility.HtmlEncode(config.Name)}</h2>
    </div>
    <div class='content'>
        <p>The scheduled import <strong>{WebUtility.HtmlEncode(config.Name)}</strong> has failed after {totalAttempts} attempt(s).</p>

        <div class='details'>
            <p><span class='label'>Configuration:</span> {WebUtility.HtmlEncode(config.Name)}</p>
            <p><span class='label'>Source ID:</span> {WebUtility.HtmlEncode(config.TargetSourceId)}</p>
            <p><span class='label'>Content Type:</span> {WebUtility.HtmlEncode(config.TargetContentType)}</p>
            <p><span class='label'>API URL:</span> {WebUtility.HtmlEncode(config.ApiUrl)}</p>
            <p><span class='label'>Duration:</span> {result.Duration.TotalSeconds:F1} seconds</p>
        </div>

        <h3 class='error'>Errors:</h3>
        <div class='details'>
            {(string.IsNullOrEmpty(errors) ? "<p>No specific error details available.</p>" : $"<p>{errors}</p>")}
        </div>

        <h3>Statistics:</h3>
        <div class='details'>
            <p><span class='label'>Items Received:</span> {result.TotalItemsReceived}</p>
            <p><span class='label'>Items Imported:</span> {result.ItemsImported}</p>
            <p><span class='label'>Items Skipped:</span> {result.ItemsSkipped}</p>
            <p><span class='label'>Items Failed:</span> {result.ItemsFailed}</p>
        </div>

        <p>The import will be attempted again at the next scheduled time. Please review the configuration and external API to resolve the issue.</p>
    </div>
    <div class='footer'>
        <p>This is an automated message from OptiGraph Extensions.</p>
    </div>
</body>
</html>";
        }

        private static string BuildRecoveryEmailBody(ImportConfiguration config, ImportResult result)
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .header {{ background-color: #28a745; color: white; padding: 20px; }}
        .content {{ padding: 20px; }}
        .details {{ background-color: #f8f9fa; padding: 15px; border-radius: 5px; margin: 15px 0; }}
        .label {{ font-weight: bold; color: #666; }}
        .success {{ color: #28a745; }}
        .footer {{ padding: 20px; font-size: 12px; color: #666; border-top: 1px solid #ddd; }}
    </style>
</head>
<body>
    <div class='header'>
        <h2>Import Recovered: {WebUtility.HtmlEncode(config.Name)}</h2>
    </div>
    <div class='content'>
        <p class='success'>Good news! The scheduled import <strong>{WebUtility.HtmlEncode(config.Name)}</strong> has recovered and completed successfully.</p>

        <div class='details'>
            <p><span class='label'>Configuration:</span> {WebUtility.HtmlEncode(config.Name)}</p>
            <p><span class='label'>Source ID:</span> {WebUtility.HtmlEncode(config.TargetSourceId)}</p>
            <p><span class='label'>Content Type:</span> {WebUtility.HtmlEncode(config.TargetContentType)}</p>
            <p><span class='label'>Duration:</span> {result.Duration.TotalSeconds:F1} seconds</p>
        </div>

        <h3>Statistics:</h3>
        <div class='details'>
            <p><span class='label'>Items Received:</span> {result.TotalItemsReceived}</p>
            <p><span class='label'>Items Imported:</span> {result.ItemsImported}</p>
            <p><span class='label'>Items Skipped:</span> {result.ItemsSkipped}</p>
        </div>
    </div>
    <div class='footer'>
        <p>This is an automated message from OptiGraph Extensions.</p>
    </div>
</body>
</html>";
        }
    }
}
