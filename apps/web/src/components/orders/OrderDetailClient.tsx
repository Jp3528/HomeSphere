'use client';

import React, { useEffect, useState } from 'react';
import Link from 'next/link';
import Image from 'next/image';
import { ordersApi, OrderDto } from '@/lib/orders-api';
import {
  CheckCircle2,
  Clock,
  Package,
  Truck,
  AlertTriangle,
  XCircle,
  CreditCard,
  ArrowLeft,
  ShieldCheck,
  MapPin,
  Calendar,
  RefreshCw,
} from 'lucide-react';

interface OrderDetailClientProps {
  publicId: string;
}

export default function OrderDetailClient({ publicId }: OrderDetailClientProps) {
  const [order, setOrder] = useState<OrderDto | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [actionLoading, setActionLoading] = useState(false);
  const [paymentSuccessMsg, setPaymentSuccessMsg] = useState<string | null>(null);

  useEffect(() => {
    let isCancelled = false;

    ordersApi
      .getOrderByPublicId(publicId)
      .then((data) => {
        if (!isCancelled) {
          setOrder(data);
          setError(null);
          setLoading(false);
        }
      })
      .catch((err: unknown) => {
        if (!isCancelled) {
          const msg = err instanceof Error ? err.message : 'No se pudo cargar el pedido';
          setError(msg);
          setLoading(false);
        }
      });

    return () => {
      isCancelled = true;
    };
  }, [publicId]);

  const refreshOrder = async () => {
    const data = await ordersApi.getOrderByPublicId(publicId);
    setOrder(data);
  };

  const handleSimulatePayment = async () => {
    if (!order) return;
    try {
      setActionLoading(true);
      setError(null);

      // Call payment initiation to get mock checkout URL or execute simulation
      const initResult = await ordersApi.payOrder(order.id);

      if (initResult.success && initResult.paymentUrl) {
        // Automatically simulate webhook confirmation for development demo
        await ordersApi.simulatePaymentWebhook(order.id, order.orderNumber, order.total);
        setPaymentSuccessMsg('¡Pago simulado exitosamente! El webhook seguro de servidor confirmó la transacción.');
        await refreshOrder();
      }
    } catch (err: unknown) {
      const msg = err instanceof Error ? err.message : 'Error al procesar el pago';
      setError(msg);
    } finally {
      setActionLoading(false);
    }
  };

  const handleCancelOrder = async () => {
    if (!order) return;
    const confirmed = window.confirm('¿Estás seguro de que deseas cancelar este pedido? Se liberará el stock reservado.');
    if (!confirmed) return;

    try {
      setActionLoading(true);
      setError(null);
      await ordersApi.cancelOrder(order.id, 'Cancelado por el cliente desde el panel');
      await refreshOrder();
    } catch (err: unknown) {
      const msg = err instanceof Error ? err.message : 'Error al cancelar el pedido';
      setError(msg);
    } finally {
      setActionLoading(false);
    }
  };

  if (loading) {
    return (
      <div className="min-h-[60vh] flex items-center justify-center">
        <div className="text-center">
          <RefreshCw className="w-8 h-8 text-amber-700 animate-spin mx-auto mb-3" />
          <p className="text-sm font-medium text-stone-600">Cargando detalles de tu pedido...</p>
        </div>
      </div>
    );
  }

  if (error || !order) {
    return (
      <div className="min-h-[60vh] flex items-center justify-center px-4">
        <div className="max-w-md w-full bg-white p-8 rounded-2xl border border-stone-200 shadow-sm text-center">
          <div className="w-14 h-14 bg-red-50 text-red-600 rounded-full flex items-center justify-center mx-auto mb-4">
            <XCircle className="w-8 h-8" />
          </div>
          <h2 className="text-xl font-serif font-bold text-stone-900 mb-2">Pedido no encontrado</h2>
          <p className="text-sm text-stone-500 mb-6">{error || 'El identificador de pedido no es válido.'}</p>
          <Link
            href="/products"
            className="inline-flex items-center gap-2 px-5 py-2.5 rounded-xl bg-amber-800 text-white text-sm font-medium hover:bg-amber-900 transition-colors"
          >
            <ArrowLeft className="w-4 h-4" />
            Explorar Catálogo
          </Link>
        </div>
      </div>
    );
  }

  let addressObj: {
    RecipientName?: string;
    PhoneNumber?: string;
    Line1?: string;
    Line2?: string;
    District?: string;
    City?: string;
    Region?: string;
  } = {};
  try {
    addressObj = JSON.parse(order.shippingAddressSnapshotJson);
  } catch {
    // fallback
  }

  const getStatusBadge = (status: string) => {
    switch (status) {
      case 'PendingPayment':
        return (
          <span className="inline-flex items-center gap-1.5 px-3 py-1 rounded-full text-xs font-semibold bg-amber-100 text-amber-800 border border-amber-200">
            <Clock className="w-3.5 h-3.5" />
            Pendiente de Pago
          </span>
        );
      case 'Paid':
        return (
          <span className="inline-flex items-center gap-1.5 px-3 py-1 rounded-full text-xs font-semibold bg-emerald-100 text-emerald-800 border border-emerald-200">
            <CheckCircle2 className="w-3.5 h-3.5" />
            Pago Confirmado
          </span>
        );
      case 'Processing':
        return (
          <span className="inline-flex items-center gap-1.5 px-3 py-1 rounded-full text-xs font-semibold bg-blue-100 text-blue-800 border border-blue-200">
            <Package className="w-3.5 h-3.5" />
            En Preparación
          </span>
        );
      case 'Shipped':
        return (
          <span className="inline-flex items-center gap-1.5 px-3 py-1 rounded-full text-xs font-semibold bg-indigo-100 text-indigo-800 border border-indigo-200">
            <Truck className="w-3.5 h-3.5" />
            En Camino
          </span>
        );
      case 'Delivered':
        return (
          <span className="inline-flex items-center gap-1.5 px-3 py-1 rounded-full text-xs font-semibold bg-emerald-100 text-emerald-900 border border-emerald-300">
            <CheckCircle2 className="w-3.5 h-3.5" />
            Entregado
          </span>
        );
      case 'Cancelled':
        return (
          <span className="inline-flex items-center gap-1.5 px-3 py-1 rounded-full text-xs font-semibold bg-red-100 text-red-800 border border-red-200">
            <XCircle className="w-3.5 h-3.5" />
            Cancelado
          </span>
        );
      default:
        return (
          <span className="inline-flex items-center gap-1.5 px-3 py-1 rounded-full text-xs font-semibold bg-stone-100 text-stone-800">
            {status}
          </span>
        );
    }
  };

  const steps = [
    { key: 'PendingPayment', label: 'Pendiente' },
    { key: 'Paid', label: 'Pagado' },
    { key: 'Processing', label: 'Preparación' },
    { key: 'Shipped', label: 'En Camino' },
    { key: 'Delivered', label: 'Entregado' },
  ];

  const currentStepIdx = steps.findIndex((s) => s.key === order.status);
  const isCancelled = order.status === 'Cancelled';

  return (
    <div className="min-h-screen bg-stone-50/50 py-12 px-4 sm:px-6 lg:px-8">
      <div className="max-w-4xl mx-auto space-y-8">
        {/* Navigation & Header */}
        <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4 pb-4 border-b border-stone-200">
          <div>
            <div className="flex items-center gap-2 mb-2">
              <Link
                href="/products"
                className="text-xs font-medium text-stone-500 hover:text-stone-800 flex items-center gap-1"
              >
                <ArrowLeft className="w-3.5 h-3.5" />
                Volver a la tienda
              </Link>
            </div>
            <div className="flex items-center gap-3">
              <h1 className="text-2xl sm:text-3xl font-serif font-bold text-stone-900">
                Pedido {order.orderNumber}
              </h1>
              {getStatusBadge(order.status)}
            </div>
            <p className="text-xs text-stone-500 mt-1 flex items-center gap-2">
              <Calendar className="w-3.5 h-3.5" />
              Creado el {new Date(order.createdAtUtc).toLocaleString('es-ES', { dateStyle: 'long', timeStyle: 'short' })}
            </p>
          </div>

          <div className="flex items-center gap-2">
            <Link
              href="/account/orders"
              className="px-4 py-2 text-xs font-semibold text-stone-700 bg-white border border-stone-300 rounded-lg hover:bg-stone-50 transition-colors"
            >
              Mis Pedidos
            </Link>
          </div>
        </div>

        {/* Payment Success Alert */}
        {paymentSuccessMsg && (
          <div className="p-4 bg-emerald-50 border border-emerald-200 rounded-xl flex items-start gap-3">
            <CheckCircle2 className="w-5 h-5 text-emerald-600 mt-0.5 flex-shrink-0" />
            <div>
              <p className="text-sm font-semibold text-emerald-900">{paymentSuccessMsg}</p>
              <p className="text-xs text-emerald-700 mt-0.5">
                Las reservas de stock han sido confirmadas de forma permanente.
              </p>
            </div>
          </div>
        )}

        {/* Pending Payment Warning & Actions */}
        {order.status === 'PendingPayment' && (
          <div className="bg-amber-50/80 border border-amber-200/90 rounded-2xl p-6">
            <div className="flex items-start gap-4">
              <div className="p-2.5 bg-amber-100 rounded-xl text-amber-700 flex-shrink-0">
                <AlertTriangle className="w-6 h-6" />
              </div>
              <div className="flex-1">
                <h3 className="text-base font-serif font-bold text-amber-900 mb-1">
                  Reserva temporal de stock activa (15 minutos)
                </h3>
                <p className="text-xs text-amber-800 leading-relaxed mb-4">
                  Los muebles y accesorios de este pedido han sido reservados exclusivamente para ti. Para evitar que el
                  sistema libere automáticamente el inventario al expirar el tiempo, procesa tu pago a continuación.
                </p>
                <div className="flex flex-wrap items-center gap-3">
                  <button
                    type="button"
                    onClick={handleSimulatePayment}
                    disabled={actionLoading}
                    className="inline-flex items-center gap-2 px-6 py-2.5 bg-amber-800 text-white rounded-xl text-sm font-semibold shadow hover:bg-amber-900 transition-colors disabled:opacity-50"
                  >
                    <CreditCard className="w-4 h-4" />
                    {actionLoading ? 'Procesando Pago...' : `Completar Pago Seguro ($${order.total.toFixed(2)})`}
                  </button>
                  <button
                    type="button"
                    onClick={handleCancelOrder}
                    disabled={actionLoading}
                    className="px-4 py-2.5 text-xs font-semibold text-stone-600 bg-white border border-stone-300 rounded-xl hover:bg-stone-50 transition-colors disabled:opacity-50"
                  >
                    Cancelar Pedido
                  </button>
                </div>
              </div>
            </div>
          </div>
        )}

        {/* State Machine Stepper Progress Bar */}
        {!isCancelled && (
          <div className="bg-white p-6 rounded-2xl border border-stone-200 shadow-sm">
            <h3 className="text-sm font-semibold text-stone-700 mb-6">Estado del Despacho</h3>
            <div className="relative flex items-center justify-between">
              <div className="absolute left-0 top-1/2 -translate-y-1/2 h-1 bg-stone-200 w-full z-0" />
              <div
                className="absolute left-0 top-1/2 -translate-y-1/2 h-1 bg-amber-700 transition-all duration-500 z-0"
                style={{
                  width: `${(Math.max(0, currentStepIdx) / (steps.length - 1)) * 100}%`,
                }}
              />
              {steps.map((step, idx) => {
                const isPassed = currentStepIdx >= idx;
                const isCurrent = currentStepIdx === idx;
                return (
                  <div key={step.key} className="relative z-10 flex flex-col items-center">
                    <div
                      className={`w-8 h-8 rounded-full flex items-center justify-center text-xs font-bold transition-colors ${
                        isCurrent
                          ? 'bg-amber-800 text-white ring-4 ring-amber-100'
                          : isPassed
                          ? 'bg-amber-800 text-white'
                          : 'bg-white border-2 border-stone-300 text-stone-400'
                      }`}
                    >
                      {idx + 1}
                    </div>
                    <span
                      className={`text-[11px] font-medium mt-2 text-center ${
                        isCurrent ? 'text-amber-900 font-bold' : isPassed ? 'text-stone-800' : 'text-stone-400'
                      }`}
                    >
                      {step.label}
                    </span>
                  </div>
                );
              })}
            </div>
          </div>
        )}

        {/* Main Content Grid: Items & Order Details */}
        <div className="grid grid-cols-1 lg:grid-cols-3 gap-8">
          {/* Order Items Snapshot (2 cols) */}
          <div className="lg:col-span-2 space-y-6">
            <div className="bg-white p-6 rounded-2xl border border-stone-200 shadow-sm">
              <h2 className="text-base font-serif font-bold text-stone-900 mb-4 flex items-center gap-2">
                <Package className="w-4 h-4 text-amber-800" />
                Artículos Congelados ({order.items.length})
              </h2>

              <div className="divide-y divide-stone-100">
                {order.items.map((item) => (
                  <div key={item.id} className="py-4 first:pt-0 last:pb-0 flex items-center gap-4">
                    <div className="w-16 h-16 rounded-xl bg-stone-100 relative overflow-hidden flex-shrink-0 border border-stone-200">
                      {item.imageUrlSnapshot ? (
                        <Image
                          src={item.imageUrlSnapshot}
                          alt={item.productNameSnapshot}
                          fill
                          className="object-cover"
                        />
                      ) : (
                        <div className="w-full h-full flex items-center justify-center text-stone-300">
                          <Package className="w-6 h-6" />
                        </div>
                      )}
                    </div>

                    <div className="flex-1 min-w-0">
                      <h4 className="text-sm font-semibold text-stone-900 truncate">
                        {item.productNameSnapshot}
                      </h4>
                      <p className="text-xs text-stone-500 font-mono mt-0.5">SKU: {item.skuSnapshot}</p>
                      <p className="text-xs text-stone-600 mt-1">
                        Cantidad: <span className="font-semibold">{item.quantity}</span> × $
                        {item.unitPrice.toFixed(2)}
                      </p>
                    </div>

                    <div className="text-right">
                      <p className="text-sm font-bold text-stone-900">${item.totalPrice.toFixed(2)}</p>
                    </div>
                  </div>
                ))}
              </div>
            </div>

            {/* Audit / Status Transition History */}
            {order.statusHistory && order.statusHistory.length > 0 && (
              <div className="bg-white p-6 rounded-2xl border border-stone-200 shadow-sm">
                <h3 className="text-sm font-serif font-bold text-stone-900 mb-4 flex items-center gap-2">
                  <Clock className="w-4 h-4 text-amber-800" />
                  Historial de Auditoría de Estados
                </h3>
                <div className="space-y-3">
                  {order.statusHistory.map((h, i) => (
                    <div key={i} className="flex items-start gap-3 text-xs border-l-2 border-amber-800/40 pl-3 py-1">
                      <div>
                        <div className="flex items-center gap-2">
                          <span className="font-semibold text-stone-800">
                            {h.previousStatus} → {h.newStatus}
                          </span>
                          <span className="text-[10px] bg-stone-100 text-stone-600 px-2 py-0.5 rounded font-mono">
                            {h.changedBy}
                          </span>
                        </div>
                        {h.reason && <p className="text-stone-500 mt-0.5 italic">{h.reason}</p>}
                        <p className="text-[10px] text-stone-400 mt-0.5">
                          {new Date(h.createdAtUtc).toLocaleString('es-ES')}
                        </p>
                      </div>
                    </div>
                  ))}
                </div>
              </div>
            )}
          </div>

          {/* Delivery & Cost Breakdown (1 col) */}
          <div className="space-y-6">
            {/* Delivery Details */}
            <div className="bg-white p-6 rounded-2xl border border-stone-200 shadow-sm space-y-4">
              <h3 className="text-sm font-serif font-bold text-stone-900 flex items-center gap-2">
                <MapPin className="w-4 h-4 text-amber-800" />
                Dirección de Envío
              </h3>

              <div className="text-xs text-stone-600 space-y-1">
                <p className="font-bold text-stone-900 text-sm">{addressObj.RecipientName || 'Destinatario'}</p>
                <p>{addressObj.Line1 || 'Dirección no especificada'}</p>
                {addressObj.Line2 && <p>{addressObj.Line2}</p>}
                <p>
                  {[addressObj.District, addressObj.City, addressObj.Region].filter(Boolean).join(', ')}
                </p>
                {addressObj.PhoneNumber && (
                  <p className="pt-1 text-stone-500 font-mono">Tel: {addressObj.PhoneNumber}</p>
                )}
              </div>

              <div className="pt-3 border-t border-stone-100 text-xs">
                <span className="text-stone-500">Método de Transporte:</span>
                <span className="font-semibold text-stone-800 block capitalize">
                  {order.shippingMethod === 'express' ? 'Envío Prioritario Express' : 'Transporte Estándar Especializado'}
                </span>
              </div>
            </div>

            {/* Financial Summary */}
            <div className="bg-white p-6 rounded-2xl border border-stone-200 shadow-sm space-y-3">
              <h3 className="text-sm font-serif font-bold text-stone-900 mb-2">Desglose de Pago</h3>

              <div className="flex justify-between text-xs text-stone-600">
                <span>Subtotal</span>
                <span>${order.subtotal.toFixed(2)}</span>
              </div>

              {order.discountTotal > 0 && (
                <div className="flex justify-between text-xs text-emerald-700 font-medium">
                  <span>Descuento {order.couponCode ? `(${order.couponCode})` : ''}</span>
                  <span>-${order.discountTotal.toFixed(2)}</span>
                </div>
              )}

              <div className="flex justify-between text-xs text-stone-600">
                <span>Envío</span>
                <span>{order.shippingCost === 0 ? 'GRATIS' : `$${order.shippingCost.toFixed(2)}`}</span>
              </div>

              <div className="pt-3 border-t border-stone-200 flex justify-between text-base font-bold text-stone-950">
                <span>Total</span>
                <span>${order.total.toFixed(2)}</span>
              </div>

              <div className="pt-4 flex items-center justify-center gap-2 text-[11px] text-stone-400">
                <ShieldCheck className="w-3.5 h-3.5 text-emerald-600" />
                <span>Auditoría y cálculos 100% verificados por el servidor</span>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}
