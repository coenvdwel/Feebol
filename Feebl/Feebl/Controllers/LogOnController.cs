using Feebl.Utilities;
using System;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;

namespace Feebl.Controllers
{
  public class LogOnController : Controller
  {
    public ActionResult Index()
    {
      return View();
    }

    [HttpPost]
    public ActionResult Index(string email, string password, string remember)
    {
      using (var ctx = new FeeblDataContext())
      {
        var user = (from u in ctx.Users
                    where u.Email == email
                    && u.Password == Methods.EncryptPassword(password)
                    select u).FirstOrDefault();

        if (user == null)
        {
          TempData["Error"] = "Invalid email and/or password.";
          return View();
        }

        var ticket = new FormsAuthenticationTicket(1, user.Email, DateTime.UtcNow, DateTime.UtcNow.AddYears(1), remember == "on", password);
        var cookie = new HttpCookie(FormsAuthentication.FormsCookieName, FormsAuthentication.Encrypt(ticket));

        if (remember == "on") cookie.Expires = DateTime.UtcNow.AddYears(1);

        System.Web.HttpContext.Current.Response.Cookies.Add(cookie);

        TempData["Success"] = "Welcome back!";
        return RedirectToAction("Index", "Applications");
      }
    }
    
    [Authorize]
    public ActionResult LogOff()
    {
      FormsAuthentication.SignOut();
      TempData["Success"] = "Goodbye!";
      return RedirectToAction("Index", "Applications");
    }
  }
}