using System.Text.RegularExpressions;

namespace Records.Architecture.Tests;

public sealed partial class AgentsModuleArchitectureTests
{
    private static readonly Regex ProjectReferenceRegex = GetRegex();

    [Test]
    public void Agents_projects_follow_target_module_dependency_graph()
    {
        var repoRoot = FindRepoRoot();
        var agentsRoot = Path.Combine(repoRoot, "src", "Modules", "Agents");

        AssertProjectReferences(
            Path.Combine(agentsRoot, "Records.Agents.Domain", "Records.Agents.Domain.csproj"),
            mustInclude: ["Records.Core.Domain"],
            mustExclude: ["Records.Agents.Application", "Records.Agents.Contracts", "Records.Agents.Infrastructure.OpenAI", "Records.Agents.Infrastructure.Sql", "Records.Agents.Presentation"]);

        AssertProjectReferences(
            Path.Combine(agentsRoot, "Records.Agents.Contracts", "Records.Agents.Contracts.csproj"),
            mustInclude: ["Records.Agents.Domain"],
            mustExclude: ["Records.Agents.Application", "Records.Agents.Infrastructure.OpenAI", "Records.Agents.Infrastructure.Sql", "Records.Agents.Presentation"]);

        AssertProjectReferences(
            Path.Combine(agentsRoot, "Records.Agents.Application", "Records.Agents.Application.csproj"),
            mustInclude: ["Records.Agents.Domain", "Records.Agents.Contracts"],
            mustExclude: ["Records.Agents.Infrastructure.OpenAI", "Records.Agents.Infrastructure.Sql", "Records.Agents.Presentation"]);

        AssertProjectReferences(
            Path.Combine(agentsRoot, "Records.Agents.Infrastructure.Sql", "Records.Agents.Infrastructure.Sql.csproj"),
            mustInclude: ["Records.Agents.Domain", "Records.Agents.Contracts"],
            mustExclude: ["Records.Agents.Application", "Records.Agents.Infrastructure.OpenAI", "Records.Agents.Presentation"]);

        AssertProjectReferences(
            Path.Combine(agentsRoot, "Records.Agents.Infrastructure.OpenAI", "Records.Agents.Infrastructure.OpenAI.csproj"),
            mustInclude: ["Records.Agents.Domain", "Records.Agents.Contracts", "Records.Agents.Infrastructure.Sql"],
            mustExclude: ["Records.Agents.Application", "Records.Agents.Presentation"]);

        AssertProjectReferences(
            Path.Combine(agentsRoot, "Records.Agents.Presentation", "Records.Agents.Presentation.csproj"),
            mustInclude: ["Records.Agents.Application", "Records.Agents.Contracts"],
            mustExclude: ["Records.Agents.Domain", "Records.Agents.Infrastructure.OpenAI", "Records.Agents.Infrastructure.Sql"]);
    }

    private static void AssertProjectReferences(
        string projectPath,
        IReadOnlyCollection<string> mustInclude,
        IReadOnlyCollection<string> mustExclude
    )
    {
        var references = GetReferencedProjectNames(projectPath);

        foreach (var expected in mustInclude)
        {
            Assert.That(
                references,
                Contains.Item(expected),
                $"{Path.GetFileName(projectPath)} should reference {expected}.");
        }

        foreach (var forbidden in mustExclude)
        {
            Assert.That(
                references,
                Does.Not.Contain(forbidden),
                $"{Path.GetFileName(projectPath)} should not reference {forbidden}.");
        }
    }

    private static string[] GetReferencedProjectNames(string projectPath)
    {
        var contents = File.ReadAllText(projectPath);

        return ProjectReferenceRegex
            .Matches(contents)
            .Select(match => match.Groups[1].Value)
            .Select(path => path.Replace('\\', Path.DirectorySeparatorChar))
            .Select(Path.GetFileNameWithoutExtension)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .ToArray()!;
    }

    private static string FindRepoRoot()
    {
        var current = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "Records.sln")))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        throw new InvalidOperationException("Could not locate Records.sln from the test working directory.");
    }

    [GeneratedRegex("ProjectReference Include=\"([^\"]+)\"", RegexOptions.Compiled | RegexOptions.CultureInvariant)]
    private static partial Regex GetRegex();
}
