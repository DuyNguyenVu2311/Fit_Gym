using System.Text.RegularExpressions;

namespace FitGym.Infrastructure;

public sealed record CustomerInstallation(string Id)
{
    public string ApplicationName => "FitGym.Customer." + Id;
    public string CookieName => "FitGym." + Id + ".Auth";

    public static CustomerInstallation Resolve(string? configuredId, bool isDevelopment)
    {
        var value = configuredId?.Trim();
        if (string.IsNullOrEmpty(value))
        {
            if (!isDevelopment) throw new InvalidOperationException("Configure FitGymDeployment:InstallationId uniquely for this customer installation before running Production.");
            value = "local-development";
        }
        if (!Regex.IsMatch(value, "^[a-z0-9][a-z0-9-]{2,49}$", RegexOptions.CultureInvariant))
            throw new InvalidOperationException("FitGymDeployment:InstallationId must contain 3–50 lowercase letters, digits or hyphens, starting with a letter or digit.");
        return new(value);
    }
}
