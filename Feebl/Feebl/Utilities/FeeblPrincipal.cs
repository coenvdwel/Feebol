using System;
using System.Collections.Generic;
using System.Security.Principal;

namespace Feebl.Utilities
{
  [Serializable]
  public class FeeblPrincipal : IPrincipal
  {
    public static FeeblPrincipal Current { get; set; }

    private readonly FeeblIdentity _identity;
    private List<string> _roles;

    public IIdentity Identity
    {
      get { return _identity; }
    }

    public List<string> Roles
    {
      get { return _roles; }
    }

    public bool IsInRole(string role)
    {
      return Roles.Contains(role);
    }

    public FeeblPrincipal()
    {
      _identity = new FeeblIdentity();
      _roles = new List<string>();
    }

    public FeeblPrincipal(FeeblIdentity identity)
    {
      _identity = identity;
      _roles = new List<string>();

      _roles.Add("User");
      if (identity.IsAdmin) _roles.Add("Admin");
    }
  }
}