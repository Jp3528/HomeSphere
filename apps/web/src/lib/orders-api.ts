import { apiClient } from './api-client';

export interface OrderItemDto {
  id: string;
  productVariantId: string;
  skuSnapshot: string;
  productNameSnapshot: string;
  unitPrice: number;
  quantity: number;
  totalPrice: number;
  attributesJsonSnapshot?: string | null;
  imageUrlSnapshot?: string | null;
}

export interface OrderStatusHistoryDto {
  previousStatus: string;
  newStatus: string;
  changedBy: string;
  reason?: string | null;
  createdAtUtc: string;
}

export interface OrderDto {
  id: string;
  orderNumber: string;
  publicId: string;
  userId?: string | null;
  guestEmail?: string | null;
  status: 'PendingPayment' | 'Paid' | 'Processing' | 'Shipped' | 'Delivered' | 'Cancelled' | 'Refunded';
  subtotal: number;
  discountTotal: number;
  shippingCost: number;
  total: number;
  couponCode?: string | null;
  shippingMethod: string;
  shippingAddressSnapshotJson: string;
  customerNotes?: string | null;
  paidAtUtc?: string | null;
  shippedAtUtc?: string | null;
  deliveredAtUtc?: string | null;
  cancelledAtUtc?: string | null;
  cancellationReason?: string | null;
  createdAtUtc: string;
  items: OrderItemDto[];
  statusHistory: OrderStatusHistoryDto[];
}

export interface OrderSummaryDto {
  id: string;
  orderNumber: string;
  publicId: string;
  status: string;
  total: number;
  itemsCount: number;
  createdAtUtc: string;
}

export interface CreateOrderRequest {
  shippingAddressId?: string | null;
  guestShippingAddress?: {
    label: string;
    recipientName: string;
    phoneNumber: string;
    line1: string;
    line2?: string;
    district: string;
    city: string;
    region: string;
    reference?: string;
    isDefault?: boolean;
  } | null;
  shippingMethod?: string;
  couponCode?: string | null;
  customerNotes?: string | null;
  guestEmail?: string | null;
}

export interface PaymentInitiationResult {
  success: boolean;
  transactionId: string;
  paymentUrl?: string | null;
  clientSecret?: string | null;
  errorMessage?: string | null;
}

export const ordersApi = {
  createOrder: (request: CreateOrderRequest, idempotencyKey?: string) => {
    const headers: Record<string, string> = {};
    if (idempotencyKey) {
      headers['Idempotency-Key'] = idempotencyKey;
    }
    return apiClient.post<OrderDto>('/orders', request, { headers });
  },

  getOrderByPublicId: (publicId: string) => {
    return apiClient.get<OrderDto>(`/orders/${publicId}`);
  },

  getMyOrders: () => {
    return apiClient.get<OrderSummaryDto[]>('/orders/my-orders');
  },

  cancelOrder: (orderId: string, reason?: string) => {
    return apiClient.post<OrderDto>(`/orders/${orderId}/cancel`, { reason });
  },

  payOrder: (orderId: string, returnUrl?: string, cancelUrl?: string) => {
    const params = new URLSearchParams();
    if (returnUrl) params.set('returnUrl', returnUrl);
    if (cancelUrl) params.set('cancelUrl', cancelUrl);
    const qs = params.toString() ? `?${params.toString()}` : '';
    return apiClient.post<PaymentInitiationResult>(`/orders/${orderId}/pay${qs}`);
  },

  simulatePaymentWebhook: async (orderId: string, orderNumber: string, amount: number) => {
    // Calls payment webhook through a developer-friendly endpoint or direct simulation
    const payload = JSON.stringify({
      eventType: 'payment.succeeded',
      transactionId: `mock_manual_${Date.now()}`,
      orderId,
      orderNumber,
      amount,
      status: 'succeeded',
    });

    // We can also trigger state update or webhook directly
    return apiClient.post<{ status: string; processed: boolean }>('/webhooks/payments', JSON.parse(payload), {
      headers: {
        'Content-Type': 'application/json',
        'X-Signature': 'mock-dev-signature', // Handled by server or direct state update
      },
    });
  },
};
