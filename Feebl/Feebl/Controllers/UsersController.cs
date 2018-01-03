using Feebl.Interfaces;
using Feebl.Utilities;
using System;
using System.Linq;
using System.Web.Mvc;

namespace Feebl.Controllers
{
  public class UserTile
  {
    public int UserID;
    public string Name;
    public string Email;
    public string Application;
    public int? ApplicationID;
    public string Customer;
    public int? CustomerID;
    public bool IsAdmin;
    public string Mobile;
  }

  [Authorize(Roles = "Admin")]
  public class UsersController : Controller
  {
    [HttpGet]
    public ActionResult Index()
    {
      using (var ctx = new FeeblDataContext())
      {
        var user = (FeeblIdentity)FeeblPrincipal.Current.Identity;

        var pt = (from u in ctx.Users
                  where (!user.ApplicationID.HasValue || u.ApplicationID == user.ApplicationID)
                  && (!user.CustomerID.HasValue || u.CustomerID == user.CustomerID)
                  select new UserTile
                  {
                    UserID = u.UserID,
                    Name = u.Email.Substring(0, u.Email.IndexOf('@') < 0 ? u.Email.Length : u.Email.IndexOf('@')),
                    Email = u.Email,
                    Application = u.Application.Name,
                    ApplicationID = u.ApplicationID,
                    Customer = u.Customer.Name,
                    CustomerID = u.CustomerID,
                    IsAdmin = u.IsAdmin,
                    Mobile = u.Mobile
                  }).ToList();

        return View(pt);
      }
    }

    [HttpGet]
    public ViewResult Create()
    {
      return View(new User());
    }

    [HttpPost]
    public ActionResult Create(User u, string applicationName, string customerName, string admin)
    {
      using (var ctx = new FeeblDataContext())
      {
        if (!TryUpdateModel(u, new[] { "Email", "Mobile" }))
        {
          TempData["Error"] = "Failure creating User.";
          return View(u);
        }

        #region Update Application, Customer and IsAdmin

        if (!string.IsNullOrEmpty(applicationName))
        {
          u.ApplicationID = (from a in ctx.Applications
                             where a.Name == applicationName
                             select (int?)a.ApplicationID).FirstOrDefault();

          if (!u.ApplicationID.HasValue)
          {
            TempData["Error"] = "Failure creating User, application '" + applicationName + "' does not exist.";
            return View(u);
          }
        }

        if (!string.IsNullOrEmpty(customerName))
        {
          u.CustomerID = (from a in ctx.Customers
                          where a.Name == customerName
                          select (int?)a.CustomerID).FirstOrDefault();

          if (!u.CustomerID.HasValue)
          {
            TempData["Error"] = "Failure creating User, customer '" + customerName + "' does not exist.";
            return View(u);
          }
        }

        u.IsAdmin = (admin.ToLower() == "true");

        var user = (FeeblIdentity)FeeblPrincipal.Current.Identity;
        if (user.ApplicationID.HasValue) u.ApplicationID = user.ApplicationID.Value;
        if (user.CustomerID.HasValue) u.CustomerID = user.CustomerID.Value;

        #endregion

        #region Password

        var password = Methods.GeneratePassword();

        var mail = new Email
        {
          Subject = "User registration",
          Body = "Hi,<br /><br /><br />" + $"An account has been created for you! You can log in using the link below, using your email address and the following temporary password: {password}."
        };

        mail.AddReceipient(u.Email);
        mail.Send();

        u.Password = Methods.EncryptPassword(password);

        #endregion

        ctx.Users.InsertOnSubmit(u);
        ctx.SubmitChanges();

        TempData["Success"] = "Successfully created User.";
        return RedirectToAction("Index");
      }
    }

    [HttpGet]
    public ViewResult Edit(int id)
    {
      using (var ctx = new FeeblDataContext())
      {
        var user = (from u in ctx.Users
                    where u.UserID == id
                    select new UserTile
                    {
                      UserID = u.UserID,
                      Name = u.Email.Substring(0, u.Email.IndexOf('@') < 0 ? u.Email.Length : u.Email.IndexOf('@')),
                      Email = u.Email,
                      Application = u.Application.Name,
                      ApplicationID = u.ApplicationID,
                      Customer = u.Customer.Name,
                      CustomerID = u.CustomerID,
                      IsAdmin = u.IsAdmin,
                      Mobile = u.Mobile
                    }).First();

        return View(user);
      }
    }

    [HttpPost]
    public ActionResult Edit(int id, string applicationName, string customerName, string admin, string resetPassword)
    {
      using (var ctx = new FeeblDataContext())
      {
        var u = ctx.Users.First(x => x.UserID == id);

        var user = (FeeblIdentity)FeeblPrincipal.Current.Identity;
        if ((user.ApplicationID.HasValue && u.ApplicationID != user.ApplicationID.Value) || (user.CustomerID.HasValue && u.CustomerID != user.CustomerID.Value))
        {
          TempData["Error"] = "You have no right to edit this user.";
          return View(u);
        }

        if (!TryUpdateModel(u, new[] { "Email", "Mobile" }))
        {
          TempData["Error"] = "Failure updating User.";
          return View(u);
        }

        #region Update Application, Customer and IsAdmin

        if (!string.IsNullOrEmpty(applicationName))
        {
          u.ApplicationID = (from a in ctx.Applications
                             where a.Name == applicationName
                             select (int?)a.ApplicationID).FirstOrDefault();

          if (!u.ApplicationID.HasValue)
          {
            TempData["Error"] = "Failure creating User, application '" + applicationName + "' does not exist.";
            return View(u);
          }
        } else if(u.ApplicationID.HasValue) u.ApplicationID = null;

        if (!string.IsNullOrEmpty(customerName))
        {
          u.CustomerID = (from a in ctx.Customers
                          where a.Name == customerName
                          select (int?)a.CustomerID).FirstOrDefault();

          if (!u.CustomerID.HasValue)
          {
            TempData["Error"] = "Failure creating User, customer '" + customerName + "' does not exist.";
            return View(u);
          }
        } else if(u.CustomerID.HasValue) u.CustomerID = null;

        if (user.ApplicationID.HasValue) u.ApplicationID = user.ApplicationID.Value;
        if (user.CustomerID.HasValue) u.CustomerID = user.CustomerID.Value;

        u.IsAdmin = (admin.ToLower() == "true");

        #endregion

        #region Password

        if (resetPassword == "on")
        {
          var password = Methods.GeneratePassword();
          var mail = new Email
          {
            Subject = "User registration",
            Body = $"Hi,<br /><br /><br />Your password has been reset by {user.Name}! You can log in using the link below, using your email address and the following temporary password: {password}."
          };

          mail.AddReceipient(u.Email);
          mail.Send();

          u.Password = Methods.EncryptPassword(password);
        }

        #endregion

        ctx.SubmitChanges();

        TempData["Success"] = "Successfully updated User.";
        return RedirectToAction("Index");
      }
    }

    public ActionResult Delete(int id)
    {
      using (var ctx = new FeeblDataContext())
      {
        var user = (from u in ctx.Users
                    where u.UserID == id
                    select u).First();

        return View(user);
      }
    }

    [HttpPost]
    public ActionResult Delete(int id, FormCollection collection)
    {
      using (var ctx = new FeeblDataContext())
      {
        var u = ctx.Users.First(x => x.UserID == id);

        var user = (FeeblIdentity)FeeblPrincipal.Current.Identity;
        if ((user.ApplicationID.HasValue && u.ApplicationID != user.ApplicationID.Value) || (user.CustomerID.HasValue && u.CustomerID != user.CustomerID.Value))
        {
          TempData["Error"] = "You have no right to delete this user.";
          return View(u);
        }

        ctx.UserSubscriptions.DeleteAllOnSubmit(u.UserSubscriptions);
        ctx.Users.DeleteOnSubmit(u);
        ctx.SubmitChanges();

        TempData["Success"] = "Successfully deleted User.";
        return RedirectToAction("Index");
      }
    }

    [HttpGet]
    public ViewResult Broadcast()
    {
      return View();
    }

    [HttpPost]
    public ActionResult Broadcast(string message, string SMS, string applicationName, string customerName)
    {
      using (var ctx = new FeeblDataContext())
      {
        var user = (FeeblIdentity)FeeblPrincipal.Current.Identity;

        var applicationID = user.ApplicationID ?? (from a in ctx.Applications where a.Name.ToLower() == applicationName.ToLower() select (int?)a.ApplicationID).FirstOrDefault();
        var customerID = user.CustomerID ?? (from c in ctx.Customers where c.Name.ToLower() == customerName.ToLower() select (int?)c.CustomerID).FirstOrDefault();
        var sendSMS = (SMS.ToLower() == "yes");

        var users = (from u in ctx.Users
                     where (!u.ApplicationID.HasValue || !applicationID.HasValue || applicationID == u.ApplicationID)
                     && (!u.CustomerID.HasValue || !customerID.HasValue || customerID == u.CustomerID)
                     && (!sendSMS || (u.Mobile != null && u.Mobile != string.Empty))
                     && (sendSMS || (u.Email.Contains("@") && !u.Email.ToLower().Contains("bi_support")))
                     select u).ToList();

        IMessage msg;

        if (sendSMS)
        {
          msg = new SmsMessage
          {
            Body = message
          };
        }
        else
        {
          msg = new Email
          {
            Subject = "Feebl broadcast message",
            Body = message
          };
        }

        foreach (var u in users) msg.AddReceipient(sendSMS ? u.Mobile : u.Email);
        msg.Send();

        TempData["Success"] = "Successfully broadcasted message!";
        return RedirectToAction("Index");
      }
    }
  }
}