using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using Microsoft.VisualStudio.Shell;
using Microsoft.Win32;
using static System.Environment;
using Task = System.Threading.Tasks.Task;

namespace ConfigureAwaitEnforcer.Vsix
{
  public class ConfigureAwaitEnforcerProperties : DialogPage
  {
    public const string DEFAULT_CATEGORY = "ConfigureAwaitEnforcer";
    public const string DEFAULT_SUBCATEGORY = "Basic settings";

    public const string CONFIG_NAME = nameof(ConfigureAwaitEnforcerProperties);
    public const string EXTENSION_NAME = "RStein.ConfigureAwaitEnforcer";

    private static readonly string CONFIG_FILE_DIRECTORY = Path.Combine(GetFolderPath(SpecialFolder.LocalApplicationData),
      EXTENSION_NAME);

    private static readonly string CONFIG_FILE_PATH = Path.Combine(CONFIG_FILE_DIRECTORY, CONFIG_NAME);

    private const string DIAGNOSTICS_SEVERITY_KEY = "Diagnostics_Severity";

    [Category(DEFAULT_CATEGORY)]
    [DisplayName("Diagnostics severity")]
    [Description("Diagnostics severity for a missing 'ConfigureAwait' expression.")]
    [DefaultValue(typeof(DiagnosticSeverity), "Error")]
    public DiagnosticSeverity Severity
    {
      get;
      set;
    }

    public override void LoadSettingsFromStorage()
    {
      Severity = Config.Default.Severity;
    }

    public override void SaveSettingsToStorage()
    {
      if (!Directory.Exists(CONFIG_FILE_PATH))
      {
        Directory.CreateDirectory(CONFIG_FILE_DIRECTORY);
      }
      File.WriteAllLines(CONFIG_FILE_PATH, new[]
      {
        $"{DIAGNOSTICS_SEVERITY_KEY}={Severity}"
      }, Encoding.UTF8);

      Config.Default.Reload();
    }
  }


  /// <summary>
  /// This is the class that implements the package exposed by this assembly.
  /// </summary>
  /// <remarks>
  /// <para>
  /// The minimum requirement for a class to be considered a valid package for Visual Studio
  /// is to implement the IVsPackage interface and register itself with the shell.
  /// This package uses the helper classes defined inside the Managed Package Framework (MPF)
  /// to do it: it derives from the Package class that provides the implementation of the
  /// IVsPackage interface and uses the registration attributes defined in the framework to
  /// register itself and its components with the shell. These attributes tell the pkgdef creation
  /// utility what data to put into .pkgdef file.
  /// </para>
  /// <para>
  /// To get loaded into VS, the package must be referred by &lt;Asset Type="Microsoft.VisualStudio.VsPackage" ...&gt; in .vsixmanifest file.
  /// </para>
  /// </remarks>
  [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
  [InstalledProductRegistration("#110", "#112", "1.0", IconResourceID = 400)] // Info on this package for Help/About
  [Guid(ConfigureAwaitEnforcerOptions.PackageGuidString)]
  [SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1650:ElementDocumentationMustBeSpelledCorrectly", Justification = "pkgdef, VS and vsixmanifest are valid VS terms")]
  [ProvideOptionPage(typeof(ConfigureAwaitEnforcerProperties), ConfigureAwaitEnforcerProperties.DEFAULT_CATEGORY, ConfigureAwaitEnforcerProperties.DEFAULT_SUBCATEGORY, 0, 0, true)]
  public sealed class ConfigureAwaitEnforcerOptions : AsyncPackage
  {
    /// <summary>
    /// ConfigureAwaitEnforcerOptions GUID string.
    /// </summary>


    public const string PackageGuidString = "71B98E75-A28C-4D1C-A164-57B5564324BE";

    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigureAwaitEnforcerOptions"/> class.
    /// </summary>
    public ConfigureAwaitEnforcerOptions()
    {
      // Inside this method you can place any initialization code that does not require
      // any Visual Studio service because at this point the package object is created but
      // not sited yet inside Visual Studio environment. The place to do all the other
      // initialization is the Initialize method.
    }

    #region Package Members

    /// <summary>
    /// Initialization of the package; this method is called right after the package is sited, so this is the place
    /// where you can put all the initialization code that rely on services provided by VisualStudio.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token to monitor for initialization cancellation, which can occur when VS is shutting down.</param>
    /// <param name="progress">A provider for progress updates.</param>
    /// <returns>A task representing the async work of package initialization, or an already completed task if there is none. Do not return null from this method.</returns>
    protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
    {
      // When initialized asynchronously, the current thread may be a background thread at this point.
      // Do any initialization that requires the UI thread after switching to the UI thread.
      await this.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);


    }

    #endregion
  }
}
