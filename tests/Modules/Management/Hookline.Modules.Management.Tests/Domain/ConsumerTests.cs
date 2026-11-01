using Hookline.Modules.Management.Domain.Consumers;

using Shouldly;

namespace Hookline.Modules.Management.Tests.Domain;

public class ConsumerTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 31, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Create_returns_a_consumer_with_a_time_ordered_id()
    {
        var result = Consumer.Create("  Loja 1  ", "store-1", Now);

        result.IsSuccess.ShouldBeTrue();
        var consumer = result.Value;
        consumer.Name.ShouldBe("Loja 1");
        consumer.ExternalId.ShouldBe("store-1");
        consumer.CreatedAt.ShouldBe(Now);
        consumer.Id.Version.ShouldBe(7);
    }

    [Fact]
    public void Create_accepts_a_consumer_without_external_id()
    {
        var result = Consumer.Create("Loja 1", null, Now);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ExternalId.ShouldBeNull();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_rejects_an_empty_name(string name)
    {
        Consumer.Create(name, null, Now).Error.ShouldBe(ConsumerErrors.NameRequired);
    }

    [Fact]
    public void Create_accepts_a_name_at_the_maximum_length()
    {
        Consumer.Create(new string('a', Consumer.NameMaxLength), null, Now).IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public void Create_rejects_a_name_above_the_maximum_length()
    {
        Consumer.Create(new string('a', Consumer.NameMaxLength + 1), null, Now).Error
            .ShouldBe(ConsumerErrors.NameTooLong);
    }

    [Theory]
    [InlineData("store-1")]
    [InlineData("STORE_1")]
    [InlineData("a")]
    public void Create_accepts_a_valid_external_id(string externalId)
    {
        Consumer.Create("Loja 1", externalId, Now).IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public void Create_accepts_an_external_id_at_the_maximum_length()
    {
        Consumer.Create("Loja 1", new string('a', Consumer.ExternalIdMaxLength), Now).IsSuccess.ShouldBeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("store 1")]
    [InlineData("loja.1")]
    [InlineData("ação")]
    public void Create_rejects_an_invalid_external_id(string externalId)
    {
        Consumer.Create("Loja 1", externalId, Now).Error.ShouldBe(ConsumerErrors.ExternalIdInvalid);
    }

    [Fact]
    public void Create_rejects_an_external_id_above_the_maximum_length()
    {
        Consumer.Create("Loja 1", new string('a', Consumer.ExternalIdMaxLength + 1), Now).Error
            .ShouldBe(ConsumerErrors.ExternalIdInvalid);
    }

    [Fact]
    public void Each_consumer_gets_its_own_id()
    {
        var first = Consumer.Create("Loja 1", null, Now).Value;
        var second = Consumer.Create("Loja 2", null, Now).Value;

        first.Id.ShouldNotBe(second.Id);
    }
}
