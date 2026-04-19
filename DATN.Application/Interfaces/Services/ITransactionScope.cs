namespace DATN.Application.Interfaces.Services;

/// <summary>
/// Abstraction cho database transaction, tránh phụ thuộc trực tiếp vào LLBLGen/DataAccessAdapter trong Application layer.
/// </summary>
public interface ITransactionScope : IDisposable
{
    void Commit();
    void Rollback();
}
