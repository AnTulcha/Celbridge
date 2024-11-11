using Newtonsoft.Json.Linq;
using Celbridge.ResourceData;

namespace Celbridge.Tests;

public class EntityPatcherTests
{
    [Test]
    public void Set_AddNewProperty_ShouldSucceed()
    {
        var json = JObject.Parse("{ 'name': 'John' }");
        var patcher = new EntityPatcher();

        patcher.Set("age", 30);
        var result = patcher.Apply(json);

        result.IsSuccess.Should().BeTrue();
        json["age"]!.Value<int>().Should().Be(30);
    }

    [Test]
    public void Set_ReplaceExistingProperty_ShouldSucceed()
    {
        var json = JObject.Parse("{ 'name': 'John' }");
        var patcher = new EntityPatcher();

        patcher.Set("name", "Jane");
        var result = patcher.Apply(json);

        result.IsSuccess.Should().BeTrue();
        json["name"]!.Value<string>().Should().Be("Jane");
    }

    [Test]
    public void Remove_ExistingProperty_ShouldSucceed()
    {
        var json = JObject.Parse("{ 'name': 'John', 'age': 30 }");
        var patcher = new EntityPatcher();

        patcher.Remove("age");
        var result = patcher.Apply(json);

        result.IsSuccess.Should().BeTrue();
        json["age"].Should().BeNull();
    }

    [Test]
    public void Remove_NonExistentProperty_ShouldFail()
    {
        var json = JObject.Parse("{ 'name': 'John' }");
        var patcher = new EntityPatcher();

        patcher.Remove("age");
        var result = patcher.Apply(json);

        result.IsFailure.Should().BeTrue();
    }

    [Test]
    public void Move_PropertyToNewPath_ShouldSucceed()
    {
        var json = JObject.Parse("{ 'name': 'John', 'address': { 'street': '123 Main St' } }");
        var patcher = new EntityPatcher();

        patcher.Move("address.street", "street");
        var result = patcher.Apply(json);

        result.IsSuccess.Should().BeTrue();
        json["street"]!.Value<string>().Should().Be("123 Main St");
        json.SelectToken("address.street").Should().BeNull();
    }

    [Test]
    public void Move_NonExistentSourcePath_ShouldFail()
    {
        var json = JObject.Parse("{ 'name': 'John' }");
        var patcher = new EntityPatcher();

        patcher.Move("address.street", "street");
        var result = patcher.Apply(json);

        result.IsFailure.Should().BeTrue();
    }

    [Test]
    public void Copy_PropertyToNewPath_ShouldSucceed()
    {
        var json = JObject.Parse("{ 'name': 'John', 'address': { 'street': '123 Main St' } }");
        var patcher = new EntityPatcher();

        patcher.Copy("address.street", "street");
        var result = patcher.Apply(json);

        result.IsSuccess.Should().BeTrue();
        json["street"]!.Value<string>().Should().Be("123 Main St");
        json.SelectToken("address.street")!.Value<string>().Should().Be("123 Main St");
    }

    [Test]
    public void Copy_NonExistentSourcePath_ShouldFail()
    {
        var json = JObject.Parse("{ 'name': 'John' }");
        var patcher = new EntityPatcher();

        patcher.Copy("address.street", "street");
        var result = patcher.Apply(json);

        result.IsFailure.Should().BeTrue();
    }

    [Test]
    public void Test_PropertyMatches_ShouldSucceed()
    {
        var json = JObject.Parse("{ 'name': 'John' }");
        var patcher = new EntityPatcher();

        patcher.Test("name", "John");
        var result = patcher.Apply(json);

        result.IsSuccess.Should().BeTrue();
    }

    [Test]
    public void Test_PropertyDoesNotMatch_ShouldFail()
    {
        var json = JObject.Parse("{ 'name': 'John' }");
        var patcher = new EntityPatcher();

        patcher.Test("name", "Jane");
        var result = patcher.Apply(json);

        result.IsFailure.Should().BeTrue();
    }

    [Test]
    public void Apply_MultipleOperations_ShouldSucceed()
    {
        var json = JObject.Parse("{ 'name': 'John' }");
        var patcher = new EntityPatcher();

        patcher.Set("age", 30);
        patcher.Set("name", "Jane");
        patcher.Remove("name");

        var result = patcher.Apply(json);

        result.IsSuccess.Should().BeTrue();
        json["age"]!.Value<int>().Should().Be(30);
        json["name"].Should().BeNull();
    }

    [Test]
    public void Undo_SetAndRemoveOperations_ShouldSucceed()
    {
        var json = JObject.Parse("{ 'name': 'John' }");
        var patcher = new EntityPatcher();

        patcher.Set("age", 30);
        patcher.Remove("name");

        patcher.Apply(json);
        var undoResult = patcher.Undo(json);

        undoResult.IsSuccess.Should().BeTrue();
        json["name"]!.Value<string>().Should().Be("John");
        json["age"].Should().BeNull();
    }

    [Test]
    public void Undo_MoveOperation_ShouldSucceed()
    {
        var json = JObject.Parse("{ 'name': 'John', 'address': { 'street': '123 Main St' } }");
        var patcher = new EntityPatcher();

        patcher.Move("address.street", "street");

        patcher.Apply(json);
        var undoResult = patcher.Undo(json);

        undoResult.IsSuccess.Should().BeTrue();
        json.SelectToken("address.street")!.Value<string>().Should().Be("123 Main St");
        json["street"].Should().BeNull();
    }

    [Test]
    public void Undo_CopyOperation_ShouldSucceed()
    {
        var json = JObject.Parse("{ 'name': 'John', 'address': { 'street': '123 Main St' } }");
        var patcher = new EntityPatcher();

        patcher.Copy("address.street", "street");

        patcher.Apply(json);
        var undoResult = patcher.Undo(json);

        undoResult.IsSuccess.Should().BeTrue();
        json["street"].Should().BeNull();
        json.SelectToken("address.street")!.Value<string>().Should().Be("123 Main St");
    }

    [Test]
    public void Undo_TestOperation_ShouldDoNothing()
    {
        var json = JObject.Parse("{ 'name': 'John' }");
        var patcher = new EntityPatcher();

        patcher.Test("name", "John");

        patcher.Apply(json);
        var undoResult = patcher.Undo(json);

        undoResult.IsSuccess.Should().BeTrue();
        json["name"]!.Value<string>().Should().Be("John");
    }
}
