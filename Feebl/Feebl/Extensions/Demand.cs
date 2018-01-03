using Feebl.Utilities;
using Quartz;
using System;
using System.Linq;

// ReSharper disable PossibleInvalidOperationException
// ReSharper disable CheckNamespace
// ReSharper disable ReplaceWithStringIsNullOrEmpty

namespace Feebl
{
	public partial class Demand
	{
	  public void CalculateNextRunTime()
	  {
	    CalculateNextRunTime(DateTime.UtcNow);
	  }

    public void CalculateNextRunTime(DateTime? offset)
    {
      CalculateNextRunTime(offset ?? DateTime.UtcNow);
    }

	  public void CalculateNextRunTime(DateTime offset)
	  {
	    if (string.IsNullOrEmpty(CronExpression) || !Quartz.CronExpression.IsValidExpression(CronExpression))
	    {
	      NextRunTime = null;
	    }
	    else
	    {
	      var date = new CronExpression(CronExpression).GetNextValidTimeAfter(offset.AddHours(UtcOffset ?? DateTimeExtensions.DefaultUtcOffset));
	      NextRunTime = date == null ? (DateTime?) null : date.Value.DateTime.AddHours(-(UtcOffset ?? DateTimeExtensions.DefaultUtcOffset));
	    }
	  }

	  public void Update(FeeblDataContext ctx, bool isMet, string remark, int? counter)
		{
      // if the demand was still not met (it failed previously and is still failed) and it's priority is Normal or higher
      // we will initiate a ticket log for support
		  if (!IsMet && !isMet && Priority >= (int)Lists.Priority.Normal)
		  {
        // find the last log of state change for this demand
        var log = (from h in Histories orderby h.HistoryID descending select h).FirstOrDefault();

        // create a buffer of MaxRunTime ?? 30 minutes and check the IsExported flag to prevent doubles
		    if (log != null && log.Status == "Failed" && !log.IsExported && log.CreationTime <= DateTime.UtcNow.AddMinutes(-(MaxRunTime ?? 30)))
		    {
		      var subject = string.Format("{0} failed for {1} @ {2}", Process.Name, Process.Application.Name, Process.Customer.Name);

		      if (Priority >= (int) Lists.Priority.High) subject = "[PRIO: HIGH] " + subject;

          var message = counter.HasValue
		        ? string.Format("{0} / {1}", Methods.GetCounterValue(counter.Value), Methods.GetCounterValue(QuantityValue))
		        : string.Format("{0}, last run {1}", NextRunTime.Value.ToCET().ToString("MM/dd HH:mm"), Process.LastRunTime.HasValue ? Process.LastRunTime.Value.ToCET().ToString("MM/dd HH:mm") : "never");

		      var subscribers = Process.UserSubscriptions.Select(s => s.User.Email).Distinct().ToArray();

          // include related users, in case of backup procedures
		      var related = (from p in ctx.Processes
		                     from s in p.UserSubscriptions
		                     where p.CustomerID == Process.CustomerID
		                           && p.ApplicationID == Process.ApplicationID
		                     select s.User.Email).Distinct().ToArray();

		      var mail = new Email
		      {
		        Subject = subject,
		        Body = $"{subject}<br /><br />Failed at {message}<br /><br />{Comment}<br /><br />Subscribers: {string.Join(",", subscribers)}<br />Related: {string.Join(",", related)}<br /><br />{Methods.GetUrl($"Demand?processID={ProcessID}")}"
		      };

		      mail.AddReceipient("support@diract-it.nl");
		      mail.Send();

		      log.IsExported = true;
		    }
		  }

      // if there was a change in condition (we either failed a green process or just resolved a red process)
      // make sure the proper users are notified of this change
      else if (IsMet != isMet)
      {
        var diff = !isMet ? "Failed" : "Resolved";
        
        ctx.Histories.InsertOnSubmit(new History
        {
          Counter = counter,
          CreationTime = DateTime.UtcNow,
          Demand = this,
          DemandID = DemandID,
          Status = diff,
          Remark = remark
        });
        
        var emails = (from s in Process.UserSubscriptions where s.User.Email != null && s.User.Email != string.Empty && s.Email select s.User.Email).Distinct().ToList();
        var mobiles = (from s in Process.UserSubscriptions where s.User.Mobile != null && s.User.Mobile != string.Empty && s.SMS select s.User.Mobile).Distinct().ToList();

        // if this is triggered from a user itself (through Ignore)
        // remove this user from SMS notification if applicable
        if (FeeblPrincipal.Current != null)
        {
          var user = (FeeblIdentity) FeeblPrincipal.Current.Identity;
          if (!string.IsNullOrEmpty(user.Mobile)) mobiles.Remove(user.Mobile);
        }

        var subject = string.Format("{0} {1} for {2} @ {3}", Process.Name, diff.ToLower(), Process.Application.Name, Process.Customer.Name);

        var detail = counter.HasValue
          ? string.Format("{0} / {1}", Methods.GetCounterValue(counter.Value), Methods.GetCounterValue(QuantityValue))
          : isMet
            ? string.Format("at {1} - next @{0}", NextRunTime.Value.ToCET().ToString("MM/dd HH:mm"), Process.LastRunTime.HasValue ? Process.LastRunTime.Value.ToCET().ToString("MM/dd HH:mm") : "never")
            : string.Format("{0}, last run {1}", NextRunTime.Value.ToCET().ToString("MM/dd HH:mm"), Process.LastRunTime.HasValue ? Process.LastRunTime.Value.ToCET().ToString("MM/dd HH:mm") : "never");

        if (!string.IsNullOrEmpty(remark)) detail += ", " + remark;

        if (emails.Any())
        {
          var mail = new Email
          {
            Subject = subject,
            Body = detail + "<br /><br />" + Comment + "<br /><br />" + Methods.GetUrl($"Demand?processID={ProcessID}")
          };

          foreach (var to in emails) mail.AddReceipient(to);
          mail.Send();
        }

        if (mobiles.Any() && Priority >= (int) Lists.Priority.Low)
        {
          var sms = new SmsMessage {Body = subject + ", " + detail};
          if (sms.Body.Length > 160) sms.Body = subject;
          if (sms.Body.Length > 160) sms.Body = string.Format("{0} {1}!", Process.Name, diff.ToLower());

          foreach (var to in mobiles) sms.AddReceipient(to);
          sms.Send();
        }

        var slack = new Slack
        {
          Body = string.Format("<{0}|{1}>, {2}", Methods.GetUrl($"Demand?processID={ProcessID}"), subject, detail),
          Success = isMet
        };

        slack.AddReceipient("feebl");
        slack.AddReceipient(Process.Application.Name.ToLower());
        slack.AddReceipient(Process.Customer.Name.ToLower());
        slack.Send();

        IsMet = isMet;
      }

      ErrorMessage = (!isMet && counter.HasValue)
        ? string.Format("{0} / {1}", Methods.GetCounterValue(counter.Value), Methods.GetCounterValue(QuantityValue))
        : string.Empty;
		}

    public void Check(FeeblDataContext ctx)
    {
      // if we've not yet determined next run, calculate and bail
      if (!NextRunTime.HasValue)
      {
        CalculateNextRunTime();
        return;
      }

      // when do we expect to have a message from this job
      var threshold = NextRunTime.Value.AddMinutes(MaxRunTime ?? 0);

      // if it's in the future, we're all good - just bail
      if (threshold > DateTime.UtcNow)
      {
        return;
      }

      // if there's no event, make a special case to signal and bail
      if (!Process.LastRunTime.HasValue)
      {
        Update(ctx, true, "No events", null);
        return;
      }

      // if the last event was from a previous run, signal failure and bail
      // prevent this from executing if we left MaxRunTime empty (= pure counter check)
      //// also reducing scheduled time by 1 minute to prevent time sync issues
      if (MaxRunTime.HasValue && Process.LastRunTime.Value < NextRunTime.Value.AddMinutes(-1))
      {
        Update(ctx, false, null, null);
        return;
      }

      int? quantity = null;
      if (QuantityValue.HasValue)
      {
        // if quantity time was given, use this as interval
        // otherwise, use start time of this demand (minute 1 minute time sync issue)
        var fromTime = QuantityTime.HasValue
          ? DateTime.UtcNow.AddMinutes(-QuantityTime.Value)
          : NextRunTime.Value;

        quantity = (from e in Process.Events
                    where e.CreationTime >= fromTime.AddMinutes(-1)
                    select e.Counter ?? 0).Sum();

        // when the quantity demands are not met, signal failure and bail
        if (quantity < QuantityValue.Value)
        {
          Update(ctx, false, null, quantity);
          return;
        }
      }

      // we've passed all the checks, and the current "NextRunTime" conditions were met
      // so move on to the next and signal success
      CalculateNextRunTime();
      Update(ctx, true, null, quantity);
    }
	}
}