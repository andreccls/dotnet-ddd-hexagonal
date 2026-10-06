using DddHexagonal.Application.Ports.Out;
using DddHexagonal.Domain.Common;
using Microsoft.EntityFrameworkCore;
using MySqlConnector;

namespace DddHexagonal.Infrastructure.Persistence;

internal sealed class UnitOfWork(AppDbContext db) : IUnitOfWork
{
    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConflictException("The record was changed by someone else. Reload it and try again.");
        }
        catch (DbUpdateException ex) when (ex.InnerException is MySqlException { ErrorCode: MySqlErrorCode.DuplicateKeyEntry })
        {
            // Race between the "already exists?" check and the INSERT: the unique index won.
            throw new ConflictException("A record with the same unique value already exists.");
        }
    }
}
