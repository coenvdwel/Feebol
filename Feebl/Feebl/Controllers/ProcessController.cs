using Feebl.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;

namespace Feebl.Controllers
{
  public class ProcessTile
  {
    public string Application;
    public int ApplicationID;
    public string Customer;
    public int CustomerID;
    public int ProcessID;
    public string Name;
    public string Status;
    public string ErrorMessage;
    public DateTime? LastRunTime;
    public string Badge;
    public List<Demand> Demands;
    public List<Event> Events;
    public List<History> History;
    public int EventsToday;
    public int CountsToday;
    public int FailsToday;
    public bool SubscribedSMS;
    public bool SubscribedEmail;
    public string GroupID;
  }

  [Authorize]
  public class ProcessController : Controller
  {
    [HttpGet]
    public ActionResult Index(int customerID, int applicationID, string groupID)
    {
      using (var ctx = new FeeblDataContext())
      {
        var user = (FeeblIdentity)FeeblPrincipal.Current.Identity;

        if ((user.ApplicationID.HasValue && applicationID != user.ApplicationID.Value)
          || (user.CustomerID.HasValue && customerID != user.CustomerID.Value))
        {
          Response.StatusCode = 401;
          Response.End();
        }

        var list = (from p in ctx.Processes
                    where p.CustomerID == customerID
                      && p.ApplicationID == applicationID
                      && (!user.ApplicationID.HasValue || p.ApplicationID == user.ApplicationID)
                      && (!user.CustomerID.HasValue || p.CustomerID == user.CustomerID)
                      && ((groupID == null || groupID == string.Empty) || (p.GroupID == groupID))
                    select new ProcessTile
                    {
                      Application = p.Application.Name,
                      ApplicationID = p.ApplicationID,
                      Customer = p.Customer.Name,
                      CustomerID = p.CustomerID,
                      ProcessID = p.ProcessID,
                      Name = p.Name,
                      LastRunTime = p.LastRunTime,
                      ErrorMessage = p.Demands.OrderBy(d => d.IsMet).FirstOrDefault().ErrorMessage,
                      Badge = p.Demands.All(d => d.IsMet)
                      // ReSharper disable ReplaceWithStringIsNullOrEmpty
                        ? p.URL == "" || p.URL == null ? "" : "available"
                        : p.URL == "" || p.URL == null ? "error" : "busy",
                      // ReSharper restore ReplaceWithStringIsNullOrEmpty
                      FailsToday = p.Demands.Sum(d => (int?)d.Histories.Count(h => (h.Status == "Failed") && (h.CreationTime > DateTime.Now.AddDays(-1)))) ?? 0,
                      Status = p.Demands.All(d => d.IsMet)
                        ? "fg-color-white bg-color-green"
                        : p.Demands.Any(d => !d.IsMet && d.Priority >= (int)Lists.Priority.Normal)
                          ? "fg-color-white bg-color-red"
                          : "fg-color-white bg-color-orange",
                      SubscribedEmail = p.UserSubscriptions.Any(s => s.UserID == user.UserID && s.Email),
                      SubscribedSMS = p.UserSubscriptions.Any(s => s.UserID == user.UserID && s.SMS),
                      GroupID = p.GroupID
                    }).ToList();

        return View(list);
      }
    }
  }
}