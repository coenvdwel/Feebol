using Feebl.Interfaces;
using System;
using System.Collections.Generic;
using System.Xml;

namespace Feebl.Utilities
{
  public class SmsMessage : IMessage
  {
    public string Body;

    private const string Host =
      @"https://api.messagebird.com/xml/sms/?username={0}&password={1}&originator={2}&recipients={3}&message={4}";

    private readonly List<string> _toNumbers;

    public SmsMessage()
    {
      _toNumbers = new List<string>();
      Body = String.Empty;
    }

    public void AddReceipient(string mobile)
    {
      if (!_toNumbers.Contains(mobile)) _toNumbers.Add(mobile);
    }

    public List<string> GetReceipients()
    {
      return _toNumbers;
    }

    public void Send()
    {
      try
      {
        SendMessage();
        Log.Info("SMS sent", Body);
      }
      catch (Exception ex)
      {
        Log.Error(ex);
      }
    }

    private void SendMessage()
    {
      if (String.IsNullOrEmpty(Body))
      {
        throw new Exception("Message body empty.");
      }

      if (_toNumbers.Count == 0)
      {
        throw new Exception("No receipients.");
      }

      if (Body.Length > 160)
      {
        throw new Exception(String.Format("Body exceeds maximum character count of 160 (length = {0})", Body.Length));
      }


      var url = String.Format(Host,
        "",
        "",
        "Feebl",
        String.Join(",", _toNumbers.ToArray()),
        Body.Replace(" ", "%20")
        );

      var success = false;
      var msg = "No result.";

      var xmlReader = new XmlTextReader(url);
      while (xmlReader.Read())
      {
        if (xmlReader.NodeType != XmlNodeType.Element) continue;
        if (xmlReader.LocalName == "success") success = bool.Parse(xmlReader.ReadString());
        if (xmlReader.LocalName == "resultmessage") msg = xmlReader.ReadString();
      }

      if (!success) throw new Exception(msg);
    }
  }
}