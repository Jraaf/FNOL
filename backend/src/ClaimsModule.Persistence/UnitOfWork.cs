using ClaimsModule.Application.Common.Interfaces;
using ClaimsModule.Domain.Common;
using Microsoft.EntityFrameworkCore;

namespace ClaimsModule.Persistence;

public class UnitOfWork : IUnitOfWork
{
    private readonly ClaimsDbContext _db;
    private readonly IDomainEventDispatcher _dispatcher;

    public UnitOfWork(ClaimsDbContext db, IDomainEventDispatcher dispatcher)
    {
        _db = db;
        _dispatcher = dispatcher;
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken)
    {
        var aggregates = _db.ChangeTracker.Entries<AggregateRoot>()
            .Where(e => e.Entity.DomainEvents.Any())
            .Select(e => e.Entity)
            .ToList();

        var events = aggregates.SelectMany(a => a.DomainEvents).ToList();
        foreach (var a in aggregates) a.ClearDomainEvents();

        var result = await _db.SaveChangesAsync(cancellationToken);
        if (events.Count > 0)
            await _dispatcher.DispatchAsync(events, cancellationToken);
        return result;
    }
}
