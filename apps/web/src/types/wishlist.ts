export interface WishlistItem {
  id: string;
  productId: string;
  productName: string;
  productSlug: string;
  basePrice: number;
  primaryImageUrl: string | null;
  categoryName: string;
  isAvailable: boolean;
  createdAtUtc: string;
}
