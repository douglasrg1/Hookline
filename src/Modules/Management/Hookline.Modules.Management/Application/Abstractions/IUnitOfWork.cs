namespace Hookline.Modules.Management.Application.Abstractions;

// Saves every change made through the repositories, in one transaction.
internal interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
