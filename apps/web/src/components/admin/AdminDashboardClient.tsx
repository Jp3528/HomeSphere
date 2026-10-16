"use client";

import { useEffect, useState, useMemo } from "react";
import Link from "next/link";
import { useAuthStore } from "@/stores/auth-store";
import { analyticsApi } from "@/lib/analytics-api";
import { DashboardKpis, AuditLogItem } from "@/types/analytics";
import {
  DollarSign,
  ShoppingBag,
  TrendingUp,
  AlertTriangle,
  Compass,
  ShieldAlert,
  Loader2,
  Calendar,
  Layers,
  FileText,
  Clock,
  ArrowUpRight,
  Sparkles,
  RefreshCw,
  Package,
} from "lucide-react";

export function AdminDashboardClient() {
  const { isAuthenticated, hasRole } = useAuthStore();
  const [activeTab, setActiveTab] = useState<"kpis" | "audit">("kpis");
  const [periodPreset, setPeriodPreset] = useState<"7d" | "30d" | "all">("30d");

  const [kpis, setKpis] = useState<DashboardKpis | null>(null);
  const [auditLogs, setAuditLogs] = useState<AuditLogItem[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [isRefreshing, setIsRefreshing] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const canAccess = isAuthenticated && (hasRole("Admin") || hasRole("Manager"));

  const loadData = async (preset: "7d" | "30d" | "all" = periodPreset) => {
    try {
      setIsRefreshing(true);
      setError(null);

      let fromUtc: string | undefined;
      const now = new Date();

      if (preset === "7d") {
        fromUtc = new Date(now.getTime() - 7 * 24 * 60 * 60 * 1000).toISOString();
      } else if (preset === "30d") {
        fromUtc = new Date(now.getTime() - 30 * 24 * 60 * 60 * 1000).toISOString();
      }

      const [kpiData, auditData] = await Promise.all([
        analyticsApi.getDashboardKpis(fromUtc),
        analyticsApi.getAuditLogs(50),
      ]);

      setKpis(kpiData);
      setAuditLogs(auditData);
    } catch (err: unknown) {
      setError(err instanceof Error ? err.message : "Error al cargar las métricas comerciales.");
    } finally {
      setIsLoading(false);
      setIsRefreshing(false);
    }
  };

  useEffect(() => {
    if (canAccess) {
      loadData(periodPreset);
    } else {
      setIsLoading(false);
    }
  }, [canAccess, periodPreset]);

  // Max revenue for bar heights
  const maxDailyRevenue = useMemo(() => {
    if (!kpis?.revenueTrend || kpis.revenueTrend.length === 0) return 1;
    const max = Math.max(...kpis.revenueTrend.map((t) => t.revenue));
    return max > 0 ? max : 1;
  }, [kpis]);

  if (!canAccess && !isLoading) {
    return (
      <div className="min-h-screen bg-zinc-50 flex items-center justify-center p-4 dark:bg-zinc-950">
        <div className="max-w-md w-full rounded-3xl border border-zinc-200 bg-white p-8 text-center shadow-lg dark:border-zinc-800 dark:bg-zinc-900">
          <ShieldAlert className="h-12 w-12 text-rose-500 mx-auto" />
          <h2 className="mt-4 text-xl font-bold text-zinc-900 dark:text-zinc-100">
            Acceso Restringido
          </h2>
          <p className="mt-2 text-xs text-zinc-500 dark:text-zinc-400">
            Esta sección ejecutiva requiere permisos de Administrador o Gestor de Negocio.
          </p>
          <Link
            href="/login"
            className="mt-6 inline-flex rounded-xl bg-amber-700 px-5 py-2.5 text-xs font-semibold text-white hover:bg-amber-600 transition-colors"
          >
            Iniciar Sesión
          </Link>
        </div>
      </div>
    );
  }

  return (
    <div className="min-h-screen bg-zinc-50/50 py-8 dark:bg-zinc-950">
      <div className="mx-auto max-w-7xl px-4 sm:px-6 lg:px-8 space-y-8">
        {/* Header & Controls */}
        <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4 border-b border-zinc-200/80 pb-6 dark:border-zinc-800">
          <div>
            <div className="flex items-center gap-2 text-xs font-semibold text-amber-700 uppercase dark:text-amber-400">
              <Sparkles className="h-4 w-4" />
              <span>Analítica Comercial & Operaciones</span>
            </div>
            <h1 className="mt-1 text-2xl font-extrabold tracking-tight text-zinc-900 sm:text-3xl dark:text-zinc-50">
              Dashboard de Rendimiento HomeSphere
            </h1>
            <p className="mt-1 text-xs text-zinc-500 dark:text-zinc-400">
              Métricas consolidadas de facturación, pedidos, rotación de inventario y trazabilidad de operaciones.
            </p>
          </div>

          <div className="flex flex-wrap items-center gap-3">
            {/* Period Filters */}
            <div className="flex items-center rounded-2xl bg-white p-1 border border-zinc-200/80 shadow-xs dark:bg-zinc-900 dark:border-zinc-800">
              <button
                type="button"
                onClick={() => setPeriodPreset("7d")}
                className={`rounded-xl px-3 py-1.5 text-xs font-semibold transition-all ${
                  periodPreset === "7d"
                    ? "bg-zinc-900 text-white dark:bg-zinc-100 dark:text-zinc-900 shadow-xs"
                    : "text-zinc-500 hover:text-zinc-900 dark:hover:text-zinc-200"
                }`}
              >
                7 Días
              </button>
              <button
                type="button"
                onClick={() => setPeriodPreset("30d")}
                className={`rounded-xl px-3 py-1.5 text-xs font-semibold transition-all ${
                  periodPreset === "30d"
                    ? "bg-zinc-900 text-white dark:bg-zinc-100 dark:text-zinc-900 shadow-xs"
                    : "text-zinc-500 hover:text-zinc-900 dark:hover:text-zinc-200"
                }`}
              >
                30 Días
              </button>
              <button
                type="button"
                onClick={() => setPeriodPreset("all")}
                className={`rounded-xl px-3 py-1.5 text-xs font-semibold transition-all ${
                  periodPreset === "all"
                    ? "bg-zinc-900 text-white dark:bg-zinc-100 dark:text-zinc-900 shadow-xs"
                    : "text-zinc-500 hover:text-zinc-900 dark:hover:text-zinc-200"
                }`}
              >
                Histórico
              </button>
            </div>

            {/* Refresh Button */}
            <button
              type="button"
              onClick={() => loadData(periodPreset)}
              disabled={isRefreshing}
              className="flex h-9 w-9 items-center justify-center rounded-2xl border border-zinc-200/80 bg-white text-zinc-600 hover:bg-zinc-100 dark:border-zinc-800 dark:bg-zinc-900 dark:text-zinc-300"
              title="Refrescar datos"
            >
              <RefreshCw className={`h-4 w-4 ${isRefreshing ? "animate-spin text-amber-600" : ""}`} />
            </button>

            {/* Link to Inventory Management */}
            <Link
              href="/admin/inventory"
              className="inline-flex items-center gap-1.5 rounded-2xl bg-zinc-900 px-4 py-2 text-xs font-bold text-white shadow-2xs hover:bg-zinc-800 transition-colors dark:bg-zinc-100 dark:text-zinc-900 dark:hover:bg-zinc-200"
            >
              <Package className="h-4 w-4 text-amber-500" />
              <span>Inventario & Almacén</span>
            </Link>

            {/* Link to Scenes Editor */}
            <Link
              href="/admin/scenes"
              className="inline-flex items-center gap-1.5 rounded-2xl bg-amber-700 px-4 py-2 text-xs font-bold text-white shadow-xs hover:bg-amber-600 transition-colors"
            >
              <Compass className="h-4 w-4" />
              <span>Ambientes Interactivos</span>
            </Link>
          </div>
        </div>

        {/* Navigation Tabs */}
        <div className="flex items-center gap-3 border-b border-zinc-200/80 pb-2 dark:border-zinc-800">
          <button
            type="button"
            onClick={() => setActiveTab("kpis")}
            className={`flex items-center gap-2 pb-2 text-xs font-bold tracking-wide transition-colors border-b-2 ${
              activeTab === "kpis"
                ? "border-amber-700 text-amber-700 dark:border-amber-400 dark:text-amber-400"
                : "border-transparent text-zinc-500 hover:text-zinc-800 dark:hover:text-zinc-300"
            }`}
          >
            <TrendingUp className="h-4 w-4" />
            <span>Métricas Comerciales</span>
          </button>

          <button
            type="button"
            onClick={() => setActiveTab("audit")}
            className={`flex items-center gap-2 pb-2 text-xs font-bold tracking-wide transition-colors border-b-2 ${
              activeTab === "audit"
                ? "border-amber-700 text-amber-700 dark:border-amber-400 dark:text-amber-400"
                : "border-transparent text-zinc-500 hover:text-zinc-800 dark:hover:text-zinc-300"
            }`}
          >
            <FileText className="h-4 w-4" />
            <span>Registro de Auditoría (Audit Log)</span>
          </button>
        </div>

        {isLoading ? (
          <div className="flex h-64 items-center justify-center">
            <Loader2 className="h-8 w-8 animate-spin text-amber-600" />
          </div>
        ) : error ? (
          <div className="rounded-2xl border border-rose-200 bg-rose-50 p-6 text-center text-xs text-rose-700 dark:border-rose-900/50 dark:bg-rose-950/20 dark:text-rose-400">
            {error}
          </div>
        ) : activeTab === "kpis" && kpis ? (
          <div className="space-y-8">
            {/* KPI Cards Grid */}
            <div className="grid grid-cols-1 gap-5 sm:grid-cols-2 lg:grid-cols-4">
              {/* Total Revenue */}
              <div className="rounded-3xl border border-zinc-200/80 bg-white p-6 shadow-xs dark:border-zinc-800 dark:bg-zinc-900">
                <div className="flex items-center justify-between">
                  <span className="text-xs font-medium text-zinc-500 dark:text-zinc-400">
                    Ventas Reconocidas
                  </span>
                  <div className="flex h-9 w-9 items-center justify-center rounded-2xl bg-emerald-50 text-emerald-600 dark:bg-emerald-950/50 dark:text-emerald-400">
                    <DollarSign className="h-4 w-4" />
                  </div>
                </div>
                <div className="mt-3">
                  <span className="text-2xl font-black tracking-tight text-zinc-900 sm:text-3xl dark:text-zinc-50">
                    ${kpis.totalRevenue.toLocaleString("en-US", { minimumFractionDigits: 2, maximumFractionDigits: 2 })}
                  </span>
                  <span className="text-xs text-zinc-400 ml-1">USD</span>
                </div>
                <p className="mt-2 text-[11px] text-zinc-500 dark:text-zinc-400">
                  {kpis.paidOrdersCount} pedidos pagados o entregados
                </p>
              </div>

              {/* Total Orders */}
              <div className="rounded-3xl border border-zinc-200/80 bg-white p-6 shadow-xs dark:border-zinc-800 dark:bg-zinc-900">
                <div className="flex items-center justify-between">
                  <span className="text-xs font-medium text-zinc-500 dark:text-zinc-400">
                    Total de Pedidos
                  </span>
                  <div className="flex h-9 w-9 items-center justify-center rounded-2xl bg-amber-50 text-amber-700 dark:bg-amber-950/50 dark:text-amber-400">
                    <ShoppingBag className="h-4 w-4" />
                  </div>
                </div>
                <div className="mt-3">
                  <span className="text-2xl font-black tracking-tight text-zinc-900 sm:text-3xl dark:text-zinc-50">
                    {kpis.totalOrders}
                  </span>
                </div>
                <p className="mt-2 text-[11px] text-zinc-500 dark:text-zinc-400">
                  {kpis.pendingOrdersCount} pendientes de pago
                </p>
              </div>

              {/* Average Ticket */}
              <div className="rounded-3xl border border-zinc-200/80 bg-white p-6 shadow-xs dark:border-zinc-800 dark:bg-zinc-900">
                <div className="flex items-center justify-between">
                  <span className="text-xs font-medium text-zinc-500 dark:text-zinc-400">
                    Ticket Promedio (AOV)
                  </span>
                  <div className="flex h-9 w-9 items-center justify-center rounded-2xl bg-blue-50 text-blue-600 dark:bg-blue-950/50 dark:text-blue-400">
                    <TrendingUp className="h-4 w-4" />
                  </div>
                </div>
                <div className="mt-3">
                  <span className="text-2xl font-black tracking-tight text-zinc-900 sm:text-3xl dark:text-zinc-50">
                    ${kpis.averageTicket.toLocaleString("en-US", { minimumFractionDigits: 2, maximumFractionDigits: 2 })}
                  </span>
                  <span className="text-xs text-zinc-400 ml-1">USD</span>
                </div>
                <p className="mt-2 text-[11px] text-zinc-500 dark:text-zinc-400">
                  Promedio por pedido confirmado
                </p>
              </div>

              {/* Low Stock Alerts */}
              <div className="rounded-3xl border border-zinc-200/80 bg-white p-6 shadow-xs dark:border-zinc-800 dark:bg-zinc-900">
                <div className="flex items-center justify-between">
                  <span className="text-xs font-medium text-zinc-500 dark:text-zinc-400">
                    Stock Crítico (≤ 5)
                  </span>
                  <div className="flex h-9 w-9 items-center justify-center rounded-2xl bg-rose-50 text-rose-600 dark:bg-rose-950/50 dark:text-rose-400">
                    <AlertTriangle className="h-4 w-4" />
                  </div>
                </div>
                <div className="mt-3">
                  <span className="text-2xl font-black tracking-tight text-zinc-900 sm:text-3xl dark:text-zinc-50">
                    {kpis.lowStockCount}
                  </span>
                  <span className="text-xs text-zinc-400 ml-1">variantes</span>
                </div>
                <p className="mt-2 text-[11px] text-zinc-500 dark:text-zinc-400">
                  Requiere reabastecimiento urgente
                </p>
              </div>
            </div>

            {/* Daily Revenue Chart & Status Breakdown */}
            <div className="grid grid-cols-1 gap-8 lg:grid-cols-12">
              {/* Daily Revenue Chart (8 cols) */}
              <div className="lg:col-span-8 rounded-3xl border border-zinc-200/80 bg-white p-6 shadow-xs dark:border-zinc-800 dark:bg-zinc-900 flex flex-col">
                <div className="flex items-center justify-between border-b border-zinc-100 pb-4 dark:border-zinc-800">
                  <div>
                    <h3 className="text-base font-bold text-zinc-900 dark:text-zinc-50">
                      Evolución de Ingresos Diarios
                    </h3>
                    <p className="text-xs text-zinc-500 dark:text-zinc-400">
                      Ventas netas reconocidas por fecha en el periodo seleccionado
                    </p>
                  </div>
                  <Calendar className="h-4 w-4 text-zinc-400" />
                </div>

                {/* SVG/Bar Chart */}
                <div className="mt-6 flex-1 flex flex-col justify-end">
                  <div className="flex items-end gap-1.5 h-48 w-full pt-6">
                    {kpis.revenueTrend.map((day) => {
                      const heightPercent = Math.max(4, (day.revenue / maxDailyRevenue) * 100);
                      const formattedRev = `$${day.revenue.toFixed(0)}`;

                      return (
                        <div
                          key={day.date}
                          className="group relative flex-1 flex flex-col items-center h-full justify-end"
                        >
                          {/* Tooltip on hover */}
                          <div className="pointer-events-none absolute bottom-full mb-2 hidden group-hover:flex flex-col items-center rounded-xl bg-zinc-900 px-2.5 py-1.5 text-[10px] font-semibold text-white shadow-xl z-20 whitespace-nowrap">
                            <span>{day.date}</span>
                            <span className="text-amber-400">${day.revenue.toFixed(2)} USD</span>
                            <span className="text-zinc-400">{day.ordersCount} pedidos</span>
                          </div>

                          {/* Bar */}
                          <div
                            style={{ height: `${heightPercent}%` }}
                            className={`w-full rounded-t-md transition-all ${
                              day.revenue > 0
                                ? "bg-amber-700 group-hover:bg-amber-600"
                                : "bg-zinc-100 dark:bg-zinc-800"
                            }`}
                          />
                        </div>
                      );
                    })}
                  </div>

                  {/* Date labels (sample) */}
                  <div className="mt-2 flex justify-between text-[10px] text-zinc-400 px-1 border-t border-zinc-100 pt-2 dark:border-zinc-800">
                    <span>{kpis.revenueTrend[0]?.date || ""}</span>
                    <span>
                      {kpis.revenueTrend[Math.floor(kpis.revenueTrend.length / 2)]?.date || ""}
                    </span>
                    <span>{kpis.revenueTrend[kpis.revenueTrend.length - 1]?.date || ""}</span>
                  </div>
                </div>
              </div>

              {/* Orders by Status (4 cols) */}
              <div className="lg:col-span-4 rounded-3xl border border-zinc-200/80 bg-white p-6 shadow-xs dark:border-zinc-800 dark:bg-zinc-900">
                <div className="flex items-center justify-between border-b border-zinc-100 pb-4 dark:border-zinc-800">
                  <div>
                    <h3 className="text-base font-bold text-zinc-900 dark:text-zinc-50">
                      Pedidos por Estado
                    </h3>
                    <p className="text-xs text-zinc-500 dark:text-zinc-400">
                      Distribución porcentual operativa
                    </p>
                  </div>
                  <Layers className="h-4 w-4 text-zinc-400" />
                </div>

                <div className="mt-6 space-y-4">
                  {kpis.ordersByStatus.map((status) => {
                    const statusColor =
                      status.status === "Delivered"
                        ? "bg-emerald-500 text-emerald-700"
                        : status.status === "Paid"
                        ? "bg-teal-500 text-teal-700"
                        : status.status === "Processing"
                        ? "bg-blue-500 text-blue-700"
                        : status.status === "PendingPayment"
                        ? "bg-amber-500 text-amber-700"
                        : "bg-rose-500 text-rose-700";

                    return (
                      <div key={status.status} className="space-y-1.5">
                        <div className="flex items-center justify-between text-xs">
                          <span className="font-semibold text-zinc-800 dark:text-zinc-200">
                            {status.status}
                          </span>
                          <span className="text-zinc-500 font-medium">
                            {status.count} ({status.percentage}%)
                          </span>
                        </div>
                        <div className="h-2 w-full overflow-hidden rounded-full bg-zinc-100 dark:bg-zinc-800">
                          <div
                            style={{ width: `${status.percentage}%` }}
                            className={`h-full rounded-full ${statusColor}`}
                          />
                        </div>
                      </div>
                    );
                  })}
                </div>
              </div>
            </div>

            {/* Bottom Row: Top Products & Low Stock Alerts */}
            <div className="grid grid-cols-1 gap-8 lg:grid-cols-2">
              {/* Top Selling Products */}
              <div className="rounded-3xl border border-zinc-200/80 bg-white p-6 shadow-xs dark:border-zinc-800 dark:bg-zinc-900">
                <h3 className="text-base font-bold text-zinc-900 dark:text-zinc-50 border-b border-zinc-100 pb-4 dark:border-zinc-800">
                  Productos Más Vendidos
                </h3>

                {kpis.topProducts.length === 0 ? (
                  <p className="mt-6 text-center text-xs text-zinc-400">
                    Aún no hay ventas registradas en el periodo.
                  </p>
                ) : (
                  <div className="mt-4 divide-y divide-zinc-100 dark:divide-zinc-800">
                    {kpis.topProducts.map((p, idx) => (
                      <div key={p.productId} className="flex items-center justify-between py-3">
                        <div className="flex items-center gap-3">
                          <div className="flex h-7 w-7 items-center justify-center rounded-full bg-amber-50 text-xs font-bold text-amber-800 dark:bg-amber-950 dark:text-amber-400">
                            {idx + 1}
                          </div>
                          <div>
                            <span className="text-xs font-semibold text-zinc-900 line-clamp-1 dark:text-zinc-100">
                              {p.productName}
                            </span>
                            <span className="text-[10px] text-zinc-400">SKU: {p.sku}</span>
                          </div>
                        </div>

                        <div className="text-right">
                          <span className="text-xs font-extrabold text-zinc-900 dark:text-zinc-100 block">
                            ${p.revenueGenerated.toFixed(2)} USD
                          </span>
                          <span className="text-[11px] text-zinc-500 dark:text-zinc-400">
                            {p.unitsSold} {p.unitsSold === 1 ? "unidad" : "unidades"}
                          </span>
                        </div>
                      </div>
                    ))}
                  </div>
                )}
              </div>

              {/* Low Stock Alerts */}
              <div className="rounded-3xl border border-zinc-200/80 bg-white p-6 shadow-xs dark:border-zinc-800 dark:bg-zinc-900">
                <h3 className="text-base font-bold text-zinc-900 dark:text-zinc-50 border-b border-zinc-100 pb-4 dark:border-zinc-800">
                  Alertas de Inventario Bajo (≤ 5 unidades)
                </h3>

                {kpis.lowStockAlerts.length === 0 ? (
                  <p className="mt-6 text-center text-xs text-emerald-600 dark:text-emerald-400">
                    Todos los niveles de inventario se encuentran estables.
                  </p>
                ) : (
                  <div className="mt-4 divide-y divide-zinc-100 dark:divide-zinc-800">
                    {kpis.lowStockAlerts.map((v) => (
                      <div key={v.variantId} className="flex items-center justify-between py-3">
                        <div>
                          <Link
                            href={`/products/${v.productSlug}`}
                            className="text-xs font-semibold text-zinc-900 hover:text-amber-700 transition-colors dark:text-zinc-100 line-clamp-1"
                          >
                            {v.productName} · {v.variantName}
                          </Link>
                          <span className="text-[10px] text-zinc-400">SKU: {v.sku}</span>
                        </div>

                        <div className="flex items-center gap-2">
                          <span className="rounded-full bg-rose-50 px-2.5 py-1 text-xs font-bold text-rose-700 dark:bg-rose-950 dark:text-rose-400">
                            {v.stockQuantity} {v.stockQuantity === 1 ? "unidad" : "unidades"}
                          </span>
                        </div>
                      </div>
                    ))}
                  </div>
                )}
              </div>
            </div>
          </div>
        ) : activeTab === "audit" ? (
          /* Audit Log Table */
          <div className="rounded-3xl border border-zinc-200/80 bg-white p-6 shadow-xs dark:border-zinc-800 dark:bg-zinc-900">
            <div className="flex items-center justify-between border-b border-zinc-100 pb-4 dark:border-zinc-800">
              <div>
                <h3 className="text-base font-bold text-zinc-900 dark:text-zinc-50">
                  Registro de Auditoría de Operaciones
                </h3>
                <p className="text-xs text-zinc-500 dark:text-zinc-400">
                  Eventos administrativos inmutables con sanitización de secretos y trazabilidad de actor
                </p>
              </div>
              <Clock className="h-4 w-4 text-zinc-400" />
            </div>

            {auditLogs.length === 0 ? (
              <p className="mt-6 text-center text-xs text-zinc-400">
                No hay eventos de auditoría registrados recientemente.
              </p>
            ) : (
              <div className="mt-4 overflow-x-auto">
                <table className="w-full text-left text-xs text-zinc-600 dark:text-zinc-300">
                  <thead className="bg-zinc-50 text-[11px] font-semibold text-zinc-500 uppercase dark:bg-zinc-800/50 dark:text-zinc-400">
                    <tr>
                      <th className="px-4 py-3">Fecha (UTC)</th>
                      <th className="px-4 py-3">Acción</th>
                      <th className="px-4 py-3">Entidad</th>
                      <th className="px-4 py-3">Actor</th>
                      <th className="px-4 py-3">Detalles</th>
                    </tr>
                  </thead>
                  <tbody className="divide-y divide-zinc-100 dark:divide-zinc-800">
                    {auditLogs.map((log) => (
                      <tr key={log.id} className="hover:bg-zinc-50/50 dark:hover:bg-zinc-800/30">
                        <td className="px-4 py-3 whitespace-nowrap text-[11px]">
                          {new Date(log.createdAtUtc).toLocaleString("es-PE")}
                        </td>
                        <td className="px-4 py-3 font-bold text-zinc-900 dark:text-zinc-100">
                          <span className="rounded-md bg-zinc-100 px-2 py-0.5 text-[10px] font-mono dark:bg-zinc-800">
                            {log.action}
                          </span>
                        </td>
                        <td className="px-4 py-3 font-medium">
                          {log.entityName} #{log.entityId.slice(0, 8)}
                        </td>
                        <td className="px-4 py-3 text-[11px]">
                          {log.actorEmail || log.actorId || "Sistema"}
                        </td>
                        <td className="px-4 py-3 max-w-xs font-mono text-[10px] text-zinc-500 dark:text-zinc-400 truncate" title={log.detailsJson || ""}>
                          {log.detailsJson || "-"}
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            )}
          </div>
        ) : null}
      </div>
    </div>
  );
}
