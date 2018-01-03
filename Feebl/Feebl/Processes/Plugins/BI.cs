using Feebl.Processes.ExchangeWebServices;
using Feebl.Utilities;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text.RegularExpressions;

namespace Feebl.Processes.Plugins
{
  public class BI
  {
    public static void ProcessMails()
    {
      var regex = new Regex("SQL Server Job System: '(.*?) (.*?)' completed on (.*?)$");

      var binding = new ExchangeServiceBinding
      {
        Url = "https://amxprd0510.outlook.com/ews/exchange.asmx",
        Credentials = new NetworkCredential("bi_support_jobs@diract-it.nl", ""),
        RequestServerVersionValue = new RequestServerVersion {Version = ExchangeVersionType.Exchange2010}
      };

      var request = new FindItemType
      {
        Traversal = ItemQueryTraversalType.Shallow,
        ItemShape = new ItemResponseShapeType {BaseShape = DefaultShapeNamesType.Default},
        ParentFolderIds = new BaseFolderIdType[]
        {
          new DistinguishedFolderIdType {Id = DistinguishedFolderIdNameType.inbox}
        }
      };

      var response = binding.FindItem(request);
      var toDelete = new List<ItemIdType>();

      foreach (FindItemResponseMessageType message in response.ResponseMessages.Items)
      {
        if (message.RootFolder == null || message.RootFolder.TotalItemsInView <= 0) continue;
        foreach (var item in ((ArrayOfRealItemsType) message.RootFolder.Item).Items)
        {
          if (!regex.IsMatch(item.Subject)) continue;
          var matches = regex.Matches(item.Subject);

          toDelete.Add(item.ItemId);

          var applicationID = "BI";
          var customerID = matches[0].Groups[1].Value.Replace('_', ' ');
          var processID = matches[0].Groups[2].Value.Replace('_', ' ');
          var groupID = matches[0].Groups[3].Value.Replace("\\", "");

          // for BI, the subject of an email message should be as follows:
          // SQL Server Job System: '<Customer> <Process>' completed on <Groupname>
          // or, SQL Server Job System: '<Customer> BI-<Process>' completed on <Groupname>

          // however, Groupname is always ignored for BI jobs

          if (processID.StartsWith("BI-"))
          {
            applicationID = "BI";
            processID = processID.Substring(3);
          }

          // for DBA, the subject of an email message should be as follows:
          // SQL Server Job System: '<Customer> DBA-<Process>' completed on <Groupname>

          // the Groupname will always be appended to the process name, so that process will be <Process @ Groupname>

          if (processID.StartsWith("DBA-"))
          {
            applicationID = "DBA";
            processID = processID.Substring(4);
            processID = string.Format("{0} @ {1}", processID, groupID);
          }

          string msg;
          if (!Client.Service.Update(out msg, Methods.GetUrl("Service.asmx"), applicationID, customerID, processID)) throw new Exception(msg);
        }
      }

      if (toDelete.Count == 0) return;

      binding.DeleteItem(new DeleteItemType
      {
        DeleteType = DisposalType.MoveToDeletedItems,
        ItemIds = toDelete.ToArray()
      });
    }
  }
}