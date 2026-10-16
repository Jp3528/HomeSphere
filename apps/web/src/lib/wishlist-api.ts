import { apiClient } from './api-client';
import { WishlistItem } from '@/types/wishlist';

export const wishlistApi = {
  getMyWishlist: async (): Promise<WishlistItem[]> => {
    return apiClient.get<WishlistItem[]>('/api/wishlist');
  },

  addToWishlist: async (productId: string): Promise<WishlistItem> => {
    return apiClient.post<WishlistItem>(`/api/wishlist/${productId}`);
  },

  removeFromWishlist: async (productId: string): Promise<void> => {
    return apiClient.delete<void>(`/api/wishlist/${productId}`);
  },

  checkStatus: async (productId: string): Promise<{ isInWishlist: boolean }> => {
    return apiClient.get<{ isInWishlist: boolean }>(`/api/wishlist/check/${productId}`);
  },
};
