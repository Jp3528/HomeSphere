'use client';

import React, { useEffect, useState } from 'react';
import Link from 'next/link';
import Image from 'next/image';
import { useCartStore } from '@/stores/cart-store';
import { apiClient } from '@/lib/api-client';
import { ValidateCouponResponse } from '@/types/checkout';
import {
  ShoppingBag,
  Trash2,
  Plus,
  Minus,
  ArrowRight,
  ShieldCheck,
  Truck,
  RotateCcw,
  Tag,
  CheckCircle2,
  AlertCircle
} from 'lucide-react';

export default function CartPage() {
  const { cart, isLoading, fetchCart, updateQuantity, removeItem, clearCart } = useCartStore();
  const [couponCode, setCouponCode] = useState('');
  const [couponLoading, setCouponLoading] = useState(false);
  const [couponResult, setCouponResult] = useState<ValidateCouponResponse | null>(null);

  useEffect(() => {
    fetchCart();
  }, [fetchCart]);

  const items = cart?.items || [];
  const subtotal = cart?.subtotal || 0;
  const freeShippingThreshold = 1000;
  const isFreeShipping = subtotal >= freeShippingThreshold;
  const shippingCost = isFreeShipping ? 0 : (subtotal > 0 ? 15 : 0);
  const discountAmount = couponResult?.isValid && couponResult.coupon ? couponResult.coupon.discountAmount : 0;
  const estimatedTotal = Math.max(0, subtotal - discountAmount) + shippingCost;

  const handleApplyCoupon = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!couponCode.trim()) return;

    setCouponLoading(true);
    setCouponResult(null);
    try {
      const res = await apiClient.get<ValidateCouponResponse>(
        `/api/checkout/coupons/validate?code=${encodeURIComponent(couponCode.trim())}&subtotal=${subtotal}`
      );
      setCouponResult(res);
    } catch {
      setCouponResult({
        isValid: false,
        message: 'Error al conectar con el servidor para validar el cupón.',
      });
    } finally {
      setCouponLoading(false);
    }
  };

  if (items.length === 0 && !isLoading) {
    return (
      <div className="min-h-[70vh] flex items-center justify-center px-4 py-16">
        <div className="max-w-md w-full text-center">
          <div className="w-24 h-24 rounded-full bg-amber-500/10 text-amber-500 flex items-center justify-center mx-auto mb-6">
            <ShoppingBag className="w-12 h-12" />
          </div>
          <h1 className="text-3xl font-serif font-semibold text-stone-900 mb-3">Tu carrito está vacío</h1>
          <p className="text-stone-600 mb-8 text-sm leading-relaxed">
            Aún no has agregado ninguna pieza a tu selección. Explora nuestras colecciones exclusivas o inspírate con nuestros ambientes interactivos.
          </p>
          <div className="flex flex-col sm:flex-row gap-3 justify-center">
            <Link
              href="/products"
              className="inline-flex items-center justify-center gap-2 px-6 py-3 rounded-xl bg-amber-800 hover:bg-amber-900 text-white font-medium text-sm transition-colors shadow-sm"
            >
              Explorar Catálogo
              <ArrowRight className="w-4 h-4" />
            </Link>
            <Link
              href="/scenes"
              className="inline-flex items-center justify-center gap-2 px-6 py-3 rounded-xl bg-stone-200 hover:bg-stone-300 text-stone-800 font-medium text-sm transition-colors"
            >
              Ver Ambientes
            </Link>
          </div>
        </div>
      </div>
    );
  }

  return (
    <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-10 lg:py-16">
      <div className="mb-8">
        <h1 className="text-3xl lg:text-4xl font-serif font-bold text-stone-900">Carrito de Compras</h1>
        <p className="text-sm text-stone-500 mt-1">
          Revisa tus artículos seleccionados antes de proceder al pago seguro.
        </p>
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-12 gap-8 lg:gap-12 items-start">
        {/* Left Column: Items List */}
        <div className="lg:col-span-8 space-y-4">
          <div className="bg-white rounded-2xl border border-stone-200/80 shadow-xs divide-y divide-stone-100 overflow-hidden">
            {items.map((item) => (
              <div key={item.id} className="p-4 sm:p-6 flex flex-col sm:flex-row gap-4 sm:gap-6 items-start sm:items-center">
                {/* Thumbnail */}
                <div className="w-24 h-24 rounded-xl overflow-hidden bg-stone-100 shrink-0 relative border border-stone-200/60">
                  {item.primaryImageUrl ? (
                    <Image
                      src={item.primaryImageUrl}
                      alt={item.productName}
                      fill
                      className="object-cover"
                      sizes="96px"
                    />
                  ) : (
                    <div className="w-full h-full flex items-center justify-center text-stone-400">
                      <ShoppingBag className="w-8 h-8" />
                    </div>
                  )}
                </div>

                {/* Info */}
                <div className="flex-1 min-w-0">
                  <div className="flex justify-between items-start">
                    <div>
                      <Link
                        href={`/products/${item.productSlug}`}
                        className="text-base font-semibold text-stone-900 hover:text-amber-800 transition-colors line-clamp-1"
                      >
                        {item.productName}
                      </Link>
                      <p className="text-xs text-stone-500 font-mono mt-0.5">
                        Variante: {item.variantName} • SKU: {item.sku}
                      </p>
                    </div>
                    <button
                      onClick={() => removeItem(item.id)}
                      disabled={isLoading}
                      className="text-stone-400 hover:text-rose-600 p-1.5 transition-colors disabled:opacity-50"
                      title="Eliminar de la lista"
                    >
                      <Trash2 className="w-4 h-4" />
                    </button>
                  </div>

                  {item.warning && (
                    <div className="mt-2 text-xs text-amber-700 bg-amber-50 border border-amber-200/60 px-2.5 py-1 rounded-md flex items-center gap-1.5">
                      <AlertCircle className="w-3.5 h-3.5 shrink-0" />
                      <span>{item.warning}</span>
                    </div>
                  )}

                  <div className="flex flex-wrap items-center justify-between gap-4 mt-4 pt-3 border-t border-stone-100">
                    <div className="flex items-center border border-stone-200 rounded-lg bg-stone-50/50">
                      <button
                        onClick={() => updateQuantity(item.id, item.quantity - 1)}
                        disabled={isLoading || item.quantity <= 1}
                        className="p-1.5 text-stone-600 hover:text-stone-900 disabled:opacity-30 transition-colors"
                        aria-label="Disminuir cantidad"
                      >
                        <Minus className="w-4 h-4" />
                      </button>
                      <span className="w-10 text-center text-xs font-mono font-medium text-stone-900">
                        {item.quantity}
                      </span>
                      <button
                        onClick={() => updateQuantity(item.id, item.quantity + 1)}
                        disabled={isLoading || item.quantity >= Math.min(99, item.availableStock)}
                        className="p-1.5 text-stone-600 hover:text-stone-900 disabled:opacity-30 transition-colors"
                        aria-label="Aumentar cantidad"
                      >
                        <Plus className="w-4 h-4" />
                      </button>
                    </div>

                    <div className="flex items-baseline gap-2">
                      <span className="text-xs text-stone-400 font-mono">
                        ${item.unitPrice.toFixed(2)} c/u
                      </span>
                      <span className="text-lg font-serif font-semibold text-stone-900 font-mono">
                        ${item.lineTotal.toFixed(2)}
                      </span>
                    </div>
                  </div>
                </div>
              </div>
            ))}
          </div>

          <div className="flex justify-between items-center px-2 pt-2">
            <Link
              href="/products"
              className="text-xs font-semibold text-stone-600 hover:text-stone-900 flex items-center gap-1 transition-colors"
            >
              ← Continuar Comprando
            </Link>
            <button
              onClick={clearCart}
              disabled={isLoading}
              className="text-xs text-stone-500 hover:text-rose-600 transition-colors disabled:opacity-50"
            >
              Vaciar carrito completo
            </button>
          </div>
        </div>

        {/* Right Column: Order Summary & Coupon */}
        <div className="lg:col-span-4 space-y-6">
          <div className="bg-white rounded-2xl border border-stone-200/80 p-6 shadow-xs space-y-6">
            <h2 className="text-lg font-serif font-bold text-stone-900 pb-4 border-b border-stone-100">
              Resumen de la Orden
            </h2>

            {/* Subtotal & Free Shipping Progress */}
            <div className="space-y-3">
              <div className="flex justify-between text-sm">
                <span className="text-stone-600">Subtotal ({cart?.totalItems} productos)</span>
                <span className="font-mono font-medium text-stone-900">${subtotal.toFixed(2)}</span>
              </div>

              <div className="flex justify-between text-sm">
                <span className="text-stone-600">Envío Estimado</span>
                <span className="font-mono font-medium text-stone-900">
                  {isFreeShipping ? (
                    <span className="text-emerald-700 font-semibold">GRATIS</span>
                  ) : (
                    `$${shippingCost.toFixed(2)}`
                  )}
                </span>
              </div>

              {discountAmount > 0 && (
                <div className="flex justify-between text-sm text-emerald-700">
                  <span className="flex items-center gap-1">
                    <Tag className="w-3.5 h-3.5" />
                    Descuento ({couponResult?.coupon?.code})
                  </span>
                  <span className="font-mono font-semibold">-${discountAmount.toFixed(2)}</span>
                </div>
              )}

              <div className="pt-2">
                <div className="flex justify-between text-xs text-stone-500 mb-1">
                  <span>Envío gratis a partir de $1,000</span>
                  <span>{isFreeShipping ? '¡Califica!' : `$${(freeShippingThreshold - subtotal).toFixed(2)} restantes`}</span>
                </div>
                <div className="w-full bg-stone-100 h-1.5 rounded-full overflow-hidden">
                  <div
                    className="bg-amber-600 h-full rounded-full transition-all duration-300"
                    style={{ width: `${Math.min(100, (subtotal / freeShippingThreshold) * 100)}%` }}
                  />
                </div>
              </div>
            </div>

            {/* Coupon Box */}
            <div className="pt-4 border-t border-stone-100">
              <label htmlFor="coupon-input" className="block text-xs font-semibold text-stone-700 uppercase tracking-wider mb-2">
                ¿Tienes un cupón de descuento?
              </label>
              <form onSubmit={handleApplyCoupon} className="flex gap-2">
                <input
                  id="coupon-input"
                  type="text"
                  placeholder="Ej: BIENVENIDA10, HOGAR50"
                  value={couponCode}
                  onChange={(e) => setCouponCode(e.target.value.toUpperCase())}
                  className="flex-1 rounded-xl border border-stone-300 px-3.5 py-2 text-xs font-mono uppercase placeholder:text-stone-400 focus:border-amber-700 focus:outline-none focus:ring-1 focus:ring-amber-700"
                />
                <button
                  type="submit"
                  disabled={couponLoading || !couponCode.trim()}
                  className="px-4 py-2 rounded-xl bg-stone-900 hover:bg-stone-800 text-white text-xs font-semibold transition-colors disabled:opacity-40"
                >
                  {couponLoading ? '...' : 'Aplicar'}
                </button>
              </form>

              {couponResult && (
                <div
                  className={`mt-2.5 p-2.5 rounded-xl text-xs flex items-start gap-2 ${
                    couponResult.isValid
                      ? 'bg-emerald-50 text-emerald-800 border border-emerald-200/60'
                      : 'bg-rose-50 text-rose-800 border border-rose-200/60'
                  }`}
                >
                  {couponResult.isValid ? (
                    <CheckCircle2 className="w-4 h-4 shrink-0 text-emerald-600 mt-0.5" />
                  ) : (
                    <AlertCircle className="w-4 h-4 shrink-0 text-rose-600 mt-0.5" />
                  )}
                  <div>
                    {couponResult.isValid ? (
                      <>
                        <p className="font-semibold">{couponResult.coupon?.description}</p>
                        <p className="text-[11px] text-emerald-700 mt-0.5 font-mono">
                          Ahorras ${couponResult.coupon?.discountAmount.toFixed(2)} en este pedido.
                        </p>
                      </>
                    ) : (
                      <p>{couponResult.message}</p>
                    )}
                  </div>
                </div>
              )}
            </div>

            {/* Total & Checkout Button */}
            <div className="pt-4 border-t border-stone-100 space-y-4">
              <div className="flex justify-between items-baseline">
                <span className="text-base font-semibold text-stone-900">Total Estimado</span>
                <span className="text-2xl font-serif font-bold text-stone-900 font-mono">
                  ${estimatedTotal.toFixed(2)}
                </span>
              </div>

              <Link
                href="/checkout"
                className="w-full flex items-center justify-center gap-2 py-3.5 px-6 rounded-xl bg-amber-800 hover:bg-amber-900 text-white font-semibold text-sm transition-colors shadow-sm"
              >
                Proceder al Checkout
                <ArrowRight className="w-4 h-4" />
              </Link>
            </div>
          </div>

          {/* Trust Badges */}
          <div className="bg-stone-50 rounded-2xl border border-stone-200/60 p-5 space-y-3.5 text-xs text-stone-600">
            <div className="flex items-center gap-3">
              <div className="w-8 h-8 rounded-full bg-amber-500/10 text-amber-800 flex items-center justify-center shrink-0">
                <Truck className="w-4 h-4" />
              </div>
              <div>
                <strong className="text-stone-800 block">Envío Asegurado</strong>
                Despacho con embalaje reforzado y tracking en tiempo real.
              </div>
            </div>
            <div className="flex items-center gap-3">
              <div className="w-8 h-8 rounded-full bg-amber-500/10 text-amber-800 flex items-center justify-center shrink-0">
                <RotateCcw className="w-4 h-4" />
              </div>
              <div>
                <strong className="text-stone-800 block">Garantía de Satisfacción 30 Días</strong>
                Devoluciones sencillas y sin preguntas si no encaja con tu espacio.
              </div>
            </div>
            <div className="flex items-center gap-3">
              <div className="w-8 h-8 rounded-full bg-amber-500/10 text-amber-800 flex items-center justify-center shrink-0">
                <ShieldCheck className="w-4 h-4" />
              </div>
              <div>
                <strong className="text-stone-800 block">Autenticidad y Calidad</strong>
                Materiales nobles seleccionados por diseñadores de interiores.
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}
