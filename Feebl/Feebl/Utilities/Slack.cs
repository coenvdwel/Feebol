using Feebl.Interfaces;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Net;

namespace Feebl.Utilities
{
  public class Slack : IMessage
  {
    public string Body { get; set; }
    public bool Success { get; set; }

    private List<string> Channels { get; set; }

    private const string Url = "https://hooks.slack.com/services/...";

    public Slack()
    {
      Channels = new List<string>();
    }

    public void AddReceipient(string channel)
    {
      if (!Channels.Contains(channel)) Channels.Add(channel);
    }

    public List<string> GetReceipients()
    {
      return Channels;
    }

    public void Send()
    {
      try
      {
        SendMessage();
        Log.Info("Slack updated", Body);
      }
      catch (Exception ex)
      {
        Log.Error(ex);
      }
    }

    private void SendMessage()
    {
      foreach (var c in Channels)
      {
        var channel = String.Format("#{0}_notifications", c).Replace(' ', '-');
        if (channel.Length > 21) channel = channel.Substring(0, 21);

        SendMessage(channel, Body, Success ? ":success:" : ":failure:", "Feebl");
      }
    }

    public static void SendMessage(string channel, string text, string icon, string username)
    {
      try
      {
        var p = new Payload
        {
          Channel = channel,
          Text = text,
          Icon = icon,
          Username = username
        };

        using (var client = new WebClient())
        {
          client.UploadValues(Url, "POST", new NameValueCollection
          {
            {"payload", JsonConvert.SerializeObject(p)}
          });
        }
      }
      // ReSharper disable once EmptyGeneralCatchClause
      catch
      {
        // swallow (for now)
      }
    }

    private class Payload
    {
      // ReSharper disable UnusedAutoPropertyAccessor.Local
      [JsonProperty("channel")]
      public string Channel { get; set; }

      [JsonProperty("text")]
      public string Text { get; set; }

      [JsonProperty("icon_emoji")]
      public string Icon { get; set; }

      [JsonProperty("username")]
      public string Username { get; set; }
      // ReSharper restore UnusedAutoPropertyAccessor.Local
    }
  }
}