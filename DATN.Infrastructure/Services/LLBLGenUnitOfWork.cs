using DATN.Application.Interfaces.Services;
using DATN_2026.DatabaseSpecific;

namespace DATN.Infrastructure.Services;

public class LLBLGenTransactionScope : ITransactionScope
{
    private readonly DataAccessAdapter _adapter;
    private bool _disposed;

    public LLBLGenTransactionScope(DataAccessAdapter adapter)
    {
        _adapter = adapter;
    }

    public void Commit()
    {
        _adapter.Commit();
    }

    public void Rollback()
    {
        try { _adapter.Rollback(); } catch { /* ignore rollback errors */ }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _disposed = true;
        }
    }
}

public class LLBLGenUnitOfWork : IUnitOfWork
{
    private readonly DataAccessAdapter _adapter;

    public LLBLGenUnitOfWork(DataAccessAdapter adapter)
    {
        _adapter = adapter;
    }

    public ITransactionScope BeginTransaction()
    {
        _adapter.StartTransaction(System.Data.IsolationLevel.ReadCommitted, "UoWTx");
        return new LLBLGenTransactionScope(_adapter);
    }
}
