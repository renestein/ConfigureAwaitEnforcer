using System;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;

namespace ConfigureAwaitEnforcer
{
  public class ConfigureAwaitEnforcerOptions
  {
    public const string CONFIG_NAME = "ConfigureAwaitEnforcerProperties";
    public const string EXTENSION_NAME = "RStein.ConfigureAwaitEnforcer";
    private const string DIAGNOSTICS_SEVERITY_KEY = "Diagnostics_Severity";
    public const char KEY_VALUE_SEPARATOR = '=';
    public static ConfigureAwaitEnforcerOptions Default = new ConfigureAwaitEnforcerOptions();

    private ConfigureAwaitEnforcerOptions()
    {
      Severity = readSeverity();
    }

    internal DiagnosticSeverity Severity
    {
      get;
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