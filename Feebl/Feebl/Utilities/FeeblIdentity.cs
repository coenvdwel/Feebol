using System;
using System.Linq;
using System.Security.Principal;

namespace Feebl.Utilities
{
  [Serializable]
  public class FeeblIdentity : IIdentity
  {
    protected string _name = "Anonymous";
    protected bool _isAuthenticated = false;

    public int UserID { get; private set; }
    public string Email { get; private set; }
    public string Mobile { get; private set; }
    public int? ApplicationID { get; private set; }
    public int? CustomerID { get; private set; }
    public bool IsAdmin { get; private set; }

    public bool IsAuthenticated { get { return _isAuthenticated; } }
    public string AuthenticationType { get { return "Default"; } }
    public string Name { get { return _name; } }

    internal FeeblIdentity()
    {
      _name = "Anonymous";
      Email = "anony@mous.com";
    }

    internal FeeblIdentity(FeeblDataContext ctx, string email, string password)
    {
      var user = (from u in ctx.Users
                  where u.Email == email
                  && u.Password == Methods.EncryptPassword(password)
                  select u).FirstOrDefault();

      if (user != null)
      {
        _name = user.Email.Split('@')[0];
        _isAuthenticated = true;

        UserID = user.UserID;
        Email = user.Email;
        Mobile = user.Mobile;
        ApplicationID = user.ApplicationID;
        CustomerID = user.CustomerID;
        IsAdmin = user.IsAdmin;
      }
    }
  }
}