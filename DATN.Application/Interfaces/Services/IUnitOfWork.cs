namespace DATN.Application.Interfaces.Services;

/// <summary>
/// Unit of Work abstraction để bắt đầu transaction từ Application layer.
/// </summary>
public interface IUnitOfWork
{
    ITransactionScope BeginTransaction();
}
