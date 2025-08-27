using System.Globalization;
using Tomlyn;
using Tomlyn.Model;

using Path = System.IO.Path;

namespace Celbridge.Projects.Services;

public class ProjectConfigService : IProjectConfigService
{
    private readonly Dictionary<string, string> _properties = new();
    private ProjectConfig _config = new();

    public Result Initialize(string tomlContent)
    {
        try
        {
            var parse = Toml.Parse(tomlContent);
            if (parse.HasErrors)
            {
                var errors = string.Join("; ", parse.Diagnostics.Select(d => d.ToString()));
                return Result.Fail($"TOML parse error(s): {errors}");
            }

            var root = (TomlTable)parse.ToModel();
            _config = MapRootToModel(root);

            _properties.Clear();
            foreach (var kv in _config.Project.Properties)
            {
                _properties[kv.Key] = kv.Value ?? string.Empty;
            }

            return Result.Ok();
        }
        catch (Exception ex)
        {
            return Result.Fail("Failed to load TOML.")
                .WithException(ex);
        }
    }

    public Result InitializeFromFile(string filePath)
    {
        try
        {
            var text = File.ReadAllText(filePath);
            return Initialize(text);
        }
        catch (Exception ex)
        {
            return Result.Fail($"Failed to read TOML file: {filePath}")
                .WithException(ex);
        }
    }

    public string ToToml()
    {
        SyncBagIntoModelProperties();
        var root = BuildTomlFromModel(_config);
        return Toml.FromModel(root);
    }

    public Result SaveToFile(string filePath)
    {
        try
        {
            var toml = ToToml();

            var fullPath = Path.GetFullPath(filePath);
            var folder = Path.GetDirectoryName(fullPath)!;

            Directory.CreateDirectory(folder);
            File.WriteAllText(filePath, toml);

            return Result.Ok();
        }
        catch (Exception ex)
        {
            return Result.Fail($"Failed to write TOML file: {filePath}")
                .WithException(ex);
        }
    }

    public ProjectConfig Config => _config;

    public string GetProperty(string propertyName, string defaultValue)
        => _properties.TryGetValue(propertyName, out var v) ? v ?? string.Empty : defaultValue;

    public string GetProperty(string propertyName) => GetProperty(propertyName, string.Empty);

    public void SetProperty(string propertyName, string stringEncodedValue)
    {
        if (string.IsNullOrEmpty(propertyName)) return;
        _properties[propertyName] = stringEncodedValue ?? string.Empty;
    }

    public bool HasProperty(string propertyName) => _properties.ContainsKey(propertyName);

    private static ProjectConfig MapRootToModel(TomlTable root)
    {
        var projectSection = new ProjectSection();
        var pythonSection = new PythonSection();

        // [project]
        if (root.TryGetValue("project", out var projObj) && projObj is TomlTable proj)
        {
            var propsDict = new Dictionary<string, string>();
            if (proj.TryGetValue("properties", out var propsObj) && propsObj is TomlTable props)
            {
                foreach (var (k, v) in props)
                {
                    propsDict[k] = TomlValueToString(v);
                }
            }

            projectSection = projectSection with
            {
                ProjectVersion = proj.TryGetValue("project_version", out var pv) ? pv?.ToString() : null,
                CelbridgeVersion = proj.TryGetValue("celbridge_version", out var cv) ? cv?.ToString() : null,
                Properties = propsDict
            };
        }

        // [python]
        if (root.TryGetValue("python", out var pyObj) && pyObj is TomlTable py)
        {
            List<string>? packages = null;
            if (py.TryGetValue("packages", out var packagesObj) && packagesObj is TomlArray packagesArray)
            {
                packages = packagesArray.Select(x => x?.ToString() ?? string.Empty).ToList();
            }

            var scriptsDict = new Dictionary<string, string>();
            if (py.TryGetValue("scripts", out var scriptsObj) && scriptsObj is TomlTable scripts)
            {
                foreach (var (k, v) in scripts)
                {
                    scriptsDict[k] = v?.ToString() ?? string.Empty;
                }
            }

            pythonSection = pythonSection with
            {
                Version = py.TryGetValue("version", out var ver) ? ver?.ToString() : null,
                Packages = packages,
                Scripts = scriptsDict
            };
        }

        return new ProjectConfig
        {
            Project = projectSection,
            Python = pythonSection
        };
    }

    private static TomlTable BuildTomlFromModel(ProjectConfig config)
    {
        var project = new TomlTable
        {
            ["project_version"] = config.Project.ProjectVersion ?? string.Empty,
            ["celbridge_version"] = config.Project.CelbridgeVersion ?? string.Empty
        };

        var props = new TomlTable();
        foreach (var (k, v) in config.Project.Properties)
        {
            props[k] = v ?? string.Empty;
        }
        project["properties"] = props;

        var python = new TomlTable();
        if (!string.IsNullOrEmpty(config.Python.Version))
        {
            python["version"] = config.Python.Version;
        }

        if (config.Python.Packages is { Count: > 0 })
        {
            var packageArray = new TomlArray();
            foreach (var p in config.Python.Packages)
            {
                packageArray.Add(p);
            }
            python["packages"] = packageArray;
        }

        if (config.Python.Scripts is { Count: > 0 })
        {
            var scripts = new TomlTable();
            foreach (var (k, v) in config.Python.Scripts)
            {
                scripts[k] = v ?? string.Empty;
            }
            python["scripts"] = scripts;
        }

        return new TomlTable
        {
            ["project"] = project,
            ["python"] = python
        };
    }

    private static string TomlValueToString(object? value) =>
        value switch
        {
            null => string.Empty,
            string s => s,
            bool b => b ? "true" : "false",
            sbyte or byte or short or ushort or int or uint or long or ulong
                => Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty,
            float or double or decimal
                => Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty,
            DateTime dt => dt.ToString("o", CultureInfo.InvariantCulture),
            DateTimeOffset dto => dto.ToString("o", CultureInfo.InvariantCulture),
            TomlArray arr => "[" + string.Join(",", arr.Select(FormatArrayItem)) + "]",
            TomlTable => "<table>",
            _ => value.ToString() ?? string.Empty
        };

    private static string FormatArrayItem(object? item) =>
        item switch
        {
            null => "null",
            string s => $"\"{s.Replace("\"", "\\\"")}\"",
            bool b => b ? "true" : "false",
            sbyte or byte or short or ushort or int or uint or long or ulong
                => Convert.ToString(item, CultureInfo.InvariantCulture) ?? "0",
            float or double or decimal
                => Convert.ToString(item, CultureInfo.InvariantCulture) ?? "0",
            DateTime dt => $"\"{dt:O}\"",
            DateTimeOffset dto => $"\"{dto:O}\"",
            TomlTable or TomlArray => "\"<complex>\"",
            _ => $"\"{item.ToString()?.Replace("\"", "\\\"")}\""
        };

    private void SyncBagIntoModelProperties()
    {
        var propsCopy = new Dictionary<string, string>(_properties);
        var newProject = _config.Project with { Properties = propsCopy };
        _config = _config with { Project = newProject };
    }
}
