using Feebl.Processes.Plugins;
using System;
using System.Threading;
using System.Threading.Tasks;

// ReSharper disable FunctionNeverReturns

namespace Feebl.Processes
{
  public static class Process
  {
    private const int Second = 1000;
    private const int Minute = 60*Second;
    private const int Hour = 60*Minute;
    private const int Day = 24*Hour;

    public static void Init()
    {
      Task.Factory.StartNew(() =>
      {
        while (true)
        {
          var now = DateTime.Now;
          var showtime = new DateTime(now.Year, now.Month, now.Day + 7, 3, 30, 0);

          Thread.Sleep(showtime - now);

          try
          {
            Plugins.Feebl.Cleanup();
          }
          catch (Exception ex)
          {
            Utilities.Log.Error(ex);
          }
        }
      });

      Task.Factory.StartNew(() =>
      {
        while (true)
        {
          try
          {
            Plugins.Feebl.UpdateSchedule();
          }
          catch (Exception ex)
          {
            Utilities.Log.Error(ex);
          }

          Thread.Sleep(Day);
        }
      });

      Task.Factory.StartNew(() =>
      {
        while (true)
        {
          try
          {
            Ping.ProcessWebsites();
          }
          catch (Exception ex)
          {
            Utilities.Log.Error(ex);
          }

          Thread.Sleep(5*Minute);
        }
      });

      Task.Factory.StartNew(() =>
      {
        while (true)
        {
          try
          {
            BI.ProcessMails();
            Plugins.Feebl.CheckDemands();
          }
          catch (Exception ex)
          {
            Utilities.Log.Error(ex);
          }

          Thread.Sleep(Minute);
        }
      });
    }
  }
}