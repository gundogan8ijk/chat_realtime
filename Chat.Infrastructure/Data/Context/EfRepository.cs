namespace Chat.Infrastructure.Data.Context;

// inherit from Ardalis.Specification type
public class EfRepository<T>(ChatDbContext dbContext) :
  RepositoryBase<T>(dbContext), IReadRepository<T>, IRepository<T> where T : class, IAggregateRoot
{
}

