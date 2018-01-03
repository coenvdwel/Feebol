using System;

// ReSharper disable once CheckNamespace
namespace Feebl
{
  public static class DateTimeExtensions
  {
    public static DateTime ToCET(this DateTime utc)
    {
      return ConvertToCET(utc);
    }

    public static DateTime? ToCET(this DateTime? utc)
    {
      if (!utc.HasValue) return null;
      return ConvertToCET(utc.Value);
    }

    public static DateTime? ConvertToCET(DateTime? utc)
    {
      if (!utc.HasValue) return null;
      return ConvertToCET(utc.Value);
    }

    public static DateTime ConvertToCET(DateTime utc)
    {
      return TimeZoneInfo.ConvertTimeFromUtc(utc, TimeZoneInfo.FindSystemTimeZoneById("W. Europe Standard Time"));
    }

    public static int DefaultUtcOffset
    {
      get
      {
        DateTime now = DateTime.UtcNow, cet = now.ToCET();
        return (int) ((cet - now).TotalHours);
      }
    }
  }
}