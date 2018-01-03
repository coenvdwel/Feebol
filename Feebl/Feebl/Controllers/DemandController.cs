using System.Globalization;
using Feebl.Utilities;
using System;
using System.Linq;
using System.Web.Mvc;

namespace Feebl.Controllers
{
  [Authorize]
  public class DemandController : Controller
  {
    [HttpGet]
    public ActionResult Index(int processID, bool showAll = false)
    {
      using (var ctx = new FeeblDataContext())
      {
        var user = (FeeblIdentity)FeeblPrincipal.Current.Identity;

        var eventCount = showAll ? 9999999 : 13;

        var pt = (from p in ctx.Processes
                  where p.ProcessID == processID
                  select new ProcessTile
                  {
                    Application = p.Application.Name,
                    ApplicationID = p.ApplicationID,
                    Customer = p.Customer.Name,
                    CustomerID = p.CustomerID,
                    ProcessID = p.ProcessID,
                    Name = p.Name,
                    Demands = p.Demands.ToList(),
                    Events = p.Events.OrderByDescending(e => e.CreationTime).Take(eventCount).ToList(),
                    EventsToday = p.Events.Count(e => e.CreationTime > DateTime.Now.AddDays(-1)),
                    CountsToday = p.Events.Where(e => e.CreationTime > DateTime.Now.AddDays(-1)).Select(e => e.Counter).Sum() ?? 0,
                    FailsToday = p.Demands.Sum(d => (int?)d.Histories.Count(h => (h.Status == "Failed") && (h.CreationTime > DateTime.Now.AddDays(-1)))) ?? 0,
                    History = p.Demands.SelectMany(d => d.Histories).OrderByDescending(h => h.CreationTime).Take(6).ToList(),
                    ErrorMessage = p.Demands.OrderBy(d => d.IsMet).FirstOrDefault().ErrorMessage,
                    Badge = p.Demands.All(d => d.IsMet)
                    // ReSharper disable ReplaceWithStringIsNullOrEmpty
                      ? p.URL == "" || p.URL == null ? "" : "available"
                      : p.URL == "" || p.URL == null ? "error" : "busy",
                    // ReSharper restore ReplaceWithStringIsNullOrEmpty
                    Status = p.Demands.All(d => d.IsMet)
                      ? "fg-color-white bg-color-green"
                      : p.Demands.All(d => !d.IsMet)
                        ? "fg-color-white bg-color-red"
                        : "fg-color-white bg-color-orange",
                    SubscribedEmail = p.UserSubscriptions.Any(s => s.UserID == user.UserID && s.Email),
                    SubscribedSMS = p.UserSubscriptions.Any(s => s.UserID == user.UserID && s.SMS)
                  }).First();

        if ((user.ApplicationID.HasValue && pt.ApplicationID != user.ApplicationID)
          || (user.CustomerID.HasValue && pt.CustomerID != user.CustomerID))
        {
          Response.StatusCode = 401;
          Response.End();
        }

        return View(pt);
      }
    }

    [HttpGet]
    public ActionResult Subscribe(int id, string type)
    {
      using (var ctx = new FeeblDataContext())
      {
        var user = (FeeblIdentity)FeeblPrincipal.Current.Identity;

        if(user.UserID == 0) throw new Exception("User lookup mismatch, please try again.");

        var sub = (from x in ctx.UserSubscriptions
                   where x.UserID == user.UserID
                   && x.ProcessID == id
                   select x).FirstOrDefault();

        if (sub == null)
        {
          sub = new UserSubscription { UserID = user.UserID, ProcessID = id };
          ctx.UserSubscriptions.InsertOnSubmit(sub);
        }

        if(type == "email") sub.Email = !sub.Email;
        if(type == "sms") sub.SMS = !sub.SMS;

        if (!sub.Email && !sub.SMS) ctx.UserSubscriptions.DeleteOnSubmit(sub);

        ctx.SubmitChanges();

        TempData["Success"] = "Your subscription was successfully changed.";

        return RedirectToAction("Index", new { processID = id });
      }
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    public ActionResult Index(int processID, string ignoreUntil, string remark)
    {
      using (var ctx = new FeeblDataContext())
      {
        var process = (from p in ctx.Processes
                       where p.ProcessID == processID
                       select p).First();

        var user = (FeeblIdentity)FeeblPrincipal.Current.Identity;
        if ((user.ApplicationID.HasValue && process.ApplicationID != user.ApplicationID.Value)
          || (user.CustomerID.HasValue && process.CustomerID != user.CustomerID.Value))
        {
          Response.StatusCode = 401;
          Response.End();
        }

        if (String.IsNullOrEmpty(remark)) remark = "Ignored";
        
        DateTime? ignoreUntilDate = null;
        if (!String.IsNullOrEmpty(ignoreUntil))
        {
          DateTime dt;
          var ci = CultureInfo.InvariantCulture;

          if (!DateTime.TryParseExact(ignoreUntil, "yyyy/MM/dd HH:mm", ci, DateTimeStyles.None, out dt))
          {
            TempData["Error"] = "Please fill in a valid Ignore Until date (yyyy/MM/dd HH:mm).";

            return RedirectToAction("Index", new {processID});
          }

          var utcOffset = process.Demands.Select(d => d.UtcOffset).FirstOrDefault() ?? DateTimeExtensions.DefaultUtcOffset;
          ignoreUntilDate = dt.AddHours(-utcOffset).AddMinutes(-1);
        }

        foreach (var d in process.Demands)
        {
          if (d.IsMet)
          {
            if (ignoreUntilDate.HasValue) d.CalculateNextRunTime(ignoreUntilDate.Value);
          }
          else
          {
            d.Update(ctx, true, remark, null);
            d.CalculateNextRunTime(ignoreUntilDate);
          }
        }

        ctx.SubmitChanges();

        TempData["Success"] = "Failed process demands are now ignored, and will be checked at the next expected run time.";

        return RedirectToAction("Index", new { processID });
      }
    }

    [Authorize(Roles="Admin")]
    public ViewResult Create()
    {
      return View(new Demand());
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    public ActionResult Create(int id, Demand d)
    {
      using (var ctx = new FeeblDataContext())
      {
        d.ProcessID = id;

        if (!TryUpdateModel(d))
        {
          TempData["Error"] = "Failure creating Demand.";
          return View(d);
        }

        d.IsMet = true;

        d.CalculateNextRunTime();

        ctx.Demands.InsertOnSubmit(d);
        ctx.SubmitChanges();

        TempData["Success"] = "Successfully created Demand.";
        return RedirectToAction("Index", new { processID = id });
      }
    }

    [Authorize(Roles = "Admin")]
    public ViewResult Edit(int id)
    {
      using (var ctx = new FeeblDataContext())
      {
        var demand = (from d in ctx.Demands
                      where d.DemandID == id
                      select d).First();

        var user = (FeeblIdentity)FeeblPrincipal.Current.Identity;
        if ((user.ApplicationID.HasValue && demand.Process.ApplicationID != user.ApplicationID.Value)
          || (user.CustomerID.HasValue && demand.Process.CustomerID != user.CustomerID.Value))
        {
          Response.StatusCode = 401;
          Response.End();
        }

        return View(demand);
      }
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    public ActionResult Edit(int id, Demand dummy)
    {
      using (var ctx = new FeeblDataContext())
      {
        var demand = (from d in ctx.Demands
                      where d.DemandID == id
                      select d).First();

        var user = (FeeblIdentity)FeeblPrincipal.Current.Identity;
        if ((user.ApplicationID.HasValue && demand.Process.ApplicationID != user.ApplicationID.Value)
          || (user.CustomerID.HasValue && demand.Process.CustomerID != user.CustomerID.Value))
        {
          Response.StatusCode = 401;
          Response.End();
        }

        if (!TryUpdateModel(demand))
        {
          TempData["Error"] = "Failure updating Demand.";
          return View(demand);
        }

        demand.CalculateNextRunTime();

        ctx.SubmitChanges();
        TempData["Success"] = "Successfully updated Demand.";
        return RedirectToAction("Index", new { processID = demand.ProcessID });
      }
    }

    [Authorize(Roles = "Admin")]
    public ActionResult Delete(int id)
    {
      using (var ctx = new FeeblDataContext())
      {
        var demand = (from d in ctx.Demands
                      where d.DemandID == id
                      select d).First();

        var user = (FeeblIdentity)FeeblPrincipal.Current.Identity;
        if ((user.ApplicationID.HasValue && demand.Process.ApplicationID != user.ApplicationID.Value)
          || (user.CustomerID.HasValue && demand.Process.CustomerID != user.CustomerID.Value))
        {
          Response.StatusCode = 401;
          Response.End();
        }

        return View(demand);
      }
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    public ActionResult Delete(int id, FormCollection collection)
    {
      using (var ctx = new FeeblDataContext())
      {
        var demand = (from d in ctx.Demands
                      where d.DemandID == id
                      select d).First();

        var user = (FeeblIdentity)FeeblPrincipal.Current.Identity;
        if ((user.ApplicationID.HasValue && demand.Process.ApplicationID != user.ApplicationID.Value)
          || (user.CustomerID.HasValue && demand.Process.CustomerID != user.CustomerID.Value))
        {
          Response.StatusCode = 401;
          Response.End();
        }

        var processID = demand.ProcessID;

        ctx.Histories.DeleteAllOnSubmit(demand.Histories);
        ctx.Demands.DeleteOnSubmit(demand);
        ctx.SubmitChanges();

        TempData["Success"] = "Successfully deleted Demand.";
        return RedirectToAction("Index", new { processID });
      }
    }
  }
}