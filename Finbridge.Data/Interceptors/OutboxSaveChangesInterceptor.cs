using Finbridge.Data.Outbox;
using Finbridge.Domain.Common;
using Finbridge.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Finbridge.Data.Interceptors;

public sealed class OutboxSaveChangesInterceptor : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        ConvertDomainEventsToOutboxMessages(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        ConvertDomainEventsToOutboxMessages(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private static void ConvertDomainEventsToOutboxMessages(DbContext? context)
    {
        if (context is null)
        {
            return;
        }

        var aggregates = context.ChangeTracker
            .Entries<AggregateRoot<int>>()
            .Where(e => e.Entity.DomainEvents.Count > 0)
            .ToList();

        if (aggregates.Count == 0)
        {
            return;
        }

        var outboxMessages = aggregates
            .SelectMany(entry => entry.Entity.DomainEvents)
            .Select(OutboxMessage.FromDomainEvent)
            .ToList();

        context.Set<OutboxMessage>().AddRange(outboxMessages);

        foreach (var entry in aggregates)
        {
            entry.Entity.ClearDomainEvents();
        }
    }
}
