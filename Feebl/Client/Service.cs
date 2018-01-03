using System;
using System.Configuration;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Web;

namespace Feebl.Client
{
  public static class Service
  {
    /// <summary>
    /// A static URL to the webservice .asmx.
    /// </summary>
    public static string Host = "https://feebl.diract-it.nl/Service.asmx";

    /// <summary>
    /// Sends a notification message to Feebl for the specified process.
    /// 
    /// This call will redirect to Update(host, applicationID, customerID, processID, (counter, eventLog)) and will take the first three
    /// parameters from the Web.Config. That means this call requires the following appsettings to be set:
    /// 
    /// * FeeblHost - URL to the webservice .asmx
    /// * FeeblApplicationID - Name of the application (WMS, BI, Concentrator, WPOS)
    /// * FeeblCustomerID - Name of the customer (BAS, Five4U, CoolCat) (mind existing conventions to allow grouping!)
    /// * FeeblEnabled - To allow quick disabling of the monitoring messaging, defaults to True
    /// </summary>
    /// <param name="msg">Will contain the error message in case the return value is FALSE</param>
    /// <param name="processID">Unique name of the process (eg. Order Import, Stock Check, ...)</param>
    /// <param name="counter">Optional counter to indicate progress or affected rows from this specific run</param>
    /// <param name="eventLog">Whether or not this event message should also be logged in the local Windows Event Log</param>
    /// <returns>If the update was successfull or not</returns>
    public static bool Update(out string msg, string processID, int counter = 0, bool eventLog = false)
    {
      string host, applicationID, customerID;

      try
      {
        var enabled = ConfigurationManager.AppSettings["FeeblEnabled"];
        if (!string.IsNullOrEmpty(enabled) && enabled.ToLower() == "false")
        {
          msg = "Feebl disabled, ignoring call.";
          return true;
        }

        host = ConfigurationManager.AppSettings["FeeblHost"];
        applicationID = ConfigurationManager.AppSettings["FeeblApplicationID"];
        customerID = ConfigurationManager.AppSettings["FeeblCustomerID"];

        if (string.IsNullOrEmpty(host)) throw new ConfigurationErrorsException("FeeblHost was not set in the Web.Config appsettings, aborting update.");
        if (string.IsNullOrEmpty(applicationID)) throw new ConfigurationErrorsException("FeeblApplicationID was not set in the Web.Config appsettings, aborting update.");
        if (string.IsNullOrEmpty(customerID)) throw new ConfigurationErrorsException("FeeblCustomerID was not set in the Web.Config appsettings, aborting update.");

        if (!host.StartsWith("http://")) host = "http://" + host;
        if (host.EndsWith("/") || host.EndsWith("\\")) host = host.Substring(0, host.Length - 1);
        if (!host.EndsWith(".asmx")) host = host + ".asmx";
      }
      catch (Exception ex)
      {
        msg = "Preprocess issue: " + ex;
        return false;
      }

      return Update(out msg, host, applicationID, customerID, processID, counter);
    }

    /// <summary>
    /// Sends a notification message to Feebl for the specified process.
    /// 
    /// Will use the static Host variable as host URL.
    /// </summary>
    /// <param name="msg">Will contain the error message in case the return value is FALSE</param>
    /// <param name="applicationID">Name of the application (WMS, BI, Concentrator, WPOS)</param>
    /// <param name="customerID">Name of the customer (BAS, Five4U, CoolCat) (mind existing conventions to allow grouping!)</param>
    /// <param name="processID">Unique name of the process (eg. Order Import, Stock Check, ...)</param>
    /// <param name="counter">Optional counter to indicate progress or affected rows from this specific run</param>
    /// <param name="eventLog">Whether or not this event message should also be logged in the local Windows Event Log</param>
    /// <returns>If the update was successfull or not</returns>
    public static bool Update(out string msg, string applicationID, string customerID, string processID, int counter = 0, bool eventLog = false)
    {
      return Update(out msg, Host, applicationID, customerID, processID, counter, eventLog);
    }

    /// <summary>
    /// Sends a notification message to Feebl for the specified process.
    /// </summary>
    /// <param name="msg">Will contain the error message in case the return value is FALSE</param>
    /// <param name="host">URL to the webservice .asmx</param>
    /// <param name="applicationID">Name of the application (WMS, BI, Concentrator, WPOS)</param>
    /// <param name="customerID">Name of the customer (BAS, Five4U, CoolCat) (mind existing conventions to allow grouping!)</param>
    /// <param name="processID">Unique name of the process (eg. Order Import, Stock Check, ...)</param>
    /// <param name="counter">Optional counter to indicate progress or affected rows from this specific run</param>
    /// <param name="eventLog">Whether or not this event message should also be logged in the local Windows Event Log</param>
    /// <returns>If the update was successfull or not</returns>
    public static bool Update(out string msg, string host, string applicationID, string customerID, string processID, int counter = 0, bool eventLog = false)
    {
      return Update(out msg, host, applicationID, customerID, processID, null, counter, eventLog);
    }

    /// <summary>
    /// Sends a notification message to Feebl for the specified process.
    /// </summary>
    /// <param name="msg">Will contain the error message in case the return value is FALSE</param>
    /// <param name="host">URL to the webservice .asmx</param>
    /// <param name="applicationID">Name of the application (WMS, BI, Concentrator, WPOS)</param>
    /// <param name="customerID">Name of the customer (BAS, Five4U, CoolCat) (mind existing conventions to allow grouping!)</param>
    /// <param name="processID">Unique name of the process (eg. Order Import, Stock Check, ...)</param>
    /// <param name="utcProcessingTime">UTC DateTime when the process completed, will be DateTime.UtcNow when not specified</param>
    /// <param name="counter">Optional counter to indicate progress or affected rows from this specific run</param>
    /// <param name="eventLog">Whether or not this event message should also be logged in the local Windows Event Log</param>
    /// <returns>If the update was successfull or not</returns>
    public static bool Update(out string msg, string host, string applicationID, string customerID, string processID, DateTime? utcProcessingTime, int counter = 0, bool eventLog = false)
    {
      try
      {
        var hash = new StringBuilder();
        foreach (var b in SHA1.Create().ComputeHash(Encoding.UTF8.GetBytes(applicationID + customerID + processID + counter))) hash.Append(b.ToString("X2"));

        var request = WebRequest.Create(string.Format("{0}/Update", host));

        var parameters = string.Format("applicationID={0}&customerID={1}&processID={2}&counter={3}{4}&hash={5}",
          HttpUtility.UrlEncode(applicationID),
          HttpUtility.UrlEncode(customerID),
          HttpUtility.UrlEncode(processID),
          counter,
          utcProcessingTime.HasValue ? "&utcProcessingTime=" + utcProcessingTime : "",
          hash[17] + hash[Math.Abs(counter)%hash.Length] + hash.ToString().Substring(1, 7));

        var bytes = Encoding.UTF8.GetBytes(parameters);

        request.ContentType = "application/x-www-form-urlencoded";
        request.ContentLength = bytes.Length;
        request.Method = "POST";

        using (var s = request.GetRequestStream())
        {
          s.Write(bytes, 0, bytes.Length);
        }

        request.GetResponse();

        if (eventLog)
        {
          if (!System.Diagnostics.EventLog.SourceExists(processID))
            System.Diagnostics.EventLog.CreateEventSource(processID, applicationID);

          System.Diagnostics.EventLog.WriteEntry(processID, parameters);
        }

        msg = string.Empty;
        return true;
      }
      catch (Exception ex)
      {
        msg = string.Format("{0} ApplicationID {1} | CustomerID {2} | ProcessID {3} | Counter {4}", ex + Environment.NewLine + Environment.NewLine, applicationID, customerID, processID, counter);
        return false;
      }
    }
  }
}