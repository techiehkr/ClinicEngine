using ClinicEngine.Application.Common.Interfaces;
using ClinicEngine.Domain.Exceptions;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using System.Data;

namespace ClinicEngine.Infrastructure.Persistence;


public sealed class UnitOfWork : IUnitOfWork
{
    private readonly ApplicationDbContext _context;

    public UnitOfWork(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
           
            throw new ConcurrencyConflictException(
                "The record was modified by another user. Please refresh and try again.");
        }
        catch (DbUpdateException ex)
            when (ex.InnerException?.Message.Contains("IX_Appointments_DoctorId") == true
               || ex.InnerException?.Message.Contains("UNIQUE KEY") == true
               || ex.InnerException?.Message.Contains("unique index") == true)
        {

            throw new SlotUnavailableException(
                "This appointment slot is already booked. Please select a different time.");
        }
        catch (DbUpdateException ex)
            when (ex.InnerException is SqlException sqlEx && sqlEx.Number == 1205)
        {
   
            throw new ConcurrencyConflictException(
                "The request could not be completed due to high demand on this slot. " +
                "Please try again.");
        }
    }
    public async Task ExecuteInTransactionAsync(Func<Task> action, CancellationToken cancellationToken = default)
    {
        
        IExecutionStrategy strategy = _context.Database.CreateExecutionStrategy();

        await strategy.ExecuteAsync(async () =>
        {
            await using IDbContextTransaction transaction =
                await _context.Database.BeginTransactionAsync(
                    IsolationLevel.RepeatableRead,
                    cancellationToken);
            try
            {
                await action();
                await transaction.CommitAsync(cancellationToken);
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        });
    }
}
