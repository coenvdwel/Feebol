using Feebl.Utilities;
using System.Linq;
using System.Web.Mvc;

namespace Feebl.Controllers
{
  public class SubscriptionTile
  {
    public int UserID;
    public string Application;
    public int ApplicationID;
    public string Customer;
    public int CustomerID;
    public int ProcessID;
    public string Name;
    public bool SMS;
    public bool Email;
  }

  public class SubscriptionsController : Controller
  {
    [HttpGet]
    public ActionResult Index(int id)
    {
      using (var ctx = new FeeblDataContext())
      {
        var user = (FeeblIdentity) FeeblPrincipal.Current.Identity;
        if (!user.IsAdmin) id = user.UserID;

        var pt = (from s in ctx.UserSubscriptions
          where s.UserID == id
          select new SubscriptionTile
          {
            UserID = s.UserID,
            Name = s.Process.Name,
            Email = s.Email,
            SMS = s.SMS,
            Application = s.Process.Application.Name,
            ApplicationID = s.Process.ApplicationID,
            Customer = s.Process.Customer.Name,
            CustomerID = s.Process.CustomerID,
            ProcessID = s.ProcessID
          }).ToList();

        return View(pt);
      }
    }

    [Authorize(Roles = "Admin")]
    [HttpGet]
    public ActionResult Subscribe(int id, int userID, string type)
    {
      using (var ctx = new FeeblDataContext())
      {
        var sub = (from x in ctx.UserSubscriptions
                   where x.ProcessID == id
                   && x.UserID == userID
                   select x).First();

        if (type == "email") sub.Email = !sub.Email;
        if (type == "sms") sub.SMS = !sub.SMS;

        if (!sub.Email && !sub.SMS) ctx.UserSubscriptions.DeleteOnSubmit(sub);

        ctx.SubmitChanges();

        TempData["Success"] = "User subscription was successfully changed.";

        return RedirectToAction("Index", new { id = userID });
      }
    }
  }
}