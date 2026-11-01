using Hookline.SharedKernel;

namespace Hookline.Modules.Management.Domain.Consumers;

internal static class ConsumerErrors
{
    public static readonly Error NameRequired = Error.Validation(
        "consumer.name_required",
        "The consumer name is required.");

    public static readonly Error NameTooLong = Error.Validation(
        "consumer.name_too_long",
        $"The consumer name must have at most {Consumer.NameMaxLength} characters.");

    public static readonly Error ExternalIdInvalid = Error.Validation(
        "consumer.external_id_invalid",
        $"The consumer external id must have 1 to {Consumer.ExternalIdMaxLength} characters, using only letters, digits, '_' and '-'.");
}
