import { apiClient } from './api-client';
import { DashboardKpis, AuditLogItem } from '@/types/analytics';

export const analyticsApi = {
  getDashboardKpis: async (
    fromUtc?: string,
    toUtc?: string
  ): Promise<DashboardKpis> => {
    return apiClient.get<DashboardKpis>('/api/admin/analytics/dashboard', {
      params: { fromUtc, toUtc },
    });
  },

  getAuditLogs: async (
    limit: number = 50,
    entityName?: string
  ): Promise<AuditLogItem[]> => {
    return apiClient.get<AuditLogItem[]>('/api/admin/audit-logs', {
      params: { limit, entityName },
    });
  },
};
