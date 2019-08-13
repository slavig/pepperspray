using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;

using pepperspray.SharedServices;
using pepperspray.Utils;

namespace pepperspray.RestAPIServer.Services
{
  internal class MailService
  {
    private Configuration config = DI.Get<Configuration>();
    private SmtpClient client;

    public MailService()
    {
      this.client = new SmtpClient(this.config.Mail.ServerAddress, this.config.Mail.ServerPort);
      this.client.Credentials = new NetworkCredential(this.config.Mail.Username, this.config.Mail.Password);
      this.client.EnableSsl = true;
    }

    internal void SendMessage(string to, string subject, string bodyFormat, params object[] args)
    {
      this.client.Send(new MailMessage(
        this.config.Mail.Address, 
        to, 
        subject, 
        String.Format(bodyFormat, args)
        ));
    }
  }
}
