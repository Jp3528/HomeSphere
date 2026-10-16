export interface RevenueTrend {
  date: string;
  revenue: number;
  ordersCount: number;
}

export interface OrdersByStatus {
  status: string;
  count: number;
  percentage: number;
}

export interface TopSellingProduct {
  productId: string;
  productName: string;
  sku: string;
  unitsSold: number;
  revenueGenerated: number;
  primaryImageUrl?: string;
}

export interface LowStockAlert {
  productId: string;
  productName: string;
  productSlug: string;
  variantId: string;
  variantName: string;
  sku: string;
  stockQuantity: number;
  price: number;
}

export interface DashboardKpis {
  totalRevenue: number;
  totalOrders: number;
  averageTicket: number;
  paidOrdersCount: number;
  pendingOrdersCount: number;
  completedOrdersCount: number;
  cancelledOrdersCount: number;
  lowStockCount: number;
  totalActiveProducts: number;
  totalActiveScenes: number;
  revenueTrend: RevenueTrend[];
  ordersByStatus: OrdersByStatus[];
  topProducts: TopSellingProduct[];
  lowStockAlerts: LowStockAlert[];
}

export interface AuditLogItem {
  id: string;
  action: string;
  entityName: string;
  entityId: string;
  actorId?: string;
  actorEmail?: string;
  actorIpAddress?: string;
  correlationId?: string;
  detailsJson?: string;
  createdAtUtc: string;
}
