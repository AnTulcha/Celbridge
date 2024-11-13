using Celbridge.Entities.Models;
using Celbridge.Entities.Services;
using CommunityToolkit.Diagnostics;

namespace Celbridge.Tests;

[TestFixture]
public class JsonDatastoreTests
{
    private EntitySchemaService? _schemaService;
    private EntitySchema? _entitySchema;
    private string? _validSchemaJson;

    [SetUp]
    public void SetUp()
    {
        _schemaService = new EntitySchemaService();

        // Sample valid schema with _schemaName and _schemaVersion
        _validSchemaJson = @"
        {
            ""_schemaName"": ""TestSchema"",
            ""_schemaVersion"": 1,
            ""type"": ""object"",
            ""properties"": {
                ""_schemaName"": { ""type"": ""string"", ""const"": ""TestSchema"" },
                ""_schemaVersion"": { ""type"": ""integer"", ""const"": 1 }
            },
            ""required"": [""_schemaName"", ""_schemaVersion""]
        }";

        _schemaService.AddSchema(_validSchemaJson);

        _entitySchema = _schemaService.GetSchemaByName("TestSchema").Value;
    }

    [Test]
    public void Initialize_ShouldFail_WhenJsonIsInvalid()
    {
        Guard.IsNotNull(_entitySchema);

        var jsonData = new JsonDatastore();
        var invalidJson = "{ invalid json";

        var result = jsonData.Initialize(invalidJson, _entitySchema);

        result.IsFailure.Should().BeTrue();
    }

    [Test]
    public void Initialize_ShouldFail_WhenJsonIsNotAnObject()
    {
        Guard.IsNotNull(_entitySchema);

        var jsonData = new JsonDatastore();
        var jsonArray = "[1, 2, 3]";

        var result = jsonData.Initialize(jsonArray, _entitySchema);

        result.IsFailure.Should().BeTrue();
    }

    [Test]
    public void Initialize_ShouldFail_WhenJsonIsMissingSchema()
    {
        Guard.IsNotNull(_entitySchema);

        var jsonData = new JsonDatastore();
        var jsonWithoutSchema = "{\"name\": \"Test\"}";

        var result = jsonData.Initialize(jsonWithoutSchema, _entitySchema);

        result.IsFailure.Should().BeTrue();
    }

    [Test]
    public void Initialize_ShouldFail_WhenSchemaNameIsEmpty()
    {
        Guard.IsNotNull(_entitySchema);

        var jsonData = new JsonDatastore();
        var jsonWithEmptySchema = "{\"$schema\": \"\"}";

        var result = jsonData.Initialize(jsonWithEmptySchema, _entitySchema);

        result.IsFailure.Should().BeTrue();
    }

    [Test]
    public void Initialize_ShouldSucceed_WhenJsonIsValid()
    {
        Guard.IsNotNull(_entitySchema);

        var jsonData = new JsonDatastore();
        var validJson = "{\"_schemaName\": \"TestSchema\", \"_schemaVersion\": 1, \"name\": \"Test\"}";

        var result = jsonData.Initialize(validJson, _entitySchema);

        result.IsSuccess.Should().BeTrue();
    }
}
