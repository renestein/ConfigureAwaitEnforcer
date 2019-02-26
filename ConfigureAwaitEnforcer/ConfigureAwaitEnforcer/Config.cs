using System;
using System.IO;
using System.Linq;
using System.Text;
#if !VSIX
using Microsoft.CodeAnalysis;
#endif

namespace ConfigureAwaitEnforcer
{
  internal class Config
  {
    public const string CONFIG_NAME = "ConfigureAwaitEnforcerProperties";
    public const string EXTENSION_NAME = "RStein.ConfigureAwaitEnforcer";
    private const string DIAGNOSTICS_SEVERITY_KEY = "Diagnostics_Severity";
    public const char KEY_VALUE_SEPARATOR = '=';
    public static Config Default = new Config();

    private Config()
    {
      Severity = readSeverity();
    }

    public DiagnosticSeverity Severity
    {
      get;
      set;
    }

    public void Reload()
    {
      Severity= readSeverity();
    }

    private static string getConfigPath()
    {
      return Path.Combine(Environment.GetEnvironmentVariable("LOCALAPPDATA"),
                          EXTENSION_NAME,
                          CONFIG_NAME);
    }

    private DiagnosticSeverity readSeverity()
    {
      try
      {
       var configFilePath = getConfigPath();

        if (!File.Exists(configFilePath))
        {
          return DiagnosticSeverity.Error;
        }

        var lines = File.ReadAllLines(configFilePath, Encoding.UTF8);
        if (lines.Length == 0 || !lines[0].Trim().StartsWith(DIAGNOSTICS_SEVERITY_KEY))
        {
          return DiagnosticSeverity.Error;
        }

        var configValue = lines[0].Split(KEY_VALUE_SEPARATOR).Last();
        var enumValue = Enum.Parse(typeof(DiagnosticSeverity), configValue);
        if (!Enum.IsDefined(typeof(DiagnosticSeverity), enumValue))
        {
          return DiagnosticSeverity.Error;
        }

        return (DiagnosticSeverity) enumValue;
      }
      catch (Exception)
      {
        return DiagnosticSeverity.Error;
      }
    }
  }
}