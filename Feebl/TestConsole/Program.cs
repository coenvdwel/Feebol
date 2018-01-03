using Feebl.Client;
using Feebl.Utilities;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace FeeblConsole
{
  internal class Program
  {
    private static void Main(string[] args)
    {
      // set variables
      var regex = new Regex("SQL Server Job System: '(.*?) (.*?)' completed on (.*?)$");
      var host = Methods.GetUrl("Service.asmx");

      string msg;

      // get message
      var subject = args.FirstOrDefault() ?? string.Empty;

      // get directory
      var path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? @"c:\temp\";
      var file = Path.Combine(path, "feebl.txt");

      Log(file, "Start processing, parameter: " + subject);

      // bail if it does not match
      if (string.IsNullOrEmpty(subject) || !regex.IsMatch(subject))
      {
        Log(file, "Abort processing, parameter does not match expected line.");
        return;
      }

      // get matches
      var matches = regex.Matches(subject);

      // get variables
      var applicationID = "BI";
      var customerID = matches[0].Groups[1].Value.Replace('_', ' ');
      var processID = matches[0].Groups[2].Value.Replace('_', ' ');
      var groupID = matches[0].Groups[3].Value.Replace("\\", "");

      // post-processing variables
      // directly copied from Processes.BI methods
      if (processID.StartsWith("BI-"))
      {
        applicationID = "BI";
        processID = processID.Substring(3);
      }
      else if (processID.StartsWith("DBA-"))
      {
        applicationID = "DBA";
        processID = processID.Substring(4);
        processID = string.Format("{0} @ {1}", processID, groupID);
      }

      Log(file, "Identified variables from parameter:");
      Log(file, "- Application:" + applicationID);
      Log(file, "- Customer:" + customerID);
      Log(file, "- Process:" + processID);

      // send message to Feebl
      Service.Update(out msg, host, applicationID, customerID, processID);

      Log(file, "Processed! Feebl return message: " + msg);
    }

    // send a log message to both console and a file
    private static void Log(string file, string message)
    {
      Console.WriteLine(message);
      if (string.IsNullOrEmpty(file)) return;

      File.AppendAllText(file, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + "\t" + message + Environment.NewLine);
    }
  }
}