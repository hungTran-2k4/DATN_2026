using DATN.Domain.Entities.Orders;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace DATN.Domain.Interfaces;

public interface IShipmentRepository
{
    Task<Shipment> CreateAsync(Shipment shipment, CancellationToken ct = default);
    Task<Shipment?> GetByOrderIdAsync(Guid orderId, CancellationToken ct = default);
    Task<bool> UpdateStatusAsync(Guid id, string status, CancellationToken ct = default);
}
