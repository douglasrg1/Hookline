using System.Security.Cryptography;
using System.Text.RegularExpressions;

using Hookline.Modules.Management.Domain.ConsumerKeys;

using Shouldly;

namespace Hookline.Modules.Management.Tests.Domain;

public class ConsumerKeyTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 31, 12, 0, 0, TimeSpan.Zero);
    private static readonly Guid ConsumerId = Guid.CreateVersion7(Now);

    [Fact]
    public void Issue_returns_an_active_key_for_the_consumer()
    {
        var (key, _) = ConsumerKey.Issue(ConsumerId, Now);

        key.ConsumerId.ShouldBe(ConsumerId);
        key.CreatedAt.ShouldBe(Now);
        key.IsRevoked.ShouldBeFalse();
        key.Id.Version.ShouldBe(7);
    }

    [Fact]
    public void Issued_key_has_the_prefix_and_32_random_bytes_in_url_safe_base64()
    {
        var (_, plainTextKey) = ConsumerKey.Issue(ConsumerId, Now);

        plainTextKey.ShouldStartWith(ConsumerKey.KeyPrefix);
        Regex.IsMatch(plainTextKey, "^hl_[A-Za-z0-9_-]{43}$").ShouldBeTrue();
    }

    [Fact]
    public void Issued_key_stores_only_the_hash_of_the_plain_text_key()
    {
        var (key, plainTextKey) = ConsumerKey.Issue(ConsumerId, Now);

        key.Hash.Length.ShouldBe(SHA256.HashSizeInBytes);
        key.Hash.ShouldBe(ConsumerKey.ComputeHash(plainTextKey));
    }

    [Fact]
    public void Display_prefix_identifies_the_key_without_revealing_it()
    {
        var (key, plainTextKey) = ConsumerKey.Issue(ConsumerId, Now);

        plainTextKey.ShouldStartWith(key.DisplayPrefix);
        key.DisplayPrefix.ShouldStartWith(ConsumerKey.KeyPrefix);
        key.DisplayPrefix.Length.ShouldBeLessThan(plainTextKey.Length / 3);
    }

    [Fact]
    public void Each_issued_key_is_different()
    {
        var (first, firstPlainText) = ConsumerKey.Issue(ConsumerId, Now);
        var (second, secondPlainText) = ConsumerKey.Issue(ConsumerId, Now);

        firstPlainText.ShouldNotBe(secondPlainText);
        first.Hash.ShouldNotBe(second.Hash);
        first.Id.ShouldNotBe(second.Id);
    }

    [Fact]
    public void Compute_hash_is_stable_for_the_same_key_and_differs_between_keys()
    {
        ConsumerKey.ComputeHash("hl_same").ShouldBe(ConsumerKey.ComputeHash("hl_same"));
        ConsumerKey.ComputeHash("hl_one").ShouldNotBe(ConsumerKey.ComputeHash("hl_two"));
    }

    [Fact]
    public void Revoke_marks_the_key_as_revoked()
    {
        var (key, _) = ConsumerKey.Issue(ConsumerId, Now);

        key.Revoke(Now.AddMinutes(5));

        key.IsRevoked.ShouldBeTrue();
        key.RevokedAt.ShouldBe(Now.AddMinutes(5));
    }

    [Fact]
    public void Revoking_twice_keeps_the_first_revocation_time()
    {
        var (key, _) = ConsumerKey.Issue(ConsumerId, Now);

        key.Revoke(Now.AddMinutes(5));
        key.Revoke(Now.AddMinutes(10));

        key.RevokedAt.ShouldBe(Now.AddMinutes(5));
    }
}
