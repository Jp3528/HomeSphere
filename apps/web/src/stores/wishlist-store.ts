import { create } from 'zustand';
import { WishlistItem } from '@/types/wishlist';
import { wishlistApi } from '@/lib/wishlist-api';

interface WishlistState {
  items: WishlistItem[];
  isLoading: boolean;
  error: string | null;

  fetchWishlist: () => Promise<void>;
  toggleWishlist: (productId: string) => Promise<boolean>; // returns true if added, false if removed
  removeItem: (productId: string) => Promise<void>;
  isFavorited: (productId: string) => boolean;
}

export const useWishlistStore = create<WishlistState>((set, get) => ({
  items: [],
  isLoading: false,
  error: null,

  fetchWishlist: async () => {
    set({ isLoading: true, error: null });
    try {
      const items = await wishlistApi.getMyWishlist();
      set({ items, isLoading: false });
    } catch {
      // If unauthorized (not logged in), silently reset
      set({ items: [], isLoading: false });
    }
  },

  toggleWishlist: async (productId: string) => {
    const { items } = get();
    const alreadyFavorited = items.some((i) => i.productId === productId);

    if (alreadyFavorited) {
      set({ items: items.filter((i) => i.productId !== productId) });
      try {
        await wishlistApi.removeFromWishlist(productId);
        return false;
      } catch (err) {
        // Rollback
        set({ items });
        throw err;
      }
    } else {
      try {
        const added = await wishlistApi.addToWishlist(productId);
        set({ items: [added, ...items] });
        return true;
      } catch (err) {
        throw err;
      }
    }
  },

  removeItem: async (productId: string) => {
    const { items } = get();
    set({ items: items.filter((i) => i.productId !== productId) });
    try {
      await wishlistApi.removeFromWishlist(productId);
    } catch (err) {
      set({ items });
      throw err;
    }
  },

  isFavorited: (productId: string) => {
    return get().items.some((i) => i.productId === productId);
  },
}));
