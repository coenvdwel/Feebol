using System.Collections.Generic;

namespace Feebl.Interfaces
{
  interface IMessage
  {
    void Send();
    void AddReceipient(string receipient);
    List<string> GetReceipients();
  }
}
