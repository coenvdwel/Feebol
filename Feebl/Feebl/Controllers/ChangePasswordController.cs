using Feebl.Utilities;
using System.Linq;
using System.Web.Mvc;

namespace Feebl.Controllers
{
  [Authorize]
  public class ChangePasswordController : Controller
  {
    [HttpGet]
    public ActionResult Index()
    {
      return View(new User());
    }

    [HttpPost]
    public ActionResult Index(string currentPassword, string newPassword, string newPassword2)
    {
      using (var ctx = new FeeblDataContext())
      {
        var user = (FeeblIdentity)FeeblPrincipal.Current.Identity;
        var u = ctx.Users.First(x => x.UserID == user.UserID);

        if (newPassword != newPassword2)
        {
          TempData["Error"] = "Passwords did not match.";
          return View(new User());
        }

        if (Methods.EncryptPassword(currentPassword) != u.Password)
        {
          TempData["Error"] = "Your current password is invalid, try again...";
          return View(new User());
        }

        if (newPassword.Length < 6)
        {
          TempData["Error"] = "New password is too short, please use a minimum of 6 characters (8 recommended).";
          return View(new User());
        }

        u.Password = Methods.EncryptPassword(newPassword);

        ctx.SubmitChanges();

        TempData["Success"] = "Successfully changed password.";
        return RedirectToAction("Index");
      }
    }
  }
}