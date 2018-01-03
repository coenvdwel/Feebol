using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Web.Services;

namespace Feebl
{
  [WebService(Namespace = "urn:fcd:diract-it.nl:feebl")]
  [WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
  [System.ComponentModel.ToolboxItem(false)]
  public class Service : WebService
  {
    public const int Limit = 1;
    public const int Refresh = 200;
    public static Dictionary<string, long> TokenBucket = new Dictionary<string, long>();

    [WebMethod]
    public void Update(string applicationID, string customerID, string processID, int counter, string hash)
    {
      Update(applicationID, customerID, processID, counter, hash, DateTime.UtcNow);
    }

    public void Update(string applicationID, string customerID, string processID, int counter, string hash, DateTime utcProcessingTime)
    {
      #region Rate limiting (TokenBucket)

      while (TokenBucket.ContainsKey(processID) && TokenBucket[processID] > DateTime.Now.AddSeconds(-Limit).Ticks) Thread.Sleep(Refresh);
      TokenBucket[processID] = DateTime.Now.Ticks;

      #endregion

      #region Check challenge

      HashAlgorithm algorithm = SHA1.Create();
      var x = algorithm.ComputeHash(Encoding.UTF8.GetBytes(applicationID + customerID + processID + counter));
      var sb = new StringBuilder();
      foreach (var b in x) sb.Append(b.ToString("X2"));
      var counterchallenge = sb[17] + sb[Math.Abs(counter)%sb.Length] + sb.ToString().Substring(1, 7);

      if (counterchallenge != hash) throw new ArgumentException("Invalid hash.");

      #endregion

      using (var ctx = new FeeblDataContext())
      {
        var application = ctx.Applications.FirstOrDefault(a => a.Name == applicationID.Trim()) ?? new Application {Name = applicationID};
        var customer = ctx.Customers.FirstOrDefault(c => c.Name == customerID.Trim()) ?? new Customer {Name = customerID};
        var process = ctx.Processes.FirstOrDefault(p => p.Application == application && p.Customer == customer && p.Name == processID) ??
                      new Process {Application = application, Customer = customer, Name = processID, URL = string.Empty, GroupID = string.Empty};

        process.LastRunTime = utcProcessingTime;

        var groupIndex = processID.IndexOf("@", StringComparison.Ordinal);
        if (groupIndex > 0) process.GroupID = processID.Substring(groupIndex + 1).Trim();

        ctx.Events.InsertOnSubmit(new Event
        {
          Process = process,
          CreationTime = utcProcessingTime,
          Counter = counter
        });

        ctx.SubmitChanges();
      }
    }
  }
}