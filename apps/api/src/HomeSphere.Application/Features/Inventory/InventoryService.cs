using HomeSphere.Application.Common.Interfaces;
using HomeSphere.Domain.Entities;
using HomeSphere.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace HomeSphere.Application.Features.Inventory;

public class InventoryService : IInventoryService
{
    private readonly IApplicationDbContext _context;

    public InventoryService(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<InventoryItemDto>> GetInventoryItemsAsync(
        string? search = null,
        bool? lowStockOnly = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.ProductVariants
            .AsNoTracking()
            .Include(v => v.Product)
                .ThenInclude(p => p.Category)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchLower = search.Trim().ToLower();
            query = query.Where(v =>
                v.Sku.ToLower().Contains(searchLower) ||
                v.Name.ToLower().Contains(searchLower) ||
                v.Product.Name.ToLower().Contains(searchLower));
        }

        if (lowStockOnly == true)
        {
            query = query.Where(v => v.StockQuantity <= 5);
        }

        var variants = await query
            .OrderBy(v => v.StockQuantity)
            .ThenBy(v => v.Product.Name)
            .ToListAsync(cancellationToken);

        var variantIds = variants.Select(v => v.Id).ToList();
        var lastMovements = await _context.InventoryMovements
            .AsNoTracking()
            .Where(m => variantIds.Contains(m.ProductVariantId))
            .GroupBy(m => m.ProductVariantId)
            .Select(g => new { VariantId = g.Key, LastDate = g.Max(m => m.CreatedAtUtc) })
            .ToDictionaryAsync(x => x.VariantId, x => (DateTime?)x.LastDate, cancellationToken);

        return variants.Select(v => new InventoryItemDto(
            VariantId: v.Id,
            ProductId: v.ProductId,
            Sku: v.Sku,
            VariantName: v.Name,
            ProductName: v.Product?.Name ?? string.Empty,
            CategoryName: v.Product?.Category?.Name ?? string.Empty,
            Price: v.Price,
            StockQuantity: v.StockQuantity,
            IsActive: v.IsActive,
            IsLowStock: v.StockQuantity <= 5,
            LastMovementUtc: lastMovements.TryGetValue(v.Id, out var date) ? date : null
        )).ToList();
    }

    public async Task<IReadOnlyList<InventoryMovementDto>> GetMovementsAsync(
        Guid? variantId = null,
        int limit = 50,
        CancellationToken cancellationToken = default)
    {
        var query = _context.InventoryMovements
            .AsNoTracking()
            .Include(m => m.ProductVariant)
                .ThenInclude(v => v.Product)
            .AsQueryable();

        if (variantId.HasValue)
        {
            query = query.Where(m => m.ProductVariantId == variantId.Value);
        }

        var movements = await query
            .OrderByDescending(m => m.CreatedAtUtc)
            .Take(limit)
            .ToListAsync(cancellationToken);

        return movements.Select(m => new InventoryMovementDto(
            Id: m.Id,
            VariantId: m.ProductVariantId,
            Sku: m.ProductVariant?.Sku ?? string.Empty,
            ProductName: m.ProductVariant?.Product?.Name ?? string.Empty,
            Type: m.Type,
            TypeName: m.Type.ToString(),
            QuantityDelta: m.QuantityDelta,
            PreviousStock: m.PreviousStock,
            NewStock: m.NewStock,
            Reason: m.Reason,
            ReferenceType: m.ReferenceType,
            ReferenceId: m.ReferenceId,
            Actor: m.Actor,
            CreatedAtUtc: m.CreatedAtUtc
        )).ToList();
    }

    public async Task<InventoryMovementDto> AdjustStockAsync(
        AdjustStockRequest request,
        string? actorEmail = null,
        CancellationToken cancellationToken = default)
    {
        var variant = await _context.ProductVariants
            .Include(v => v.Product)
            .FirstOrDefaultAsync(v => v.Id == request.VariantId, cancellationToken);

        if (variant == null)
        {
            throw new NotFoundException("ProductVariant", request.VariantId);
        }

        var previousStock = variant.StockQuantity;
        var newStock = previousStock + request.QuantityDelta;

        if (newStock < 0)
        {
            throw new BusinessRuleValidationException(
                $"El ajuste de {request.QuantityDelta} unidades resultaría en un stock negativo ({newStock}). Stock disponible actual: {previousStock}.");
        }

        variant.StockQuantity = newStock;
        variant.UpdatedAtUtc = DateTime.UtcNow;

        var movement = new InventoryMovement
        {
            ProductVariantId = variant.Id,
            ProductVariant = variant,
            Type = request.Type,
            QuantityDelta = request.QuantityDelta,
            PreviousStock = previousStock,
            NewStock = newStock,
            Reason = request.Reason.Trim(),
            ReferenceType = request.ReferenceType ?? "ManualAdjustment",
            ReferenceId = request.ReferenceId,
            Actor = actorEmail ?? "system",
            CreatedAtUtc = DateTime.UtcNow
        };

        _context.InventoryMovements.Add(movement);
        await _context.SaveChangesAsync(cancellationToken);

        return new InventoryMovementDto(
            Id: movement.Id,
            VariantId: variant.Id,
            Sku: variant.Sku,
            ProductName: variant.Product?.Name ?? string.Empty,
            Type: movement.Type,
            TypeName: movement.Type.ToString(),
            QuantityDelta: movement.QuantityDelta,
            PreviousStock: movement.PreviousStock,
            NewStock: movement.NewStock,
            Reason: movement.Reason,
            ReferenceType: movement.ReferenceType,
            ReferenceId: movement.ReferenceId,
            Actor: movement.Actor,
            CreatedAtUtc: movement.CreatedAtUtc
        );
    }
}
