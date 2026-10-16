export interface CartItem {
  id: string;
  productVariantId: string;
  sku: string;
  productName: string;
  productSlug: string;
  variantName: string;
  unitPrice: number;
  quantity: number;
  lineTotal: number;
  primaryImageUrl?: string | null;
  attributesJson?: string | null;
  availableStock: number;
  isAvailable: boolean;
  warning?: string | null;
}

export interface Cart {
  id: string;
  userId?: string | null;
  guestToken?: string | null;
  items: CartItem[];
  totalItems: number;
  subtotal: number;
  createdAtUtc: string;
  updatedAtUtc?: string | null;
}

export interface AddToCartPayload {
  variantId: string;
  quantity: number;
}

export interface UpdateCartItemPayload {
  quantity: number;
}
