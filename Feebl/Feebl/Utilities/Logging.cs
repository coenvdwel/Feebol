using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

// ReSharper disable once CheckNamespace
namespace Feebl
{
  public partial class FeeblDataContext
  {
    partial void OnCreated()
    {
#if DEBUG
      Log = new DebuggerWriter();
#endif
    }
  }

  public class DebuggerWriter : TextWriter
  {
    private bool _isOpen;
    private static UnicodeEncoding _encoding;
    private readonly int _level;
    private readonly string _category;
    private string _queued;

    private static readonly Regex SqlDeclareRegex;
    static DebuggerWriter()
    {
      SqlDeclareRegex = new Regex(@"^-- @p(\d*): (Input|Output) (.*?) \(.*?\) \[(.*?)\]", RegexOptions.Compiled);
    }

    public DebuggerWriter()
      : this(0, Debugger.DefaultCategory)
    {
      _queued = String.Empty;
    }

    public DebuggerWriter(int level, string category)
      : this(level, category, CultureInfo.CurrentCulture)
    {
    }

    public DebuggerWriter(int level, string category, IFormatProvider formatProvider)
      : base(formatProvider)
    {
      _level = level;
      _category = category;
      _isOpen = true;
    }

    protected override void Dispose(bool disposing)
    {
      _isOpen = false;
      base.Dispose(disposing);
    }

    public override void Write(char value)
    {
      if (!_isOpen)
      {
        throw new ObjectDisposedException(null);
      }
      Write(value.ToString(CultureInfo.InvariantCulture));
    }

    public override void Write(string value)
    {
      if (!_isOpen)
      {
        throw new ObjectDisposedException(null);
      }

      if (value == null)
      {
        return;
      }

      var match = SqlDeclareRegex.Match(value);
      if (match.Success)
      {
        var name = match.Groups[1].ToString();
        var type = match.Groups[3].ToString();
        var val = match.Groups[4].ToString();

        if (match.Groups[3].ToString().ToLower().EndsWith("varchar"))
        {
          type += "(100)";
          if (val.ToLower() != "null") val = "'" + val + "'";
        }

        value = String.Format("DECLARE @p{0} {1}; SET @p{0} = {2}" + Environment.NewLine, name, type, val);
      }
      else if (value.StartsWith("SELECT") || value.StartsWith("UPDATE") || value.StartsWith("INSERT") || value.StartsWith("DELETE"))
      {
        _queued = value;
        return;
      }
      else if (!String.IsNullOrEmpty(_queued))
      {
        Debugger.Log(_level, _category, _queued);
        _queued = String.Empty;
      }

      Debugger.Log(_level, _category, value);
    }

    public override void Write(char[] buffer, int index, int count)
    {
      if (!_isOpen)
      {
        throw new ObjectDisposedException(null);
      }

      if (index < 0 || count < 0 || buffer.Length - index < count)
      {
        base.Write(buffer, index, count);
      }

      Write(new string(buffer, index, count));
    }

    public override Encoding Encoding
    {
      get { return _encoding ?? (_encoding = new UnicodeEncoding(false, false)); }
    }

    public int Level
    {
      get { return _level; }
    }

    public string Category
    {
      get { return _category; }
    }
  }
}