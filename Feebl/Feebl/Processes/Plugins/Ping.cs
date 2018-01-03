using Feebl.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Feebl.Processes.Plugins
{
  public class Ping
  {
    public static void ProcessWebsites()
    {
      Dictionary<int, string> pings;
      using (var ctx = new FeeblDataContext())
      {
        pings = ctx.Processes
                   .Where(x => x.URL != string.Empty && x.URL != null)
                   .ToList()
                   .ToDictionary(x => x.ProcessID, y => y.URL);
      }

      foreach (var d in pings)
      {
        var processID = d.Key;
        var url = d.Value;

        Task.Factory.StartNew(() =>
        {
          try
          {
            using (var ctx = new FeeblDataContext())
            {
              int ms;
              if (!Methods.Ping(url, out ms)) return;

              var process = ctx.Processes.First(x => x.ProcessID == processID);

              process.LastRunTime = DateTime.UtcNow;

              ctx.Events.InsertOnSubmit(new Event
              {
                Process = process,
                CreationTime = DateTime.UtcNow,
                Counter = ms
              });

              ctx.SubmitChanges();
            }
          }
          catch
          {
            // swallow
          }
        });
      }
    }
  }
}