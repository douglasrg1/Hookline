using System.Security.Cryptography;
using System.Text;

using Hookline.Modules.Management.Domain.Endpoints;
using Hookline.Modules.Management.Infrastructure.Security;

using Microsoft.Extensions.Options;

using Shouldly;

namespace Hookline.Modules.Management.Tests.Infrastructure;

public class AesGcmSecretProtectorTests
{
    private const int VersionLength = 1;
    private const int NonceLength = 12;
    private const int TagLength = 16;

    private readonly AesGcmSecretProtector _protector = CreateProtector(NewKey(32));

    public static TheoryData<string> InvalidKeys => new()
    {
        string.Empty,
        "   ",
        "not base64!",
        NewKey(16),
        NewKey(48),
    };

    [Fact]
    public void Protected_secret_round_trips_to_the_original_text()
    {
        var secret = EndpointSecret.Generate();

        _protector.Unprotect(_protector.Protect(secret)).ShouldBe(secret);
    }

    [Fact]
    public void Protected_data_starts_with_the_key_version_and_has_the_expected_length()
    {
        var secret = EndpointSecret.Generate();

        var protectedData = _protector.Protect(secret);

        protectedData[0].ShouldBe((byte)1);
        protectedData.Length.ShouldBe(VersionLength + NonceLength + Encoding.UTF8.GetByteCount(secret) + TagLength);
    }

    [Fact]
    public void Same_text_protected_twice_produces_different_bytes()
    {
        var secret = EndpointSecret.Generate();

        _protector.Protect(secret).ShouldNotBe(_protector.Protect(secret));
    }

    [Fact]
    public void Protected_data_does_not_contain_the_plain_text()
    {
        var secret = EndpointSecret.Generate();

        var protectedData = _protector.Protect(secret);

        protectedData.AsSpan().IndexOf(Encoding.UTF8.GetBytes(secret)).ShouldBe(-1);
    }

    [Fact]
    public void Data_protected_with_another_key_cannot_be_read()
    {
        var protectedData = _protector.Protect(EndpointSecret.Generate());
        var otherProtector = CreateProtector(NewKey(32));

        Assert.ThrowsAny<CryptographicException>(() => otherProtector.Unprotect(protectedData));
    }

    [Theory]
    [InlineData(VersionLength + NonceLength)]
    [InlineData(-1)]
    public void Tampered_data_is_rejected(int indexToTamper)
    {
        var protectedData = _protector.Protect(EndpointSecret.Generate());
        var index = indexToTamper >= 0 ? indexToTamper : protectedData.Length - 1;

        protectedData[index] ^= 0x01;

        Assert.ThrowsAny<CryptographicException>(() => _protector.Unprotect(protectedData));
    }

    [Fact]
    public void Unknown_key_version_is_rejected()
    {
        var protectedData = _protector.Protect(EndpointSecret.Generate());

        protectedData[0] = 2;

        Assert.ThrowsAny<CryptographicException>(() => _protector.Unprotect(protectedData));
    }

    [Fact]
    public void Truncated_data_is_rejected()
    {
        var protectedData = _protector.Protect(EndpointSecret.Generate());

        Assert.ThrowsAny<CryptographicException>(() => _protector.Unprotect(protectedData[..10]));
    }

    [Theory, MemberData(nameof(InvalidKeys))]
    public void Invalid_master_key_is_rejected_when_the_protector_is_created(string key)
    {
        var exception = Should.Throw<InvalidOperationException>(() => CreateProtector(key));

        exception.Message.ShouldContain("Encryption:Key");
    }

    private static AesGcmSecretProtector CreateProtector(string key) =>
        new(Options.Create(new EncryptionOptions { Key = key }));

    private static string NewKey(int byteLength) => Convert.ToBase64String(RandomNumberGenerator.GetBytes(byteLength));
}
