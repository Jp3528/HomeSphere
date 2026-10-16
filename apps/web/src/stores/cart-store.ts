import { create } from 'zustand';
import { apiClient, getCartToken, setCartToken } from '@/lib/api-client';
import { Cart, AddToCartPayload, UpdateCartItemPayload } from '@/types/cart';

interface CartState {
  cart: Cart | null;
  isOpen: boolean;
  isLoading: boolean;
  error: string | null;

  openDrawer: () => void;
  closeDrawer: () => void;
  clearError: () => void;

  fetchCart: () => Promise<void>;
  addItem: (variantId: string, quantity?: number) => Promise<void>;
  updateQuantity: (itemId: string, quantity: number) => Promise<void>;
  removeItem: (itemId: string) => Promise<void>;
  clearCart: () => Promise<void>;
  mergeWithUser: () => Promise<void>;
}

export const useCartStore = create<CartState>((set) => ({
  cart: null,
  isOpen: false,
  isLoading: false,
  error: null,

  openDrawer: () => set({ isOpen: true }),
  closeDrawer: () => set({ isOpen: false }),
  clearError: () => set({ error: null }),

  fetchCart: async () => {
    set({ isLoading: true, error: null });
    try {
      const cart = await apiClient.get<Cart>('/api/cart');
      if (cart.guestToken) {
        setCartToken(cart.guestToken);
      }
      set({ cart, isLoading: false });
    } catch (err: unknown) {
      const msg = err instanceof Error ? err.message : 'Error al cargar el carrito';
      set({ error: msg, isLoading: false });
    }
  },

  addItem: async (variantId: string, quantity: number = 1) => {
    set({ isLoading: true, error: null });
    try {
      const payload: AddToCartPayload = { variantId, quantity };
      const cart = await apiClient.post<Cart>('/api/cart/items', payload);
      if (cart.guestToken) {
        setCartToken(cart.guestToken);
      }
      set({ cart, isOpen: true, isLoading: false });
    } catch (err: unknown) {
      const msg = err instanceof Error ? err.message : 'No se pudo agregar el producto al carrito';
      set({ error: msg, isLoading: false });
      throw err;
    }
  },

  updateQuantity: async (itemId: string, quantity: number) => {
    set({ isLoading: true, error: null });
    try {
      const payload: UpdateCartItemPayload = { quantity };
      const cart = await apiClient.put<Cart>(`/api/cart/items/${itemId}`, payload);
      set({ cart, isLoading: false });
    } catch (err: unknown) {
      const msg = err instanceof Error ? err.message : 'No se pudo actualizar la cantidad';
      set({ error: msg, isLoading: false });
      throw err;
    }
  },

  removeItem: async (itemId: string) => {
    set({ isLoading: true, error: null });
    try {
      const cart = await apiClient.delete<Cart>(`/api/cart/items/${itemId}`);
      set({ cart, isLoading: false });
    } catch (err: unknown) {
      const msg = err instanceof Error ? err.message : 'No se pudo eliminar el producto';
      set({ error: msg, isLoading: false });
      throw err;
    }
  },

  clearCart: async () => {
    set({ isLoading: true, error: null });
    try {
      const cart = await apiClient.delete<Cart>('/api/cart');
      set({ cart, isLoading: false });
    } catch (err: unknown) {
      const msg = err instanceof Error ? err.message : 'No se pudo vaciar el carrito';
      set({ error: msg, isLoading: false });
    }
  },

  mergeWithUser: async () => {
    const token = getCartToken();
    if (!token) return;

    set({ isLoading: true });
    try {
      const cart = await apiClient.post<Cart>('/api/cart/merge', { guestToken: token });
      setCartToken(null);
      set({ cart, isLoading: false });
    } catch (err: unknown) {
      console.warn('Could not merge cart with user account', err);
      set({ isLoading: false });
    }
  },
}));
