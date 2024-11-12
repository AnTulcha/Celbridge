using Celbridge.ResourceData.Services;
using CommunityToolkit.Diagnostics;
using System.Text.Json.Nodes;

namespace Celbridge.Tests;

[TestFixture]
public class SchemaServiceTests
{
    private SchemaService? _schemaService;

    private string? _validSchemaJson;
    private string? _invalidSchemaJson;
    private string? _validDataJson;
    private string? _invalidDataJson;
    private string? _mismatchedSchemaNameDataJson;
    private string? _mismatchedSchemaVersionDataJson;
    private string? _tempSchemaFilePath;

    [SetUp]
    public void SetUp()
    {
        _schemaService = new SchemaService();

        // Sample valid schema with _schemaName and _schemaVersion
        _validSchemaJson = @"
        {
            ""_schemaName"": ""TestSchema"",
            ""_schemaVersion"": 1,
            ""type"": ""object"",
            ""properties"": {
                ""_schemaName"": { ""type"": ""string"", ""const"": ""TestSchema"" },
                ""_schemaVersion"": { ""type"": ""integer"", ""const"": 1 },
                ""name"": { ""type"": ""string"" },
                ""age"": { ""type"": ""integer"" }
            },
            ""required"": [""_schemaName"", ""_schemaVersion"", ""name"", ""age""]
        }";

        // Sample invalid schema without _schemaName or _schemaVersion
        _invalidSchemaJson = @"
        {
            ""type"": ""object"",
            ""properties"": {
                ""name"": { ""type"": ""string"" }
            }
        }";

        // Sample valid JSON data matching the schema
        _validDataJson = @"
        {
            ""_schemaName"": ""TestSchema"",
            ""_schemaVersion"": 1,
            ""name"": ""John Doe"",
            ""age"": 30
        }";

        // Sample JSON data with mismatched _schemaName
        _mismatchedSchemaNameDataJson = @"
        {
            ""_schemaName"": ""DifferentSchema"",
            ""_schemaVersion"": 1,
            ""name"": ""John Doe"",
            ""age"": 30
        }";

        // Sample JSON data with mismatched _schemaVersion
        _mismatchedSchemaVersionDataJson = @"
        {
            ""_schemaName"": ""TestSchema"",
            ""_schemaVersion"": 2,
            ""name"": ""John Doe"",
            ""age"": 30
        }";

        // Temporary schema file for testing LoadSchemaFromFile
        _tempSchemaFilePath = Path.GetTempFileName();
    }

    [TearDown]
    public void TearDown()
    {
        if (File.Exists(_tempSchemaFilePath))
        {
            File.Delete(_tempSchemaFilePath);
        }
    }

    [Test]
    public void AddSchema_WithValidSchema_ShouldReturnSuccess()
    {
        Guard.IsNotNull(_schemaService);
        Guard.IsNotNull(_validSchemaJson);

        var result = _schemaService.AddSchema(_validSchemaJson);
        result.IsSuccess.Should().BeTrue();
    }

    [Test]
    public void AddSchema_WithInvalidSchema_ShouldReturnFailure()
    {
        Guard.IsNotNull(_schemaService);
        Guard.IsNotNull(_invalidSchemaJson);

        var result = _schemaService.AddSchema(_invalidSchemaJson);
        result.IsSuccess.Should().BeFalse();
    }

    [Test]
    public void ValidateJsonNode_WithMatchingSchemaNameAndVersion_ShouldReturnSuccess()
    {
        Guard.IsNotNull(_schemaService);
        Guard.IsNotNull(_validSchemaJson);
        Guard.IsNotNull(_validDataJson);

        _schemaService.AddSchema(_validSchemaJson);
        var dataNode = JsonNode.Parse(_validDataJson) as JsonObject;
        Guard.IsNotNull(dataNode);

        var result = _schemaService.ValidateJsonNode(dataNode);
        result.IsSuccess.Should().BeTrue();
    }

    [Test]
    public void ValidateJsonNode_WithMismatchedSchemaName_ShouldReturnFailure()
    {
        Guard.IsNotNull(_schemaService);
        Guard.IsNotNull(_mismatchedSchemaNameDataJson);
        Guard.IsNotNull(_validSchemaJson);

        _schemaService.AddSchema(_validSchemaJson);
        var dataNode = JsonNode.Parse(_mismatchedSchemaNameDataJson) as JsonObject;
        Guard.IsNotNull(dataNode);

        var result = _schemaService.ValidateJsonNode(dataNode);
        result.IsSuccess.Should().BeFalse();
    }

    [Test]
    public void ValidateJsonNode_WithMismatchedSchemaVersion_ShouldReturnFailure()
    {
        Guard.IsNotNull(_schemaService);
        Guard.IsNotNull(_validSchemaJson);
        Guard.IsNotNull(_mismatchedSchemaVersionDataJson);

        _schemaService.AddSchema(_validSchemaJson);
        var dataNode = JsonNode.Parse(_mismatchedSchemaVersionDataJson) as JsonObject;
        Guard.IsNotNull(dataNode);

        var result = _schemaService.ValidateJsonNode(dataNode);
        result.IsSuccess.Should().BeFalse();
    }

    [Test]
    public void AddSchema_WithDuplicateSchemaName_ShouldReturnFailure()
    {
        Guard.IsNotNull(_schemaService);
        Guard.IsNotNull(_validSchemaJson);

        _schemaService.AddSchema(_validSchemaJson);
        var result = _schemaService.AddSchema(_validSchemaJson);
        result.IsSuccess.Should().BeFalse();
    }

    [Test]
    public void LoadSchemaFromFile_WithValidSchemaFile_ShouldReturnSuccess()
    {
        Guard.IsNotNull(_schemaService);
        Guard.IsNotNull(_tempSchemaFilePath);
        Guard.IsNotNull(_validSchemaJson);

        File.WriteAllText(_tempSchemaFilePath, _validSchemaJson);

        var result = _schemaService.LoadSchemaFromFile(_tempSchemaFilePath);
        result.IsSuccess.Should().BeTrue();
    }

    [Test]
    public void LoadSchemaFromFile_WithInvalidSchemaFile_ShouldReturnFailure()
    {
        Guard.IsNotNull(_schemaService);
        Guard.IsNotNull(_tempSchemaFilePath);

        File.WriteAllText(_tempSchemaFilePath, _invalidSchemaJson);

        var result = _schemaService.LoadSchemaFromFile(_tempSchemaFilePath);
        result.IsSuccess.Should().BeFalse();
    }
}
