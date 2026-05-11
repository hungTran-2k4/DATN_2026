using DATN.Domain.Entities.Orders;
using DATN.Domain.Interfaces;
using DATN_2026.DatabaseSpecific;
using DATN_2026.EntityClasses;
using DATN_2026.Linq;
using SD.LLBLGen.Pro.LinqSupportClasses;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DATN.Infrastructure.Persistence.Repositories.Orders;

/// <summary>
/// ShipmentRepository — sử dụng LLBLGen Pro Entity và LinqMetaData.
/// </summary>
public class ShipmentRepository : IShipmentRepository
{
    private readonly DataAccessAdapter _adapter;

    public ShipmentRepository(DataAccessAdapter adapter)
    {
        _adapter = adapter;
    }

    public async Task<Shipment> CreateAsync(Shipment shipment, CancellationToken ct = default)
    {
        if (shipment.Id == Guid.Empty) shipment.Id = Guid.NewGuid();

        var entity = new ShipmentEntity
        {
            Id = shipment.Id,
            OrderId = shipment.OrderId,
            Provider = shipment.Provider,
            TrackingCode = shipment.TrackingCode,
            ShippingFee = shipment.ShippingFee,
            Status = shipment.Status,
            ExpectedDeliveryDate = shipment.ExpectedDeliveryDate,
            GhnOrderCode = shipment.GhnOrderCode,
            CreatedAt = shipment.CreatedAt,
            UpdatedAt = shipment.UpdatedAt
        };

        await _adapter.SaveEntityAsync(entity, true, cancellationToken: ct);
        return shipment;
    }

    public async Task<Shipment?> GetByOrderIdAsync(Guid orderId, CancellationToken ct = default)
    {
        var metaData = new LinqMetaData(_adapter);
        var entity = await metaData.Shipment
            .Where(s => s.OrderId == orderId)
            .OrderByDescending(s => s.CreatedAt)
            .FirstOrDefaultAsync(ct);

        if (entity == null) return null;

        return new Shipment
        {
            Id = entity.Id,
            OrderId = entity.OrderId,
            Provider = entity.Provider,
            TrackingCode = entity.TrackingCode,
            ShippingFee = entity.ShippingFee ?? 0,
            Status = entity.Status,
            ExpectedDeliveryDate = entity.ExpectedDeliveryDate,
            GhnOrderCode = entity.GhnOrderCode,
            CreatedAt = entity.CreatedAt ?? DateTime.UtcNow,
            UpdatedAt = entity.UpdatedAt ?? DateTime.UtcNow
        };
    }

    public async Task<bool> UpdateStatusAsync(Guid id, string status, CancellationToken ct = default)
    {
        var metaData = new LinqMetaData(_adapter);
        var entity = await metaData.Shipment.FirstOrDefaultAsync(s => s.Id == id, ct);
        if (entity != null)
        {
            entity.Status = status;
            entity.UpdatedAt = DateTime.UtcNow;
            return await _adapter.SaveEntityAsync(entity, true, cancellationToken: ct);
        }
        return false;
    }
}
