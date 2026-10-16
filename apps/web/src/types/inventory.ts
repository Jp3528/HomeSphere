export type InventoryMovementType = 'Receipt' | 'Adjustment' | 'ReservationRelease' | 'Sale' | 'Return';

export interface InventoryItem {
  variantId: string;
  productId: string;
  sku: string;
  variantName: string;
  productName: string;
  categoryName: string;
  price: number;
  stockQuantity: number;
  isActive: boolean;
  isLowStock: boolean;
  lastMovementUtc: string | null;
}

export interface InventoryMovement {
  id: string;
  variantId: string;
  sku: string;
  productName: string;
  type: number;
  typeName: string;
  quantityDelta: number;
  previousStock: number;
  newStock: number;
  reason: string;
  referenceType: string | null;
  referenceId: string | null;
  actor: string | null;
  createdAtUtc: string;
}

export interface AdjustStockPayload {
  variantId: string;
  quantityDelta: number;
  type: number;
  reason: string;
  referenceType?: string;
  referenceId?: string;
}
