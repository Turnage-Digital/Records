using System.Reflection;

namespace Records.Architecture.Tests;

public sealed class DomainReferenceRulesTests
{
    private static readonly string[] DomainAssemblyNames =
    [
        "Records.Core.Domain",
        "Records.Tenants.Domain",
        "Records.Users.Domain",
        "Records.Recordsets.Domain",
        "Records.Notifications.Domain",
        "Records.Clocks.Domain"
    ];

    [Test]
    public void Domain_projects_only_reference_core_domain()
    {
        foreach (var assemblyName in DomainAssemblyNames)
        {
            var assembly = Assembly.Load(new AssemblyName(assemblyName));
            var recordsReferences = assembly
                .GetReferencedAssemblies()
                .Select(a => a.Name)
                .Where(name => name is not null && name.StartsWith("Records.", StringComparison.Ordinal))
                .ToArray();

            var allowed = assemblyName == "Records.Core.Domain"
                ? Array.Empty<string>()
                : ["Records.Core.Domain"];

            var forbidden = recordsReferences
                .Where(reference => !allowed.Contains(reference, StringComparer.Ordinal))
                .ToArray();

            Assert.That(
                forbidden,
                Is.Empty,
                $"{assemblyName} references other Records assemblies: {string.Join(", ", forbidden)}");
        }
    }
}