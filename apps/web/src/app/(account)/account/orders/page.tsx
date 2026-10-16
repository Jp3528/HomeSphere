'use client';

import React, { useEffect, useState } from 'react';
import Link from 'next/link';
import { ordersApi, OrderSummaryDto } from '@/lib/orders-api';
import { useAuthStore } from '@/stores/auth-store';
import {
  Package,
  Calendar,
  Clock,
  CheckCircle2,
  XCircle,
  Truck,
  ArrowRight,
  ShoppingBag,
  RefreshCw,
  ArrowLeft,
} from 'lucide-react';

export default function AccountOrdersPage() {
  const { isAuthenticated } = useAuthStore();
  const [orders, setOrders] = useState<OrderSummaryDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const loadOrders = async () => {
    try {
      setLoading(true);
      const data = await ordersApi.getMyOrders();
      setOrders(data);
      setError(null);
    } catch (err: unknown) {
      const msg = err instanceof Error ? err.message : 'Error al cargar el historial de pedidos';
      setError(msg);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    let isCancelled = false;

    if (isAuthenticated) {
      ordersApi
        .getMyOrders()
        .then((data) => {
          if (!isCancelled) {
            setOrders(data);
            setError(null);
            setLoading(false);
          }
        })
        .catch((err: unknown) => {
          if (!isCancelled) {
            const msg = err instanceof Error ? err.message : 'Error al cargar el historial de pedidos';
            setError(msg);
            setLoading(false);
          }
        });
    } else {
      // In guest or unauthenticated mode, don't cascade renders
      Promise.resolve().then(() => {
        if (!isCancelled) {
          setLoading(false);
        }
      });
    }

    return () => {
      isCancelled = true;
    };
  }, [isAuthenticated]);

  const getStatusBadge = (status: string) => {
    switch (status) {
      case 'PendingPayment':
        return (
          <span className="inline-flex items-center gap-1 px-2.5 py-0.5 rounded-full text-xs font-semibold bg-amber-100 text-amber-800">
            <Clock className="w-3 h-3" />
            Pendiente de Pago
          </span>
        );
      case 'Paid':
        return (
          <span className="inline-flex items-center gap-1 px-2.5 py-0.5 rounded-full text-xs font-semibold bg-emerald-100 text-emerald-800">
            <CheckCircle2 className="w-3 h-3" />
            Pagado
          </span>
        );
      case 'Processing':
        return (
          <span className="inline-flex items-center gap-1 px-2.5 py-0.5 rounded-full text-xs font-semibold bg-blue-100 text-blue-800">
            <Package className="w-3 h-3" />
            En Preparación
          </span>
        );
      case 'Shipped':
        return (
          <span className="inline-flex items-center gap-1 px-2.5 py-0.5 rounded-full text-xs font-semibold bg-indigo-100 text-indigo-800">
            <Truck className="w-3 h-3" />
            En Camino
          </span>
        );
      case 'Delivered':
        return (
          <span className="inline-flex items-center gap-1 px-2.5 py-0.5 rounded-full text-xs font-semibold bg-emerald-100 text-emerald-900">
            <CheckCircle2 className="w-3 h-3" />
            Entregado
          </span>
        );
      case 'Cancelled':
        return (
          <span className="inline-flex items-center gap-1 px-2.5 py-0.5 rounded-full text-xs font-semibold bg-red-100 text-red-800">
            <XCircle className="w-3 h-3" />
            Cancelado
          </span>
        );
      default:
        return (
          <span className="inline-flex items-center gap-1 px-2.5 py-0.5 rounded-full text-xs font-semibold bg-stone-100 text-stone-700">
            {status}
          </span>
        );
    }
  };

  return (
    <div className="min-h-screen bg-stone-50/50 py-12 px-4 sm:px-6 lg:px-8">
      <div className="max-w-4xl mx-auto space-y-6">
        <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4 pb-4 border-b border-stone-200">
          <div>
            <Link
              href="/account"
              className="text-xs font-medium text-stone-500 hover:text-stone-800 flex items-center gap-1 mb-2"
            >
              <ArrowLeft className="w-3.5 h-3.5" />
              Volver a Mi Cuenta
            </Link>
            <h1 className="text-2xl sm:text-3xl font-serif font-bold text-stone-900">Historial de Pedidos</h1>
            <p className="text-xs text-stone-500 mt-1">
              Consulta el estado de despacho, facturación y detalles de tus compras.
            </p>
          </div>

          <Link
            href="/products"
            className="inline-flex items-center gap-2 px-4 py-2 bg-amber-800 text-white rounded-xl text-xs font-semibold hover:bg-amber-900 transition-colors self-start sm:self-auto"
          >
            <ShoppingBag className="w-3.5 h-3.5" />
            Explorar Catálogo
          </Link>
        </div>

        {loading ? (
          <div className="py-20 text-center">
            <RefreshCw className="w-8 h-8 text-amber-700 animate-spin mx-auto mb-3" />
            <p className="text-sm font-medium text-stone-600">Cargando tus pedidos...</p>
          </div>
        ) : error ? (
          <div className="p-6 bg-red-50 border border-red-200 rounded-2xl text-center">
            <p className="text-sm font-semibold text-red-900">{error}</p>
            <button
              onClick={loadOrders}
              className="mt-3 px-4 py-2 text-xs font-medium bg-red-600 text-white rounded-lg hover:bg-red-700"
            >
              Reintentar
            </button>
          </div>
        ) : orders.length === 0 ? (
          <div className="bg-white p-12 rounded-2xl border border-stone-200 text-center">
            <div className="w-16 h-16 bg-amber-50 text-amber-800 rounded-full flex items-center justify-center mx-auto mb-4">
              <ShoppingBag className="w-8 h-8" />
            </div>
            <h3 className="text-lg font-serif font-bold text-stone-900 mb-2">Aún no tienes pedidos</h3>
            <p className="text-xs text-stone-500 max-w-sm mx-auto mb-6">
              Tus compras de muebles exclusivos y accesorios decorativos aparecerán aquí para seguimiento en tiempo real.
            </p>
            <Link
              href="/products"
              className="inline-flex items-center gap-2 px-6 py-2.5 bg-amber-800 text-white rounded-xl text-sm font-semibold hover:bg-amber-900 transition-colors"
            >
              Ir a Comprar
              <ArrowRight className="w-4 h-4" />
            </Link>
          </div>
        ) : (
          <div className="space-y-4">
            {orders.map((order) => (
              <div
                key={order.id}
                className="bg-white p-5 rounded-2xl border border-stone-200 hover:border-amber-200 transition-all shadow-sm flex flex-col sm:flex-row sm:items-center justify-between gap-4"
              >
                <div className="space-y-1.5">
                  <div className="flex items-center gap-2 flex-wrap">
                    <span className="font-serif font-bold text-base text-stone-900">{order.orderNumber}</span>
                    {getStatusBadge(order.status)}
                  </div>
                  <p className="text-xs text-stone-500 flex items-center gap-1.5">
                    <Calendar className="w-3.5 h-3.5" />
                    {new Date(order.createdAtUtc).toLocaleDateString('es-ES', {
                      year: 'numeric',
                      month: 'long',
                      day: 'numeric',
                    })}
                    <span className="text-stone-300">•</span>
                    <span>{order.itemsCount} {order.itemsCount === 1 ? 'artículo' : 'artículos'}</span>
                  </p>
                </div>

                <div className="flex items-center justify-between sm:justify-end gap-6 pt-3 sm:pt-0 border-t sm:border-t-0 border-stone-100">
                  <div className="text-left sm:text-right">
                    <span className="text-[10px] uppercase tracking-wider text-stone-400 block">Total</span>
                    <span className="text-base font-bold text-stone-950">${order.total.toFixed(2)}</span>
                  </div>

                  <Link
                    href={`/orders/${order.publicId}`}
                    className="inline-flex items-center gap-1.5 px-4 py-2 rounded-xl text-xs font-semibold bg-stone-100 text-stone-800 hover:bg-amber-100 hover:text-amber-900 transition-colors"
                  >
                    Ver Detalle
                    <ArrowRight className="w-3.5 h-3.5" />
                  </Link>
                </div>
              </div>
            ))}
          </div>
        )}
      </div>
    </div>
  );
}
