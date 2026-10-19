using KoofrFolderNotifier.sql;
using System.Net;
using System.Net.Mail;
using Serilog;


namespace KoofrFolderNotifier
{

    internal interface IEmailSender
    {
        void Send(RecipientDto recipient, ScannedFileDto file);
    }

    internal sealed class GmailEmailSender : IEmailSender
    {
        private readonly string _fromAddress;
        private readonly string _fromPassword;

        public GmailEmailSender(string fromAddress, string fromPassword)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(fromAddress);
            ArgumentException.ThrowIfNullOrWhiteSpace(fromPassword);

            _fromAddress = fromAddress;
            _fromPassword = fromPassword;
        }

        public void Send(RecipientDto recipient, ScannedFileDto file)
        {
            ArgumentNullException.ThrowIfNull(recipient);
            ArgumentNullException.ThrowIfNull(file);

            var smtpClient = new SmtpClient
            {
                Host = "smtp.gmail.com",
                Port = 587,
                EnableSsl = true,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential(_fromAddress, _fromPassword)
            };

            var mail = new MailMessage
            {
                From = new MailAddress(_fromAddress),
                Subject = "NOVI ČLANEK V MAPI: " + file.FolderName,
                Body = $"Novi članek '{file.File}' je bil dodan v mapo '{file.FolderName}'."                
            };
            
            mail.To.Add(recipient.Email);

            try
            {
                smtpClient.Send(mail);
                Log.Information("Email sent successfully.");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to send email.");
            }
        }
    }
}
