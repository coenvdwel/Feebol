using Feebl.Utilities;
using System;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using System.Web.Security;

namespace Feebl
{
  // Note: For instructions on enabling IIS6 or IIS7 classic mode, 
  // visit http://go.microsoft.com/?LinkId=9394801

  public class MvcApplication : HttpApplication
  {
    public static void RegisterGlobalFilters(GlobalFilterCollection filters)
    {
      filters.Add(new HandleErrorAttribute());
    }

    public static void RegisterRoutes(RouteCollection routes)
    {
      routes.IgnoreRoute("{resource}.axd/{*pathInfo}");

      routes.MapRoute(
          "Default", // Route name
          "{controller}/{action}/{id}", // URL with parameters
          new { controller = "Applications", action = "Index", id = UrlParameter.Optional } // Parameter defaults
      );

      routes.MapRoute(
          "Applications", // Route name
          "{controller}/{action}/{id}", // URL with parameters
          new { controller = "Applications", action = "Index", id = UrlParameter.Optional } // Parameter defaults
      );

      routes.MapRoute(
          "Customers", // Route name
          "{controller}/{action}/{id}", // URL with parameters
          new { controller = "Customers", action = "Index", id = UrlParameter.Optional } // Parameter defaults
      );

      routes.MapRoute(
          "Demand", // Route name
          "{controller}/{action}/{id}", // URL with parameters
          new { controller = "Demand", action = "Index", id = UrlParameter.Optional } // Parameter defaults
      );

      routes.MapRoute(
         "Process", // Route name
         "{controller}/{action}/{id}", // URL with parameters
         new { controller = "Process", action = "Index", id = UrlParameter.Optional } // Parameter defaults
     );

      routes.MapRoute(
         "Subscriptions", // Route name
         "{controller}/{action}/{id}", // URL with parameters
         new { controller = "Subscriptions", action = "Index", id = UrlParameter.Optional } // Parameter defaults
     );

      routes.MapRoute(
         "Users", // Route name
         "{controller}/{action}/{id}", // URL with parameters
         new { controller = "Users", action = "Index", id = UrlParameter.Optional } // Parameter defaults
      );

      routes.MapRoute(
        "ChangePassword", // Route name
        "{controller}/{action}/{id}", // URL with parameters
        new { controller = "ChangePassword", action = "Index", id = UrlParameter.Optional } // Parameter defaults
      );

      routes.MapRoute(
         "LogOn", // Route name
         "{controller}/{action}/{id}", // URL with parameters
         new { controller = "LogOn", action = "Index", id = UrlParameter.Optional } // Parameter defaults
     );
    }

    protected void Application_Start()
    {
      AreaRegistration.RegisterAllAreas();

      RegisterGlobalFilters(GlobalFilters.Filters);
      RegisterRoutes(RouteTable.Routes);

      Processes.Process.Init();
    }

    protected void Application_AuthenticateRequest(Object sender, EventArgs e)
    {
      // reset
      Context.User = null;
      FeeblPrincipal.Current = null;

      // get new
      FeeblPrincipal.Current = new FeeblPrincipal();
      var cookieName = FormsAuthentication.FormsCookieName;
      var authCookie = Context.Request.Cookies[cookieName];

      if (authCookie == null) return;

      using (var ctx = new FeeblDataContext())
      {
        var ticket = FormsAuthentication.Decrypt(authCookie.Value);
        if (ticket == null) return;

        var password = ticket.UserData;

        var userIdentity = new FeeblIdentity(ctx, ticket.Name, password);
        var userPrincipal = new FeeblPrincipal(userIdentity);
        Context.User = userPrincipal;
        FeeblPrincipal.Current = userPrincipal;
      }
    }
  }
}