using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;

namespace Feebl.Utilities
{
  public static class Methods
  {
    public static string GetUrl(string url = "")
    {
      return @"https://feebl.diract-it.nl/" + url;
    }

    public static string GetTimeFromMinutes(int? minutes)
    {
      if (!minutes.HasValue) return string.Empty;

      var hours = minutes/60;
      minutes -= (hours*60);

      return (hours == 0 && minutes < 60)
        ? string.Format("{0}m", minutes)
        : (minutes == 0)
          ? string.Format("{0}h", hours)
          : string.Format("{0}h{1}m", hours, minutes);
    }

    public static string GetCounterValue(int? i)
    {
      if (!i.HasValue) return String.Empty;
      if (i/1000 >= 1) return String.Format("{0}k", i/1000);
      return i.ToString();
    }

    public static string GeneratePassword()
    {
      var random = new Random(DateTime.Now.Day + DateTime.Now.Hour + DateTime.Now.Minute + DateTime.Now.Second);
      var tokens = string.Format("{0:X2}", DateTime.Now.Ticks%2147483647);

      var limit = random.Next(tokens.Length + 1);
      var chars = tokens.Substring(tokens.Length - limit, limit).ToCharArray();

      Array.Reverse(chars);

      // 8-character random password
      return string.Format("{0}{1}", tokens.Substring(0, tokens.Length - limit), new string(chars)).ToUpper();
    }

    public static string EncryptPassword(string s)
    {
      s = s.Trim().ToUpper();
      if (string.IsNullOrEmpty(s)) return string.Empty;

      var sha = new SHA1CryptoServiceProvider();
      var enc = new UTF8Encoding();

      for (var i = 0; i < 777; i++)
      {
        var hash = BitConverter.ToString(sha.ComputeHash(enc.GetBytes(s)));
        s = hash.Replace("-", string.Empty).Replace("C", "7").Replace("O", "7").Replace("E", "7").Replace("N", "7");
      }

      // 40-character hash
      return s;
    }

    private static readonly List<string> Keywords = new List<string>
    {
      "internal server error",
      "404",
      "something has gone wrong"
    };

    public static bool Ping(string url, out int ms)
    {
      url = url.ToLower();
      if (!url.StartsWith("http://")) url = "http://" + url;

      var request = WebRequest.Create(url);
      request.Timeout = 10*1000;

      var sw = Stopwatch.StartNew();
      var response = (HttpWebResponse) request.GetResponse();
      sw.Stop();

      ms = (int) sw.ElapsedMilliseconds;

      if (response.StatusCode != HttpStatusCode.OK) return false;

      using (var s = response.GetResponseStream())
      {
        if (s == null) return false;
        using (var r = new StreamReader(s))
        {
          var text = r.ReadToEnd().ToLower();
          return Keywords.All(k => !text.Contains(k));
        }
      }
    }
  }
}