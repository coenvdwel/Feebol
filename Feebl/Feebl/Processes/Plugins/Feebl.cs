using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Feebl.Processes.Plugins
{
  internal static class Feebl
  {
    public static void Cleanup()
    {
      using (var ctx = new FeeblDataContext())
      {
        ctx.ExecuteCommand("EXECUTE dbo.[Cleanup]");
      }
    }

    public static void UpdateSchedule()
    {
      using (var ctx = new FeeblDataContext())
      {
        ctx.ExecuteCommand("EXECUTE dbo.[UpdateSchedule]");
      }
    }

    public static void CheckDemands()
    {
      List<int> demandIDs;
      using (var ctx = new FeeblDataContext())
      {
        demandIDs = (from d in ctx.Demands select d.DemandID).ToList();
      }

      foreach (var demandID in demandIDs)
      {
        var d = demandID;

        Task.Factory.StartNew(() =>
        {
          try
          {
            using (var ctx = new FeeblDataContext())
            {
              var demand = ctx.Demands.FirstOrDefault(x => x.DemandID == d);
              if (demand == null) return;

              demand.Check(ctx);
              ctx.SubmitChanges();
            }
          }
          catch (Exception ex)
          {
            Utilities.Log.Error(ex);
          }
        });
      }
    }
  }
}