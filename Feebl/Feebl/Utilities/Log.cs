using System;

// ReSharper disable EmptyGeneralCatchClause

namespace Feebl.Utilities
{
  public static class Log
  {
    public enum Type
    {
      Debug,
      Info,
      Warning,
      Error
    }

    public static void Info(string name, string detail)
    {
      Write(Type.Info, name, detail);
    }

    public static void Error(string name)
    {
      Write(Type.Error, name, Environment.StackTrace);
    }

    public static void Error(string name, string detail)
    {
      Write(Type.Error, name, detail);
    }

    public static void Error(Exception ex)
    {
      Write(Type.Error, ex.Message, ex.ToString());
    }

    private static void Write(Type type, string name, string detail)
    {
      try
      {
        using (var ctx = new FeeblDataContext())
        {
          ctx.Logs.InsertOnSubmit(new Feebl.Log
          {
            Message = name,
            StackTrace = detail,
            CreationTime = DateTime.Now,
            LogType = type.ToString()
          });

          ctx.SubmitChanges();
        }
      }
      catch
      {
      }

      // only email errors to me, ignore the rest
      if (type != Type.Error) return;

      try
      {
        var mail = new Email
        {
          Subject = name,
          Body = detail
        };

        mail.AddReceipient("c.wel@diract-it.nl");
        mail.Send();
      }
      catch
      {
      }
    }
  }
}