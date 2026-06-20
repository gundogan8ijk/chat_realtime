using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Chat.Infrastructure.Data.Context;

// Intercepts SaveChanges to dispatch domain events before changes are successfully saved
public class EventDispatchInterceptor(IDomainEventDispatcher domainEventDispatcher) : SaveChangesInterceptor
{
  private readonly IDomainEventDispatcher _domainEventDispatcher = domainEventDispatcher;

  public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
      DbContextEventData eventData,
      InterceptionResult<int> result,
      CancellationToken cancellationToken = default)
  {
    var context = eventData.Context;
    if (context is ChatDbContext chatDbContext)
    {
      await DispatchEvents(chatDbContext);
    }
    return await base.SavingChangesAsync(eventData, result, cancellationToken);
  }

  public override InterceptionResult<int> SavingChanges(
      DbContextEventData eventData,
      InterceptionResult<int> result)
  {
    var context = eventData.Context;
    if (context is ChatDbContext chatDbContext)
    {
      DispatchEvents(chatDbContext).GetAwaiter().GetResult();
    }
    return base.SavingChanges(eventData, result);
  }

  private async Task DispatchEvents(ChatDbContext context)
  {
    var entitiesWithEvents = context.ChangeTracker.Entries<HasDomainEventsBase>()
      .Select(e => e.Entity)
      .Where(e => e.DomainEvents.Count > 0)
      .ToArray();

    await _domainEventDispatcher.DispatchAndClearEvents(entitiesWithEvents);
  }
}

