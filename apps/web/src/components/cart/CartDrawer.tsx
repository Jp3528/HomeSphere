'use client';

import React, { useEffect, useRef } from 'react';
import Link from 'next/link';
import Image from 'next/image';
import { useCartStore } from '@/stores/cart-store';
import { X, Trash2, Plus, Minus, ShoppingBag, ArrowRight, Truck } from 'lucide-react';

export function CartDrawer() {
  const { cart, isOpen, isLoading, closeDrawer, fetchCart, updateQuantity, removeItem } = useCartStore();
  const drawerRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    fetchCart();
  }, [fetchCart]);

  // Close on Escape key
  useEffect(() => {
    const handleKeyDown = (e: KeyboardEvent) => {
      if (e.key === 'Escape' && isOpen) {
        closeDrawer();
      }
    };
    window.addEventListener('keydown', handleKeyDown);
    return () => window.removeEventListener('keydown', handleKeyDown);
  }, [isOpen, closeDrawer]);

  // Lock body scroll when open
  useEffect(() => {
    if (isOpen) {
      document.body.style.overflow = 'hidden';
    } else {
      document.body.style.overflow = '';
    }
    return () => {
      document.body.style.overflow = '';
    };
  }, [isOpen]);

  if (!isOpen) return null;

  const items = cart?.items || [];
  const subtotal = cart?.subtotal || 0;
  const freeShippingThreshold = 1000;
  const freeShippingProgress = Math.min(100, (subtotal / freeShippingThreshold) * 100);
  const remainingForFreeShipping = Math.max(0, freeShippingThreshold - subtotal);

  return (
    <div className="fixed inset-0 z-50 overflow-hidden" role="dialog" aria-modal="true" aria-label="Carrito de compras">
      {/* Backdrop */}
      <div
        className="fixed inset-0 bg-stone-900/60 backdrop-blur-xs transition-opacity animate-in fade-in duration-300"
        onClick={closeDrawer}
      />

      <div className="fixed inset-y-0 right-0 max-w-full flex pl-10">
        <div
          ref={drawerRef}
          className="w-screen max-w-md bg-stone-900 text-stone-100 border-l border-stone-800 shadow-2xl flex flex-col transform transition-transform animate-in slide-in-from-right duration-300"
        >
          {/* Header */}
          <div className="p-5 border-b border-stone-800 flex items-center justify-between">
            <div className="flex items-center gap-3">
              <div className="w-9 h-9 rounded-full bg-amber-500/10 text-amber-500 flex items-center justify-center">
                <ShoppingBag className="w-5 h-5" />
              </div>
              <div>
                <h2 className="text-lg font-serif font-medium text-stone-100">Bolsa de Compras</h2>
                <p className="text-xs text-stone-400">
                  {cart?.totalItems || 0} {cart?.totalItems === 1 ? 'artículo' : 'artículos'}
                </p>
              </div>
            </div>
            <button
              onClick={closeDrawer}
              className="p-2 rounded-lg text-stone-400 hover:text-stone-200 hover:bg-stone-800 transition-colors"
              aria-label="Cerrar carrito"
            >
              <X className="w-5 h-5" />
            </button>
          </div>

          {/* Free Shipping Meter */}
          <div className="bg-stone-950 px-5 py-3 border-b border-stone-800/80">
            <div className="flex items-center justify-between text-xs mb-1.5">
              <span className="flex items-center gap-1.5 text-stone-300">
                <Truck className="w-3.5 h-3.5 text-amber-500" />
                {remainingForFreeShipping > 0 ? (
                  <>
                    Te faltan <strong className="text-amber-400">${remainingForFreeShipping.toFixed(2)}</strong> para{' '}
                    <strong className="text-stone-100">Envío Gratis</strong>
                  </>
                ) : (
                  <span className="text-emerald-400 font-medium">¡Felicidades! Tienes Envío Estándar Gratis</span>
                )}
              </span>
              <span className="text-stone-400 font-mono text-[11px]">{freeShippingProgress.toFixed(0)}%</span>
            </div>
            <div className="w-full bg-stone-800 h-1.5 rounded-full overflow-hidden">
              <div
                className="bg-amber-500 h-full rounded-full transition-all duration-500"
                style={{ width: `${freeShippingProgress}%` }}
              />
            </div>
          </div>

          {/* Items List */}
          <div className="flex-1 overflow-y-auto p-5 space-y-4">
            {items.length === 0 ? (
              <div className="h-full flex flex-col items-center justify-center text-center p-6 text-stone-400">
                <div className="w-16 h-16 rounded-full bg-stone-800 flex items-center justify-center mb-4 text-stone-500">
                  <ShoppingBag className="w-8 h-8" />
                </div>
                <h3 className="text-base font-medium text-stone-200 mb-1">Tu bolsa está vacía</h3>
                <p className="text-sm text-stone-400 max-w-xs mb-6">
                  Descubre nuestras piezas de autor y ambientes interactivos para transformar tu hogar.
                </p>
                <Link
                  href="/products"
                  onClick={closeDrawer}
                  className="inline-flex items-center gap-2 px-5 py-2.5 rounded-lg bg-amber-600 hover:bg-amber-500 text-stone-950 font-medium text-sm transition-colors"
                >
                  Explorar Catálogo
                  <ArrowRight className="w-4 h-4" />
                </Link>
              </div>
            ) : (
              items.map((item) => (
                <div
                  key={item.id}
                  className="flex gap-4 p-3 rounded-xl bg-stone-950/50 border border-stone-800/60 hover:border-stone-700 transition-colors"
                >
                  {/* Thumbnail */}
                  <div className="w-20 h-20 rounded-lg overflow-hidden bg-stone-800 shrink-0 relative">
                    {item.primaryImageUrl ? (
                      <Image
                        src={item.primaryImageUrl}
                        alt={item.productName}
                        fill
                        className="object-cover"
                        sizes="80px"
                      />
                    ) : (
                      <div className="w-full h-full flex items-center justify-center text-stone-600">
                        <ShoppingBag className="w-6 h-6" />
                      </div>
                    )}
                  </div>

                  {/* Details */}
                  <div className="flex-1 min-w-0 flex flex-col justify-between">
                    <div>
                      <div className="flex items-start justify-between gap-2">
                        <Link
                          href={`/products/${item.productSlug}`}
                          onClick={closeDrawer}
                          className="text-sm font-medium text-stone-100 hover:text-amber-400 line-clamp-1 transition-colors"
                        >
                          {item.productName}
                        </Link>
                        <button
                          onClick={() => removeItem(item.id)}
                          disabled={isLoading}
                          className="text-stone-500 hover:text-rose-400 p-1 transition-colors disabled:opacity-50"
                          title="Eliminar producto"
                        >
                          <Trash2 className="w-4 h-4" />
                        </button>
                      </div>
                      <p className="text-xs text-stone-400 font-mono mt-0.5">{item.variantName}</p>

                      {item.warning && (
                        <p className="text-[11px] text-amber-400 mt-1 bg-amber-950/40 px-2 py-0.5 rounded border border-amber-900/40">
                          {item.warning}
                        </p>
                      )}
                    </div>

                    <div className="flex items-center justify-between mt-2 pt-2 border-t border-stone-800/40">
                      {/* Quantity Controls */}
                      <div className="flex items-center border border-stone-700 rounded-lg bg-stone-900">
                        <button
                          onClick={() => updateQuantity(item.id, item.quantity - 1)}
                          disabled={isLoading || item.quantity <= 1}
                          className="p-1 text-stone-400 hover:text-stone-200 disabled:opacity-30 transition-colors"
                          aria-label="Disminuir cantidad"
                        >
                          <Minus className="w-3.5 h-3.5" />
                        </button>
                        <span className="w-7 text-center text-xs font-mono text-stone-200">{item.quantity}</span>
                        <button
                          onClick={() => updateQuantity(item.id, item.quantity + 1)}
                          disabled={isLoading || item.quantity >= Math.min(99, item.availableStock)}
                          className="p-1 text-stone-400 hover:text-stone-200 disabled:opacity-30 transition-colors"
                          aria-label="Aumentar cantidad"
                        >
                          <Plus className="w-3.5 h-3.5" />
                        </button>
                      </div>

                      {/* Total */}
                      <div className="text-right">
                        <span className="text-sm font-medium text-stone-100 font-mono">
                          ${item.lineTotal.toFixed(2)}
                        </span>
                      </div>
                    </div>
                  </div>
                </div>
              ))
            )}
          </div>

          {/* Footer */}
          {items.length > 0 && (
            <div className="p-5 border-t border-stone-800 bg-stone-950 space-y-3">
              <div className="flex justify-between items-baseline">
                <span className="text-sm text-stone-400">Subtotal</span>
                <span className="text-xl font-serif font-medium text-stone-100 font-mono">
                  ${subtotal.toFixed(2)}
                </span>
              </div>
              <p className="text-xs text-stone-500">
                Los impuestos, descuentos de cupones y gastos de envío se calculan en el checkout.
              </p>

              <div className="grid grid-cols-2 gap-2.5 pt-1">
                <Link
                  href="/cart"
                  onClick={closeDrawer}
                  className="w-full text-center py-2.5 px-4 rounded-lg bg-stone-800 hover:bg-stone-700 text-stone-200 text-sm font-medium transition-colors border border-stone-700"
                >
                  Ver Carrito
                </Link>
                <Link
                  href="/checkout"
                  onClick={closeDrawer}
                  className="w-full text-center py-2.5 px-4 rounded-lg bg-amber-500 hover:bg-amber-400 text-stone-950 text-sm font-semibold transition-colors flex items-center justify-center gap-1.5 shadow-lg shadow-amber-500/10"
                >
                  Checkout
                  <ArrowRight className="w-4 h-4" />
                </Link>
              </div>
            </div>
          )}
        </div>
      </div>
    </div>
  );
}
