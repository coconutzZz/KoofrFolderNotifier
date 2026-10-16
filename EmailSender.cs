using KoofrFolderNotifier.sql;

namespace KoofrFolderNotifier
{
    internal interface IEmailSender
    {
        void Send(RecipientDto recipient, ScannedFileDto file);
    }

    internal sealed class MockEmailSender : IEmailSender
    {
        public void Send(RecipientDto recipient, ScannedFileDto file)
        {
            ArgumentNullException.ThrowIfNull(recipient);
            ArgumentNullException.ThrowIfNull(file);

            Console.WriteLine($"[Mock email] To: {recipient.Email} | New document: {file.Path}");
        }
    }
}
