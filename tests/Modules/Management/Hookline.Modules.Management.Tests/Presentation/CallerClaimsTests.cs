using System.Security.Claims;

using Hookline.Modules.Management.Application.Authentication;
using Hookline.Modules.Management.Presentation.Authentication;

using Shouldly;

namespace Hookline.Modules.Management.Tests.Presentation;

public class CallerClaimsTests
{
    private const string AuthenticationType = "Bearer";

    private static readonly DateTimeOffset Now = new(2026, 11, 1, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Organization_is_read_back_from_its_claims()
    {
        var principal = CallerClaims.ToPrincipal(Caller.Organization, AuthenticationType);

        CallerClaims.FromPrincipal(principal).ShouldBe(Caller.Organization);
    }

    [Fact]
    public void Consumer_is_read_back_from_its_claims()
    {
        var caller = Caller.Consumer(Guid.CreateVersion7(Now));

        var principal = CallerClaims.ToPrincipal(caller, AuthenticationType);

        CallerClaims.FromPrincipal(principal).ShouldBe(caller);
    }

    [Fact]
    public void Anonymous_user_has_no_caller()
    {
        CallerClaims.FromPrincipal(new ClaimsPrincipal(new ClaimsIdentity())).ShouldBeNull();
    }

    [Fact]
    public void Consumer_role_without_a_valid_id_has_no_caller()
    {
        var identity = new ClaimsIdentity(
            [new Claim(ClaimTypes.Role, CallerClaims.ConsumerRole), new Claim(CallerClaims.ConsumerIdType, "not-a-guid")],
            AuthenticationType);

        CallerClaims.FromPrincipal(new ClaimsPrincipal(identity)).ShouldBeNull();
    }
}
