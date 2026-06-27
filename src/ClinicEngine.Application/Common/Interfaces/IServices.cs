namespace ClinicEngine.Application.Common.Interfaces;


public interface IUnitOfWork
{

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);


    Task ExecuteInTransactionAsync(Func<Task> action, CancellationToken cancellationToken = default);
}


public interface ICurrentUserService
{
    string UserName { get; }
}


public interface IDateTimeService
{
    DateTime UtcNow { get; }
}
