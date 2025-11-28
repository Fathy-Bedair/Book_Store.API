using Microsoft.AspNetCore.Identity.UI.Services;
using System.Net;
using System.Net.Mail;

namespace Book_Store.API.Utitlies
{
    public class EmailSender : IEmailSender
    {
        public Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            var client = new SmtpClient("smtp.gmail.com", 587)
            {
                EnableSsl = true,
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential("fathysamy2004@gmail.com", "grew gcni gwfe idsb")
            };
            return client.SendMailAsync(
                new MailMessage(from: "fathysamy2004@gmail.com",
                            to: email,
                            subject,
                            htmlMessage
                            )
                {
                    IsBodyHtml = true
                });
        }
    }
}
