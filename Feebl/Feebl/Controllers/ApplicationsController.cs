using Feebl.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;

namespace Feebl.Controllers
{
  public class ApplicationTile
  {
    public int ApplicationID;
    public string Application;
    public int CustomerID;
    public string Customer;
    public string Status;
    public int PassCount;
    public DateTime? LastRunTime;
    public IEnumerable<ProcessTile> Errors;
  }

  [Authorize]
  public class ApplicationsController : Controller
  {
    public ActionResult Index(int? customerID)
    {
      using (var ctx = new FeeblDataContext())
      {
        var user = (FeeblIdentity)FeeblPrincipal.Current.Identity;
        if (user.CustomerID.HasValue) customerID = user.CustomerID;

        var data = (from p in ctx.Processes
                    where (!user.ApplicationID.HasValue || p.ApplicationID == user.ApplicationID)
                        && (!customerID.HasValue || p.CustomerID == customerID)
                    select new
                    {
                      Application = p.Application.Name,
                      p.ApplicationID,
                      Customer = p.Customer.Name,
                      p.CustomerID,
                      p.ProcessID,
                      p.Name,
                      IsMet = p.Demands.All(d => d.IsMet),
                      IsLowPrio = p.Demands.All(d => d.IsMet || d.Priority < (int)Lists.Priority.Normal),
                      p.Demands.OrderBy(d => d.IsMet).FirstOrDefault().ErrorMessage,
                      p.LastRunTime,
                      SubscribedEmail = p.UserSubscriptions.Any(s => s.UserID == user.UserID && s.Email),
                      SubscribedSMS = p.UserSubscriptions.Any(s => s.UserID == user.UserID && s.SMS),
                      p.URL
                    }).ToList();

        var result = (from d in data
                      group d by new { d.Application, d.ApplicationID } into grouped
                      orderby grouped.Key.Application ascending
                      select new ApplicationTile
                      {
                        Customer = customerID.HasValue ? grouped.First().Customer : null,
                        CustomerID = customerID.HasValue ? grouped.First().CustomerID : 0,
                        Application = grouped.Key.Application,
                        ApplicationID = grouped.Key.ApplicationID,
                        PassCount = grouped.Count(d => d.IsMet),
                        Status = grouped.All(d => d.IsMet)
                                 ? "fg-color-white bg-color-green"
                                 : grouped.All(d => !d.IsMet)
                                   ? "fg-color-white bg-color-red"
                                   : "fg-color-white bg-color-orange",
                        LastRunTime = (from d in grouped
                                       orderby d.LastRunTime descending
                                       select d.LastRunTime).FirstOrDefault(),
                        Errors = (from pt in grouped
                                  group pt by new { pt.Customer, pt.CustomerID, pt.ProcessID, pt.Name, pt.URL } into process
                                  where process.Any(d => !d.IsMet && !d.IsLowPrio)
                                  orderby process.Key.Name ascending
                                  select new ProcessTile
                                  {
                                    ProcessID = process.Key.ProcessID,
                                    Application = grouped.Key.Application,
                                    ApplicationID = grouped.Key.ApplicationID,
                                    Customer = process.Key.Customer,
                                    CustomerID = process.Key.CustomerID,
                                    Name = process.Key.Name,
                                    ErrorMessage = process.OrderBy(d => d.IsMet).ThenByDescending(d => d.ErrorMessage).Select(d => d.ErrorMessage).FirstOrDefault(),
                                    Badge = process.All(d => d.IsMet)
                                    // ReSharper disable ReplaceWithStringIsNullOrEmpty
                                      ? process.Key.URL == "" || process.Key.URL == null ? "" : "available"
                                      : process.Key.URL == "" || process.Key.URL == null ? "error" : "busy",
                                    // ReSharper restore ReplaceWithStringIsNullOrEmpty
                                    Status = process.All(d => d.IsMet)
                                      ? "fg-color-white bg-color-green"
                                      : process.All(d => !d.IsMet)
                                        ? "fg-color-white bg-color-red"
                                        : "fg-color-white bg-color-orange",
                                    SubscribedEmail = process.First().SubscribedEmail,
                                    SubscribedSMS = process.First().SubscribedSMS,
                                  }).ToList()
                      }).ToList();

        return View(result);
      }
    }
  }
}