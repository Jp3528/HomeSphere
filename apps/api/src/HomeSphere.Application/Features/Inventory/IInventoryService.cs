namespace HomeSphere.Application.Features.Inventory;

public interface IInventoryService
{
    Task<IReadOnlyList<InventoryItemDto>> GetInventoryItemsAsync(string? search = null, bool? lowStockOnly = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<InventoryMovementDto>> GetMovementsAsync(Guid? variantId = null, int limit = 50, CancellationToken cancellationToken = default);
    Task<InventoryMovementDto> AdjustStockAsync(AdjustStockRequest request, string? actorEmail = null, CancellationToken cancellationToken = default);
}
