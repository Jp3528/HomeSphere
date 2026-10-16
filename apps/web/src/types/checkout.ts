export interface CheckoutItemPreview {
  variantId: string;
  sku: string;
  productName: string;
  productSlug: string;
  variantName: string;
  unitPrice: number;
  quantity: number;
  lineTotal: number;
  availableStock: number;
  isAvailable: boolean;
  warningMessage?: string | null;
  errorMessage?: string | null;
}

export interface CouponSummary {
  code: string;
  description: string;
  discountType: string;
  discountValue: number;
  discountAmount: number;
}

export interface ShippingSummary {
  code: string;
  name: string;
  cost: number;
  estimatedDelivery: string;
  isFree: boolean;
}

export interface CheckoutPreview {
  cartId: string;
  items: CheckoutItemPreview[];
  subtotal: number;
  discountTotal: number;
  appliedCoupon?: CouponSummary | null;
  shipping: ShippingSummary;
  total: number;
  canProceedToOrder: boolean;
  validationErrors: string[];
}

export interface CheckoutPreviewRequest {
  addressId?: string | null;
  shippingMethod: string;
  couponCode?: string | null;
}

export interface ValidateCouponResponse {
  isValid: boolean;
  message?: string | null;
  coupon?: CouponSummary | null;
}
