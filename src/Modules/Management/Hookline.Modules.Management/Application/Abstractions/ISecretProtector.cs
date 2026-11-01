namespace Hookline.Modules.Management.Application.Abstractions;

// Encrypts secrets that must be stored but read back later, such as endpoint signing secrets.
// Implemented in the Infrastructure layer, so use cases never depend on a specific algorithm.
internal interface ISecretProtector
{
    byte[] Protect(string plainText);

    string Unprotect(byte[] protectedData);
}
