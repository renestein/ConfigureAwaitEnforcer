using System;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;

namespace ConfigureAwaitEnforcer
{

  public class ConfigureAwaitEnforcerOptions
  {
    public static ConfigureAwaitEnforcerOptions Default = new ConfigureAwaitEnforcerOptions();
    public const string CONFIG_NAME = "ConfigureAwaitEnforcerProperties";
    public const string EXTENSION_NAME = "RStein.ConfigureAwaitEnforcer";
    private const string DIAGNOSTICS_SEVERITY_KEY = "Diagnostics_Severity";
    public const char KEY_VALUE_SEPARATOR = '=';

    private static readonly string CONFIG_FILE_PATH = Path.Combine(Environment.GetEnvironmentVariable("LocalAppData"),
      EXTENSION_NAME, CONFIG_NAME);

    private ConfigureAwaitEnforcerOptions()
    {
      Severity = readSeverity();

    }

    private DiagnosticSeverity readSeverity()
    {
      try
      {
        if (!File.Exists(CONFIG_FILE_PATH))
        {
          return DiagnosticSeverity.Error;
        }

        var lines = File.ReadAllLines(CONFIG_FILE_PATH, Encoding.UTF8);
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

        return (DiagnosticSeverity)enumValue;
      }
      catch (Exception)
      {
        return DiagnosticSeverity.Error;
      }
    }

    internal DiagnosticSeverity Severity
    {
      get;
    }

  }
}