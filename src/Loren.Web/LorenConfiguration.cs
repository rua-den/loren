using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.CommandLine;
using Microsoft.Extensions.Configuration.EnvironmentVariables;

namespace Loren.Web;

public static class LorenConfiguration
{
    public static void AddLocalConfigurationBeforeEnvironmentOverrides(
        ConfigurationManager configuration,
        string[] args)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(args);

        for (int index = configuration.Sources.Count - 1; index >= 0; index--)
        {
            IConfigurationSource source = configuration.Sources[index];
            if (source is EnvironmentVariablesConfigurationSource environmentSource
                && string.IsNullOrEmpty(environmentSource.Prefix)
                || source is CommandLineConfigurationSource)
            {
                configuration.Sources.RemoveAt(index);
            }
        }

        configuration.AddJsonFile(
            "appsettings.Local.json",
            optional: true,
            reloadOnChange: false);
        configuration.AddEnvironmentVariables();
        configuration.AddCommandLine(args);
    }
}
