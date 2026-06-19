using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Chat.Infrastructure.Data.Context;

// Intercepts SaveChanges to dispatch domain events after changes are successfully saved
public class EventDispatchInterceptor(IDomainEventDispatcher domainEventDispatcher) : SaveChangesInterceptor
{
  private readonly IDomainEventDispatcher _domainEventDispatcher = domainEventDispatcher;

  public override async ValueTask<int> SavedChangesAsync(SaveChangesCompletedEventData eventData, int result,
    CancellationToken cancellationToken = new CancellationToken())
  {
    var context = eventData.Context;
    if (context is not ChatDbContext chatDbContext)
    {
      return await base.SavedChangesAsync(eventData, result, cancellationToken).ConfigureAwait(false);
    }

    var entitiesWithEvents = chatDbContext.ChangeTracker.Entries<HasDomainEventsBase>()
      .Select(e => e.Entity)
      .Where(e => e.DomainEvents.Count > 0)
      .ToArray();

    await _domainEventDispatcher.DispatchAndClearEvents(entitiesWithEvents);

    return await base.SavedChangesAsync(eventData, result, cancellationToken);
  }
}

