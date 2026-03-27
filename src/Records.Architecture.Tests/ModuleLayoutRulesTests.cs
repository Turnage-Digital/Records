namespace Records.Architecture.Tests;

public sealed class ModuleLayoutRulesTests
{
    private static readonly HashSet<string> AllowedInfrastructureSqlFolders = new(StringComparer.Ordinal)
    {
        "Entities",
        "Mappers",
        "Migrations",
        "QueryCriteria"
    };

    private static readonly string[] FeatureModuleProjectSuffixes =
    [
        ".Application",
        ".Contracts",
        ".Domain",
        ".Infrastructure.Sql",
        ".Presentation",
        ".Tests"
    ];

    private static readonly string[] CoreProjectSuffixes =
    [
        ".Application",
        ".Contracts",
        ".Domain",
        ".Infrastructure.Sql",
        ".Tests"
    ];

    [Test]
    public void Modules_use_expected_project_layout()
    {
        var repoRoot = FindRepoRoot();
        var modulesRoot = Path.Combine(repoRoot, "src", "Modules");

        var modules = Directory
            .EnumerateDirectories(modulesRoot)
            .Select(Path.GetFileName)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .ToArray();

        foreach (var module in modules)
        {
            var expectedSuffixes = string.Equals(module, "Core", StringComparison.Ordinal)
                ? CoreProjectSuffixes
                : FeatureModuleProjectSuffixes;

            var modulePath = Path.Combine(modulesRoot, module!);
            var projectNames = Directory
                .EnumerateFiles(modulePath, "*.csproj", SearchOption.AllDirectories)
                .Select(Path.GetFileNameWithoutExtension)
                .ToHashSet(StringComparer.Ordinal);

            var expectedProjectNames = expectedSuffixes
                .Select(suffix => $"Records.{module}{suffix}")
                .ToArray();

            var missing = expectedProjectNames
                .Where(expected => !projectNames.Contains(expected))
                .ToArray();

            Assert.That(
                missing,
                Is.Empty,
                $"Module '{module}' is missing expected projects: {string.Join(", ", missing)}");
        }
    }

    [Test]
    public void Controllers_live_under_presentation_projects()
    {
        var repoRoot = FindRepoRoot();
        var modulesRoot = Path.Combine(repoRoot, "src", "Modules");

        var violations = new List<string>();
        foreach (var file in Directory.EnumerateFiles(modulesRoot, "*Controller.cs", SearchOption.AllDirectories))
        {
            var normalized = file.Replace('\\', '/');
            if (normalized.Contains(".Presentation/Controllers/", StringComparison.Ordinal))
            {
                continue;
            }

            violations.Add(Path.GetRelativePath(repoRoot, file));
        }

        Assert.That(
            violations,
            Is.Empty,
            "Controller files must be in .Presentation/Controllers.\n" + string.Join(Environment.NewLine, violations));
    }

    [Test]
    public void Contract_queries_and_projections_use_conventional_folders()
    {
        var repoRoot = FindRepoRoot();
        var modulesRoot = Path.Combine(repoRoot, "src", "Modules");

        var violations = new List<string>();
        foreach (var file in Directory.EnumerateFiles(modulesRoot, "*.cs", SearchOption.AllDirectories))
        {
            var normalized = file.Replace('\\', '/');
            if (!normalized.Contains(".Contracts/", StringComparison.Ordinal))
            {
                continue;
            }

            var fileName = Path.GetFileName(file);

            if (fileName.StartsWith("I", StringComparison.Ordinal) &&
                fileName.EndsWith("Queries.cs", StringComparison.Ordinal) &&
                !normalized.Contains(".Contracts/Queries/", StringComparison.Ordinal))
            {
                violations.Add($"{Path.GetRelativePath(repoRoot, file)} should be under Queries/");
            }

            if (fileName.StartsWith("I", StringComparison.Ordinal) &&
                fileName.Contains("ProjectionWriter", StringComparison.Ordinal) &&
                !normalized.Contains(".Contracts/Projections/", StringComparison.Ordinal))
            {
                violations.Add($"{Path.GetRelativePath(repoRoot, file)} should be under Projections/");
            }
        }

        Assert.That(
            violations,
            Is.Empty,
            "Contract query/projection interfaces are not in conventional folders.\n" +
            string.Join(Environment.NewLine, violations));
    }

    [Test]
    public void Domain_repository_and_uow_interfaces_are_root_level()
    {
        var repoRoot = FindRepoRoot();
        var modulesRoot = Path.Combine(repoRoot, "src", "Modules");

        var violations = new List<string>();
        foreach (var file in Directory.EnumerateFiles(modulesRoot, "*.cs", SearchOption.AllDirectories))
        {
            var normalized = file.Replace('\\', '/');
            if (!normalized.Contains(".Domain/", StringComparison.Ordinal))
            {
                continue;
            }

            if (normalized.Contains("/Core/", StringComparison.Ordinal))
            {
                continue;
            }

            var fileName = Path.GetFileName(file);
            var isRepositoryOrUnitOfWorkInterface = fileName.StartsWith("I", StringComparison.Ordinal) &&
                                                    (fileName.EndsWith("Repository.cs", StringComparison.Ordinal) ||
                                                     fileName.EndsWith("UnitOfWork.cs", StringComparison.Ordinal));
            if (!isRepositoryOrUnitOfWorkInterface)
            {
                continue;
            }

            const string marker = ".Domain/";
            var markerIndex = normalized.IndexOf(marker, StringComparison.Ordinal);
            if (markerIndex < 0)
            {
                continue;
            }

            var pathAfterDomain = normalized[(markerIndex + marker.Length)..];
            if (!pathAfterDomain.Contains('/', StringComparison.Ordinal))
            {
                continue;
            }

            violations.Add($"{Path.GetRelativePath(repoRoot, file)} should be at the Domain project root.");
        }

        Assert.That(
            violations,
            Is.Empty,
            "Domain repository and unit-of-work interfaces are not root-level.\n" +
            string.Join(Environment.NewLine, violations));
    }

    [Test]
    public void Infrastructure_sql_projects_use_allowed_subfolders_only()
    {
        var repoRoot = FindRepoRoot();
        var modulesRoot = Path.Combine(repoRoot, "src", "Modules");
        var infrastructureProjects = Directory.EnumerateDirectories(
            modulesRoot,
            "*.Infrastructure.Sql",
            SearchOption.AllDirectories);

        var violations = new List<string>();
        foreach (var project in infrastructureProjects)
        foreach (var directory in Directory.EnumerateDirectories(project))
        {
            var folderName = Path.GetFileName(directory);
            if (folderName is "bin" or "obj")
            {
                continue;
            }

            if (AllowedInfrastructureSqlFolders.Contains(folderName))
            {
                continue;
            }

            violations.Add(
                $"{Path.GetRelativePath(repoRoot, directory)} is not an allowed Infrastructure.Sql folder.");
        }

        Assert.That(
            violations,
            Is.Empty,
            "Infrastructure.Sql contains disallowed subfolders.\n" + string.Join(Environment.NewLine, violations));
    }

    [Test]
    public void Infrastructure_sql_files_use_namespace_matching_directory()
    {
        var repoRoot = FindRepoRoot();
        var modulesRoot = Path.Combine(repoRoot, "src", "Modules");
        var infrastructureProjects = Directory.EnumerateDirectories(
            modulesRoot,
            "*.Infrastructure.Sql",
            SearchOption.AllDirectories);

        var violations = new List<string>();
        foreach (var project in infrastructureProjects)
        {
            var projectName = Path.GetFileName(project);
            var moduleName = projectName
                .Replace("Records.", string.Empty, StringComparison.Ordinal)
                .Replace(".Infrastructure.Sql", string.Empty, StringComparison.Ordinal);
            var baseNamespace = $"Records.{moduleName}.Infrastructure.Sql";

            foreach (var file in Directory.EnumerateFiles(project, "*.cs", SearchOption.AllDirectories))
            {
                var normalized = file.Replace('\\', '/');
                if (normalized.Contains("/bin/", StringComparison.Ordinal) ||
                    normalized.Contains("/obj/", StringComparison.Ordinal))
                {
                    continue;
                }

                var namespaceLine = File.ReadLines(file)
                    .FirstOrDefault(line => line.TrimStart().StartsWith("namespace ", StringComparison.Ordinal));
                if (namespaceLine is null)
                {
                    continue;
                }

                var actualNamespace = namespaceLine
                    .Replace("namespace ", string.Empty, StringComparison.Ordinal)
                    .Replace(";", string.Empty, StringComparison.Ordinal)
                    .Replace("{", string.Empty, StringComparison.Ordinal)
                    .Trim();

                var relativePath = Path.GetRelativePath(project, file).Replace('\\', '/');
                var expectedNamespace = relativePath switch
                {
                    var path when path.StartsWith("Entities/", StringComparison.Ordinal) => baseNamespace + ".Entities",
                    var path when path.StartsWith("Mappers/", StringComparison.Ordinal) => baseNamespace + ".Mappers",
                    var path when path.StartsWith("Migrations/", StringComparison.Ordinal) => baseNamespace +
                        ".Migrations",
                    var path when path.StartsWith("QueryCriteria/", StringComparison.Ordinal) => baseNamespace +
                        ".QueryCriteria",
                    _ => baseNamespace
                };

                if (!string.Equals(actualNamespace, expectedNamespace, StringComparison.Ordinal))
                {
                    violations.Add(
                        $"{Path.GetRelativePath(repoRoot, file)} has namespace '{actualNamespace}', expected '{expectedNamespace}'.");
                }
            }
        }

        Assert.That(
            violations,
            Is.Empty,
            "Infrastructure.Sql namespace/path mismatches detected.\n" + string.Join(Environment.NewLine, violations));
    }

    [Test]
    public void Infrastructure_sql_projects_define_a_root_unit_of_work()
    {
        var repoRoot = FindRepoRoot();
        var modulesRoot = Path.Combine(repoRoot, "src", "Modules");
        var infrastructureProjects = Directory.EnumerateDirectories(
            modulesRoot,
            "*.Infrastructure.Sql",
            SearchOption.AllDirectories);

        var violations = new List<string>();
        foreach (var project in infrastructureProjects)
        {
            var hasUnitOfWorkAtRoot = Directory
                .EnumerateFiles(project, "*UnitOfWork.cs", SearchOption.TopDirectoryOnly)
                .Any();

            if (!hasUnitOfWorkAtRoot)
            {
                violations.Add($"{Path.GetRelativePath(repoRoot, project)} must define a root-level *UnitOfWork.cs.");
            }
        }

        Assert.That(
            violations,
            Is.Empty,
            "Infrastructure.Sql projects missing explicit UnitOfWork.\n" +
            string.Join(Environment.NewLine, violations));
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
}