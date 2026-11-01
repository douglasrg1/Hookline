using Hookline.Modules.Management.Domain.Endpoints;

using Shouldly;

namespace Hookline.Modules.Management.Tests.Domain;

public class EndpointTests
{
    private const string ValidUrl = "https://loja1.com/webhooks";

    private static readonly DateTimeOffset Now = new(2026, 10, 31, 12, 0, 0, TimeSpan.Zero);
    private static readonly Guid ConsumerId = Guid.CreateVersion7(Now);
    private static readonly byte[] Secret = [1, 2, 3];

    [Fact]
    public void Create_returns_an_enabled_endpoint()
    {
        var result = Endpoint.Create(ConsumerId, ValidUrl, ["invoice.paid", "invoice.overdue"], Secret, Now);

        result.IsSuccess.ShouldBeTrue();
        var endpoint = result.Value;
        endpoint.ConsumerId.ShouldBe(ConsumerId);
        endpoint.Url.ShouldBe(ValidUrl);
        endpoint.EventTypes.ShouldBe(["invoice.paid", "invoice.overdue"]);
        endpoint.EncryptedSecret.ShouldBe(Secret);
        endpoint.IsEnabled.ShouldBeTrue();
        endpoint.CreatedAt.ShouldBe(Now);
        endpoint.UpdatedAt.ShouldBe(Now);
        endpoint.Id.Version.ShouldBe(7);
    }

    [Theory]
    [InlineData("http://localhost:5000/webhooks")]
    [InlineData("http://127.0.0.1/webhooks")]
    public void Create_accepts_plain_http_only_for_local_hosts(string url)
    {
        Endpoint.Create(ConsumerId, url, [], Secret, Now).IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public void Create_rejects_plain_http_for_other_hosts()
    {
        Endpoint.Create(ConsumerId, "http://loja1.com/webhooks", [], Secret, Now).Error
            .ShouldBe(EndpointErrors.HttpsRequired);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_rejects_an_empty_url(string url)
    {
        Endpoint.Create(ConsumerId, url, [], Secret, Now).Error.ShouldBe(EndpointErrors.UrlRequired);
    }

    [Theory]
    [InlineData("not a url")]
    [InlineData("ftp://loja1.com/webhooks")]
    [InlineData("/webhooks")]
    public void Create_rejects_a_url_that_is_not_absolute_http(string url)
    {
        Endpoint.Create(ConsumerId, url, [], Secret, Now).Error.ShouldBe(EndpointErrors.UrlInvalid);
    }

    [Fact]
    public void Create_rejects_a_url_above_the_maximum_length()
    {
        var url = "https://loja1.com/" + new string('a', Endpoint.UrlMaxLength);

        Endpoint.Create(ConsumerId, url, [], Secret, Now).Error.ShouldBe(EndpointErrors.UrlTooLong);
    }

    [Theory]
    [InlineData("invoice.paid")]
    [InlineData("invoice.payment.failed")]
    [InlineData("order_v2.created")]
    public void Create_accepts_valid_event_types(string eventType)
    {
        Endpoint.Create(ConsumerId, ValidUrl, [eventType], Secret, Now).IsSuccess.ShouldBeTrue();
    }

    [Theory]
    [InlineData("invoice")]
    [InlineData("Invoice.Paid")]
    [InlineData("invoice..paid")]
    [InlineData("invoice.paid.")]
    [InlineData("invoice paid")]
    [InlineData("invoice-paid.now")]
    public void Create_rejects_invalid_event_types(string eventType)
    {
        Endpoint.Create(ConsumerId, ValidUrl, [eventType], Secret, Now).Error!.Code
            .ShouldBe("endpoint.event_type_invalid");
    }

    [Fact]
    public void Create_rejects_an_event_type_above_the_maximum_length()
    {
        var eventType = "invoice." + new string('a', Endpoint.EventTypeMaxLength);

        Endpoint.Create(ConsumerId, ValidUrl, [eventType], Secret, Now).Error!.Code
            .ShouldBe("endpoint.event_type_invalid");
    }

    [Fact]
    public void Create_rejects_a_duplicated_event_type()
    {
        Endpoint.Create(ConsumerId, ValidUrl, ["invoice.paid", "invoice.paid"], Secret, Now).Error!.Code
            .ShouldBe("endpoint.event_type_duplicated");
    }

    [Fact]
    public void Create_accepts_the_maximum_number_of_event_types()
    {
        var eventTypes = Enumerable.Range(0, Endpoint.MaxEventTypes).Select(i => $"event.type_{i}").ToArray();

        Endpoint.Create(ConsumerId, ValidUrl, eventTypes, Secret, Now).IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public void Create_rejects_more_than_the_maximum_number_of_event_types()
    {
        var eventTypes = Enumerable.Range(0, Endpoint.MaxEventTypes + 1).Select(i => $"event.type_{i}").ToArray();

        Endpoint.Create(ConsumerId, ValidUrl, eventTypes, Secret, Now).Error.ShouldBe(EndpointErrors.TooManyEventTypes);
    }

    [Fact]
    public void Create_rejects_an_empty_encrypted_secret()
    {
        Should.Throw<ArgumentException>(() => Endpoint.Create(ConsumerId, ValidUrl, [], [], Now));
    }

    [Fact]
    public void Endpoint_without_event_types_accepts_every_event()
    {
        var endpoint = Endpoint.Create(ConsumerId, ValidUrl, [], Secret, Now).Value;

        endpoint.Accepts("invoice.paid").ShouldBeTrue();
        endpoint.Accepts("order.created").ShouldBeTrue();
    }

    [Fact]
    public void Endpoint_with_event_types_accepts_only_those_events()
    {
        var endpoint = Endpoint.Create(ConsumerId, ValidUrl, ["invoice.paid"], Secret, Now).Value;

        endpoint.Accepts("invoice.paid").ShouldBeTrue();
        endpoint.Accepts("invoice.overdue").ShouldBeFalse();
        endpoint.Accepts("Invoice.Paid").ShouldBeFalse();
    }

    [Fact]
    public void Update_changes_the_url_and_event_types()
    {
        var endpoint = Endpoint.Create(ConsumerId, ValidUrl, ["invoice.paid"], Secret, Now).Value;

        var result = endpoint.Update("https://loja1.com/v2/webhooks", ["order.created"], Now.AddHours(1));

        result.IsSuccess.ShouldBeTrue();
        endpoint.Url.ShouldBe("https://loja1.com/v2/webhooks");
        endpoint.EventTypes.ShouldBe(["order.created"]);
        endpoint.UpdatedAt.ShouldBe(Now.AddHours(1));
        endpoint.CreatedAt.ShouldBe(Now);
    }

    [Fact]
    public void Update_with_invalid_data_keeps_the_endpoint_unchanged()
    {
        var endpoint = Endpoint.Create(ConsumerId, ValidUrl, ["invoice.paid"], Secret, Now).Value;

        var result = endpoint.Update("http://loja1.com/webhooks", ["order.created"], Now.AddHours(1));

        result.Error.ShouldBe(EndpointErrors.HttpsRequired);
        endpoint.Url.ShouldBe(ValidUrl);
        endpoint.EventTypes.ShouldBe(["invoice.paid"]);
        endpoint.UpdatedAt.ShouldBe(Now);
    }

    [Fact]
    public void Disable_and_enable_change_the_state_and_the_update_time()
    {
        var endpoint = Endpoint.Create(ConsumerId, ValidUrl, [], Secret, Now).Value;

        endpoint.Disable(Now.AddMinutes(1));
        endpoint.IsEnabled.ShouldBeFalse();
        endpoint.UpdatedAt.ShouldBe(Now.AddMinutes(1));

        endpoint.Enable(Now.AddMinutes(2));
        endpoint.IsEnabled.ShouldBeTrue();
        endpoint.UpdatedAt.ShouldBe(Now.AddMinutes(2));
    }

    [Fact]
    public void Disabling_an_already_disabled_endpoint_changes_nothing()
    {
        var endpoint = Endpoint.Create(ConsumerId, ValidUrl, [], Secret, Now).Value;
        endpoint.Disable(Now.AddMinutes(1));

        endpoint.Disable(Now.AddMinutes(5));

        endpoint.UpdatedAt.ShouldBe(Now.AddMinutes(1));
    }

    [Fact]
    public void Enabling_an_already_enabled_endpoint_changes_nothing()
    {
        var endpoint = Endpoint.Create(ConsumerId, ValidUrl, [], Secret, Now).Value;

        endpoint.Enable(Now.AddMinutes(5));

        endpoint.UpdatedAt.ShouldBe(Now);
    }
}
