using Feebl.Interfaces;
using System;
using System.Collections.Generic;
using System.Net.Mail;

namespace Feebl.Utilities
{
  public class Email : IMessage
  {
    private string _subject;

    public string Subject
    {
      get { return _subject; }
      set { _subject = value.Replace('\r', ' ').Replace('\n', ' '); }
    }

    public string Body { get; set; }
    private List<string> ToAddresses { get; set; }

    public Email()
    {
      ToAddresses = new List<string>();
    }

    public void AddReceipient(string email)
    {
      if (!ToAddresses.Contains(email)) ToAddresses.Add(email);
    }

    public List<string> GetReceipients()
    {
      return ToAddresses;
    }

    public void Send()
    {
      try
      {
        Body += $"<br /><br />Kind regards,<br /><a href='{Methods.GetUrl()}'>Feebl</a>";

        SendMessage();
        Log.Info("Email sent", Subject);
      }
      catch (Exception ex)
      {
        //Log.Error(ex);

        // below alternative logging is to catch the nasty error that keeps spamming me
        // logging exception does not provide information such as the Subject that could help me resolve this
        // after fixing, we can just use above Log.Error method again

        var name = ex.Message;
        var detail = ex.ToString();

        if (ex.Message.ToLower().Contains("specified string is not in the form required for a subject"))
        {
          detail += Environment.NewLine + Environment.NewLine + "Subject: '" + Subject + "'.";
        }

        Log.Error(name, detail);
      }
    }

    private void SendMessage()
    {
      var client = new SmtpClient("smtp.sendgrid.net", 587);

      var mail = new MailMessage
      {
        From = new MailAddress("feebl@diract-it.nl", "Feebl"),
        Subject = Subject,
        Body = Body,
        IsBodyHtml = true
      };

      mail.ReplyToList.Add(new MailAddress("c.wel@diract-it.nl", "Coen van der Wel"));
      mail.To.Add(new MailAddress("feebl@diract-it.nl", "Feebl"));

      foreach (var to in ToAddresses) mail.Bcc.Add(new MailAddress(to));

      client.Credentials = new System.Net.NetworkCredential(
        "",
        ""
        );

      client.Send(mail);
    }
  }
}