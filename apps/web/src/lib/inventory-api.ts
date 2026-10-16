import { apiClient } from './api-client';
import { InventoryItem, InventoryMovement, AdjustStockPayload } from '@/types/inventory';

export const inventoryApi = {
  getInventory: async (search?: string, lowStockOnly?: boolean): Promise<InventoryItem[]> => {
    return apiClient.get<InventoryItem[]>('/api/admin/inventory', {
      params: { search, lowStockOnly },
    });
  },

  getMovements: async (variantId?: string, limit: number = 50): Promise<InventoryMovement[]> => {
    return apiClient.get<InventoryMovement[]>('/api/admin/inventory/movements', {
      params: { variantId, limit },
    });
  },

  adjustStock: async (payload: AdjustStockPayload): Promise<InventoryMovement> => {
    return apiClient.post<InventoryMovement>('/api/admin/inventory/adjust', payload);
  },
};
