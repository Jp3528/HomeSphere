'use client';

import React, { useEffect, useState } from 'react';
import Link from 'next/link';
import { inventoryApi } from '@/lib/inventory-api';
import { InventoryItem, InventoryMovement } from '@/types/inventory';
import {
  Package,
  History,
  AlertTriangle,
  Search,
  PlusCircle,
  ArrowLeft,
  CheckCircle2,
  TrendingUp,
  TrendingDown,
  RefreshCw,
} from 'lucide-react';

export default function AdminInventoryPage() {
  const [activeTab, setActiveTab] = useState<'items' | 'movements'>('items');
  const [items, setItems] = useState<InventoryItem[]>([]);
  const [movements, setMovements] = useState<InventoryMovement[]>([]);
  const [loading, setLoading] = useState(true);
  const [search, setSearch] = useState('');
  const [lowStockOnly, setLowStockOnly] = useState(false);

  // Adjustment Modal State
  const [selectedVariant, setSelectedVariant] = useState<InventoryItem | null>(null);
  const [delta, setDelta] = useState<number>(1);
  const [movementType, setMovementType] = useState<number>(1); // 1 = Receipt, 2 = Adjustment
  const [reason, setReason] = useState('');
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [adjustError, setAdjustError] = useState<string | null>(null);
  const [successMessage, setSuccessMessage] = useState<string | null>(null);

  const loadInventory = async () => {
    try {
      setLoading(true);
      const data = await inventoryApi.getInventory(search || undefined, lowStockOnly || undefined);
      setItems(data);
    } catch (err: unknown) {
      console.error('Error loading inventory:', err);
    } finally {
      setLoading(false);
    }
  };

  const loadMovements = async () => {
    try {
      setLoading(true);
      const data = await inventoryApi.getMovements(undefined, 100);
      setMovements(data);
    } catch (err: unknown) {
      console.error('Error loading movements:', err);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    if (activeTab === 'items') {
      loadInventory();
    } else {
      loadMovements();
    }
  }, [activeTab, lowStockOnly]);

  const handleSearchSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    loadInventory();
  };

  const handleOpenAdjustModal = (item: InventoryItem) => {
    setSelectedVariant(item);
    setDelta(1);
    setMovementType(1);
    setReason('');
    setAdjustError(null);
  };

  const handleAdjustSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!selectedVariant) return;

    if (!reason.trim()) {
      setAdjustError('El motivo del ajuste es obligatorio.');
      return;
    }

    setIsSubmitting(true);
    setAdjustError(null);

    try {
      await inventoryApi.adjustStock({
        variantId: selectedVariant.variantId,
        quantityDelta: delta,
        type: movementType,
        reason: reason.trim(),
        referenceType: 'ManualAdjustment',
      });

      setSuccessMessage(`Stock de ${selectedVariant.sku} actualizado exitosamente.`);
      setSelectedVariant(null);
      await loadInventory();
      setTimeout(() => setSuccessMessage(null), 4000);
    } catch (err: unknown) {
      const msg = err instanceof Error ? err.message : 'Error al actualizar el stock';
      setAdjustError(msg);
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <div className="mx-auto max-w-7xl px-4 py-8 sm:px-6 lg:px-8">
      {/* Header */}
      <div className="mb-6 flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
        <div>
          <Link
            href="/admin"
            className="inline-flex items-center gap-2 text-xs font-medium text-zinc-500 hover:text-amber-600 transition-colors"
          >
            <ArrowLeft className="h-3.5 w-3.5" />
            Volver al Panel Ejecutivo
          </Link>
          <div className="mt-2 flex items-center gap-3">
            <h1 className="text-2xl font-bold tracking-tight text-zinc-900 sm:text-3xl dark:text-zinc-100">
              Control de Inventario y Almacén
            </h1>
            <span className="rounded-full bg-amber-100 px-3 py-1 text-xs font-semibold text-amber-800 dark:bg-amber-950/60 dark:text-amber-400">
              {items.length} variantes
            </span>
          </div>
        </div>

        <div className="flex items-center gap-3">
          <button
            onClick={() => (activeTab === 'items' ? loadInventory() : loadMovements())}
            className="inline-flex items-center gap-1.5 rounded-xl border border-zinc-200 bg-white px-3.5 py-2 text-xs font-semibold text-zinc-700 shadow-2xs hover:bg-zinc-50 transition-colors dark:border-zinc-800 dark:bg-zinc-900 dark:text-zinc-300"
          >
            <RefreshCw className="h-3.5 w-3.5" />
            Refrescar
          </button>
        </div>
      </div>

      {successMessage && (
        <div className="mb-6 flex items-center gap-2 rounded-xl border border-emerald-200 bg-emerald-50 p-4 text-sm font-medium text-emerald-800 dark:border-emerald-800 dark:bg-emerald-950/50 dark:text-emerald-300">
          <CheckCircle2 className="h-5 w-5 text-emerald-600" />
          {successMessage}
        </div>
      )}

      {/* Tabs */}
      <div className="mb-6 flex border-b border-zinc-200 dark:border-zinc-800">
        <button
          onClick={() => setActiveTab('items')}
          className={`flex items-center gap-2 border-b-2 px-4 py-3 text-sm font-semibold transition-colors ${
            activeTab === 'items'
              ? 'border-amber-600 text-amber-600 dark:border-amber-400 dark:text-amber-400'
              : 'border-transparent text-zinc-500 hover:text-zinc-700 dark:text-zinc-400'
          }`}
        >
          <Package className="h-4 w-4" />
          Estado del Inventario
        </button>

        <button
          onClick={() => setActiveTab('movements')}
          className={`flex items-center gap-2 border-b-2 px-4 py-3 text-sm font-semibold transition-colors ${
            activeTab === 'movements'
              ? 'border-amber-600 text-amber-600 dark:border-amber-400 dark:text-amber-400'
              : 'border-transparent text-zinc-500 hover:text-zinc-700 dark:text-zinc-400'
          }`}
        >
          <History className="h-4 w-4" />
          Historial de Movimientos
        </button>
      </div>

      {/* Tab: Items */}
      {activeTab === 'items' && (
        <div className="space-y-4">
          {/* Toolbar */}
          <div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between rounded-2xl border border-zinc-200 bg-white p-4 shadow-2xs dark:border-zinc-800 dark:bg-zinc-900">
            <form onSubmit={handleSearchSubmit} className="relative flex-1 max-w-md">
              <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-zinc-400" />
              <input
                type="text"
                placeholder="Buscar por SKU o nombre de producto..."
                value={search}
                onChange={(e) => setSearch(e.target.value)}
                className="w-full rounded-xl border border-zinc-300 bg-zinc-50 py-2 pl-9 pr-4 text-xs text-zinc-900 placeholder:text-zinc-400 focus:border-amber-600 focus:bg-white focus:outline-hidden dark:border-zinc-700 dark:bg-zinc-800 dark:text-white"
              />
            </form>

            <label className="flex items-center gap-2 text-xs font-semibold text-zinc-700 dark:text-zinc-300 cursor-pointer">
              <input
                type="checkbox"
                checked={lowStockOnly}
                onChange={(e) => setLowStockOnly(e.target.checked)}
                className="rounded border-zinc-300 text-amber-600 focus:ring-amber-500"
              />
              <span className="flex items-center gap-1.5 text-amber-600 dark:text-amber-400">
                <AlertTriangle className="h-3.5 w-3.5" />
                Solo stock bajo (≤ 5 unidades)
              </span>
            </label>
          </div>

          {/* Table */}
          <div className="overflow-hidden rounded-2xl border border-zinc-200 bg-white shadow-2xs dark:border-zinc-800 dark:bg-zinc-900">
            {loading ? (
              <div className="flex h-64 items-center justify-center">
                <div className="h-8 w-8 animate-spin rounded-full border-2 border-amber-600 border-t-transparent" />
              </div>
            ) : items.length === 0 ? (
              <div className="p-12 text-center text-sm text-zinc-500">
                No se encontraron variantes con los filtros especificados.
              </div>
            ) : (
              <table className="w-full text-left text-xs">
                <thead className="border-b border-zinc-100 bg-zinc-50/70 font-semibold text-zinc-500 uppercase tracking-wider dark:border-zinc-800 dark:bg-zinc-800/40">
                  <tr>
                    <th className="px-5 py-3.5">SKU / Variante</th>
                    <th className="px-5 py-3.5">Producto</th>
                    <th className="px-5 py-3.5">Categoría</th>
                    <th className="px-5 py-3.5 text-right">Precio</th>
                    <th className="px-5 py-3.5 text-center">Stock Actual</th>
                    <th className="px-5 py-3.5 text-center">Estado</th>
                    <th className="px-5 py-3.5 text-right">Acción</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-zinc-100 dark:divide-zinc-800/60 font-medium text-zinc-700 dark:text-zinc-300">
                  {items.map((item) => (
                    <tr key={item.variantId} className="hover:bg-zinc-50/50 dark:hover:bg-zinc-800/30 transition-colors">
                      <td className="px-5 py-4">
                        <span className="font-mono font-bold text-zinc-900 dark:text-zinc-100">{item.sku}</span>
                        <div className="text-[11px] text-zinc-500">{item.variantName}</div>
                      </td>
                      <td className="px-5 py-4 font-semibold text-zinc-900 dark:text-zinc-100">{item.productName}</td>
                      <td className="px-5 py-4 text-zinc-500">{item.categoryName}</td>
                      <td className="px-5 py-4 text-right font-semibold text-zinc-900 dark:text-white">
                        ${item.price.toFixed(2)}
                      </td>
                      <td className="px-5 py-4 text-center">
                        <span
                          className={`inline-flex items-center gap-1 rounded-full px-2.5 py-0.5 text-xs font-bold ${
                            item.stockQuantity <= 0
                              ? 'bg-rose-100 text-rose-800 dark:bg-rose-950/60 dark:text-rose-400'
                              : item.isLowStock
                              ? 'bg-amber-100 text-amber-800 dark:bg-amber-950/60 dark:text-amber-400'
                              : 'bg-emerald-100 text-emerald-800 dark:bg-emerald-950/60 dark:text-emerald-400'
                          }`}
                        >
                          {item.stockQuantity} u.
                        </span>
                      </td>
                      <td className="px-5 py-4 text-center">
                        <span
                          className={`inline-block h-2 w-2 rounded-full ${
                            item.isActive ? 'bg-emerald-500' : 'bg-zinc-400'
                          }`}
                          title={item.isActive ? 'Activo' : 'Inactivo'}
                        />
                      </td>
                      <td className="px-5 py-4 text-right">
                        <button
                          onClick={() => handleOpenAdjustModal(item)}
                          className="inline-flex items-center gap-1 rounded-lg bg-zinc-900 px-3 py-1.5 text-[11px] font-semibold text-white shadow-2xs hover:bg-amber-600 transition-colors dark:bg-zinc-100 dark:text-zinc-900 dark:hover:bg-amber-500"
                        >
                          <PlusCircle className="h-3 w-3" />
                          Ajustar
                        </button>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            )}
          </div>
        </div>
      )}

      {/* Tab: Movements */}
      {activeTab === 'movements' && (
        <div className="overflow-hidden rounded-2xl border border-zinc-200 bg-white shadow-2xs dark:border-zinc-800 dark:bg-zinc-900">
          {loading ? (
            <div className="flex h-64 items-center justify-center">
              <div className="h-8 w-8 animate-spin rounded-full border-2 border-amber-600 border-t-transparent" />
            </div>
          ) : movements.length === 0 ? (
            <div className="p-12 text-center text-sm text-zinc-500">
              No hay movimientos de stock registrados aún.
            </div>
          ) : (
            <table className="w-full text-left text-xs">
              <thead className="border-b border-zinc-100 bg-zinc-50/70 font-semibold text-zinc-500 uppercase tracking-wider dark:border-zinc-800 dark:bg-zinc-800/40">
                <tr>
                  <th className="px-5 py-3.5">Fecha</th>
                  <th className="px-5 py-3.5">SKU / Producto</th>
                  <th className="px-5 py-3.5">Tipo</th>
                  <th className="px-5 py-3.5 text-center">Variación</th>
                  <th className="px-5 py-3.5 text-center">Stock (Prev → Nuevo)</th>
                  <th className="px-5 py-3.5">Motivo</th>
                  <th className="px-5 py-3.5">Responsable</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-zinc-100 dark:divide-zinc-800/60 font-medium text-zinc-700 dark:text-zinc-300">
                {movements.map((mov) => (
                  <tr key={mov.id} className="hover:bg-zinc-50/50 dark:hover:bg-zinc-800/30 transition-colors">
                    <td className="px-5 py-4 whitespace-nowrap text-zinc-500">
                      {new Date(mov.createdAtUtc).toLocaleString('es-PE', {
                        day: '2-digit',
                        month: 'short',
                        hour: '2-digit',
                        minute: '2-digit',
                      })}
                    </td>
                    <td className="px-5 py-4">
                      <span className="font-mono font-bold text-zinc-900 dark:text-zinc-100">{mov.sku}</span>
                      <div className="text-[11px] text-zinc-500">{mov.productName}</div>
                    </td>
                    <td className="px-5 py-4">
                      <span className="rounded-md bg-zinc-100 px-2 py-0.5 text-[11px] font-semibold text-zinc-700 dark:bg-zinc-800 dark:text-zinc-300">
                        {mov.typeName}
                      </span>
                    </td>
                    <td className="px-5 py-4 text-center">
                      <span
                        className={`inline-flex items-center gap-0.5 font-bold ${
                          mov.quantityDelta > 0
                            ? 'text-emerald-600 dark:text-emerald-400'
                            : 'text-rose-600 dark:text-rose-400'
                        }`}
                      >
                        {mov.quantityDelta > 0 ? (
                          <TrendingUp className="h-3.5 w-3.5" />
                        ) : (
                          <TrendingDown className="h-3.5 w-3.5" />
                        )}
                        {mov.quantityDelta > 0 ? `+${mov.quantityDelta}` : mov.quantityDelta}
                      </span>
                    </td>
                    <td className="px-5 py-4 text-center font-mono">
                      {mov.previousStock} → <span className="font-bold text-zinc-900 dark:text-white">{mov.newStock}</span>
                    </td>
                    <td className="px-5 py-4 max-w-xs truncate text-zinc-600 dark:text-zinc-400" title={mov.reason}>
                      {mov.reason}
                    </td>
                    <td className="px-5 py-4 text-zinc-500 text-[11px] font-mono">{mov.actor}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          )}
        </div>
      )}

      {/* Adjust Modal */}
      {selectedVariant && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50 backdrop-blur-xs p-4">
          <div className="w-full max-w-md rounded-2xl bg-white p-6 shadow-2xl dark:bg-zinc-900 dark:border dark:border-zinc-800 space-y-4">
            <div className="flex items-center justify-between border-b border-zinc-200 pb-3 dark:border-zinc-800">
              <h3 className="text-base font-bold text-zinc-900 dark:text-white">
                Ajustar Inventario — {selectedVariant.sku}
              </h3>
              <button
                onClick={() => setSelectedVariant(null)}
                className="text-zinc-400 hover:text-zinc-600 dark:hover:text-zinc-200 font-bold"
              >
                ✕
              </button>
            </div>

            {adjustError && (
              <div className="rounded-xl border border-rose-200 bg-rose-50 p-3 text-xs text-rose-800 dark:border-rose-800 dark:bg-rose-950/50 dark:text-rose-300">
                {adjustError}
              </div>
            )}

            <form onSubmit={handleAdjustSubmit} className="space-y-4 text-xs">
              <div>
                <label className="block font-semibold text-zinc-700 dark:text-zinc-300">Producto / Variante</label>
                <div className="mt-1 p-2.5 rounded-xl bg-zinc-50 border border-zinc-200 dark:bg-zinc-800 dark:border-zinc-700 font-medium">
                  {selectedVariant.productName} ({selectedVariant.variantName})
                  <div className="text-zinc-500 mt-0.5">Stock disponible actual: <strong className="text-zinc-900 dark:text-white">{selectedVariant.stockQuantity} unidades</strong></div>
                </div>
              </div>

              <div>
                <label className="block font-semibold text-zinc-700 dark:text-zinc-300">Tipo de Movimiento</label>
                <select
                  value={movementType}
                  onChange={(e) => setMovementType(Number(e.target.value))}
                  className="mt-1 w-full rounded-xl border border-zinc-300 bg-white p-2.5 text-xs text-zinc-900 focus:border-amber-600 focus:outline-hidden dark:border-zinc-700 dark:bg-zinc-800 dark:text-white"
                >
                  <option value={1}>1 - Ingreso de mercadería / Proveedor (+)</option>
                  <option value={2}>2 - Ajuste por conteo físico / Merma</option>
                  <option value={5}>5 - Devolución de cliente (+)</option>
                </select>
              </div>

              <div>
                <label className="block font-semibold text-zinc-700 dark:text-zinc-300">
                  Cantidad a Ajustar (usar negativo para descontar)
                </label>
                <input
                  type="number"
                  required
                  value={delta}
                  onChange={(e) => setDelta(Number(e.target.value))}
                  className="mt-1 w-full rounded-xl border border-zinc-300 p-2.5 text-xs font-mono text-zinc-900 focus:border-amber-600 focus:outline-hidden dark:border-zinc-700 dark:bg-zinc-800 dark:text-white"
                />
                <p className="mt-1 text-[11px] text-zinc-500">
                  Nuevo stock proyectado: <strong className="text-zinc-900 dark:text-white">{selectedVariant.stockQuantity + delta}</strong> unidades
                </p>
              </div>

              <div>
                <label className="block font-semibold text-zinc-700 dark:text-zinc-300">Motivo Obligatorio (Auditoría)</label>
                <textarea
                  required
                  rows={2}
                  value={reason}
                  onChange={(e) => setReason(e.target.value)}
                  placeholder="Ej: Recepción OC-4512 de almacén central..."
                  className="mt-1 w-full rounded-xl border border-zinc-300 p-2.5 text-xs text-zinc-900 focus:border-amber-600 focus:outline-hidden dark:border-zinc-700 dark:bg-zinc-800 dark:text-white"
                />
              </div>

              <div className="flex justify-end gap-3 pt-3 border-t border-zinc-200 dark:border-zinc-800">
                <button
                  type="button"
                  onClick={() => setSelectedVariant(null)}
                  className="rounded-xl border border-zinc-300 px-4 py-2 text-xs font-semibold text-zinc-700 hover:bg-zinc-50 dark:border-zinc-700 dark:text-zinc-300 dark:hover:bg-zinc-800"
                >
                  Cancelar
                </button>
                <button
                  type="submit"
                  disabled={isSubmitting}
                  className="rounded-xl bg-amber-600 px-4 py-2 text-xs font-semibold text-white shadow-2xs hover:bg-amber-500 transition-colors disabled:opacity-50"
                >
                  {isSubmitting ? 'Guardando...' : 'Confirmar Ajuste'}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}
    </div>
  );
}
