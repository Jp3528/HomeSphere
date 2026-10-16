export interface Category {
  id: string;
  name: string;
  slug: string;
  description?: string;
  imageUrl?: string;
  parentId?: string | null;
  displayOrder: number;
  isActive: boolean;
  subCategories?: Category[];
  productCount: number;
}

export interface Brand {
  id: string;
  name: string;
  slug: string;
  description?: string;
  logoUrl?: string;
  isActive: boolean;
  productCount: number;
}

export interface ProductVariant {
  id: string;
  productId: string;
  sku: string;
  name: string;
  price: number;
  stockQuantity: number;
  barcode?: string;
  isActive: boolean;
  attributesJson?: string;
}

export interface ProductImage {
  id: string;
  productId: string;
  url: string;
  altText?: string;
  displayOrder: number;
  isPrimary: boolean;
}

export interface ProductListItem {
  id: string;
  name: string;
  slug: string;
  shortDescription: string;
  basePrice: number;
  status: string;
  isFeatured: boolean;
  categoryId: string;
  categoryName: string;
  categorySlug: string;
  brandId: string;
  brandName: string;
  brandSlug: string;
  primaryImageUrl?: string;
  variantCount: number;
  totalStock: number;
  createdAtUtc: string;
}

export interface ProductDetail {
  id: string;
  name: string;
  slug: string;
  shortDescription: string;
  description: string;
  basePrice: number;
  status: string;
  isFeatured: boolean;
  categoryId: string;
  categoryName: string;
  categorySlug: string;
  brandId: string;
  brandName: string;
  brandSlug: string;
  variants: ProductVariant[];
  images: ProductImage[];
  createdAtUtc: string;
  updatedAtUtc?: string;
}

export interface FilterItem {
  slug: string;
  name: string;
  count: number;
}

export interface CatalogFilters {
  minPrice: number;
  maxPrice: number;
  categories: FilterItem[];
  brands: FilterItem[];
}

export interface PagedResult<T> {
  items: T[];
  pageNumber: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
  hasPreviousPage: boolean;
  hasNextPage: boolean;
}
