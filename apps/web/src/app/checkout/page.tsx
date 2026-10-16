'use client';

import React, { useEffect, useState } from 'react';
import Link from 'next/link';
import { useRouter } from 'next/navigation';
import { useAuthStore } from '@/stores/auth-store';
import { useCartStore } from '@/stores/cart-store';
import { apiClient } from '@/lib/api-client';
import { CheckoutPreview, CheckoutPreviewRequest } from '@/types/checkout';
import {
  MapPin,
  Truck,
  Tag,
  CheckCircle2,
  AlertCircle,
  ArrowRight,
  ShieldCheck,
  ShoppingBag,
  CreditCard,
  Plus
} from 'lucide-react';

import { ordersApi, CreateOrderRequest } from '@/lib/orders-api';

interface AddressDto {
  id: string;
  label: string;
  recipientName: string;
  phoneNumber: string;
  line1: string;
  line2?: string | null;
  district: string;
  city: string;
  region: string;
  reference?: string | null;
  isDefault: boolean;
}

export default function CheckoutPage() {
  const router = useRouter();
  const { isAuthenticated } = useAuthStore();
  const { cart, fetchCart } = useCartStore();

  const [addresses, setAddresses] = useState<AddressDto[]>([]);
  const [selectedAddressId, setSelectedAddressId] = useState<string | null>(null);
  const [shippingMethod, setShippingMethod] = useState<'standard' | 'express'>('standard');
  const [couponCode, setCouponCode] = useState('');
  const [appliedCouponCode, setAppliedCouponCode] = useState<string | null>(null);

  // Guest Address State
  const [guestAddress, setGuestAddress] = useState({
    recipientName: '',
    phoneNumber: '',
    line1: '',
    district: '',
    city: '',
    region: 'Lima',
  });

  const [preview, setPreview] = useState<CheckoutPreview | null>(null);
  const [loadingPreview, setLoadingPreview] = useState(true);
  const [errorMsg, setErrorMsg] = useState<string | null>(null);
  const [orderProcessing, setOrderProcessing] = useState(false);

  // Fetch Cart and Addresses on mount
  useEffect(() => {
    fetchCart();
    if (isAuthenticated) {
      apiClient
        .get<AddressDto[]>('/api/addresses')
        .then((data) => {
          setAddresses(data);
          const defaultAddr = data.find((a) => a.isDefault) || data[0];
          if (defaultAddr) {
            setSelectedAddressId(defaultAddr.id);
          }
        })
        .catch(() => {
          // ignore or fallback
        });
    }
  }, [fetchCart, isAuthenticated]);

  // Update preview asynchronously whenever parameters change
  useEffect(() => {
    let isCancelled = false;

    async function loadPreview() {
      try {
        const payload: CheckoutPreviewRequest = {
          addressId: isAuthenticated ? selectedAddressId : null,
          shippingMethod,
          couponCode: appliedCouponCode,
        };

        const res = await apiClient.post<CheckoutPreview>('/api/checkout/preview', payload);
        if (!isCancelled) {
          setPreview(res);
          setErrorMsg(null);
          setLoadingPreview(false);
        }
      } catch (err: unknown) {
        if (!isCancelled) {
          const msg = err instanceof Error ? err.message : 'Error al calcular el resumen de compra';
          setErrorMsg(msg);
          setLoadingPreview(false);
        }
      }
    }

    loadPreview();

    return () => {
      isCancelled = true;
    };
  }, [isAuthenticated, selectedAddressId, shippingMethod, appliedCouponCode]);

  const handleApplyCoupon = (e: React.FormEvent) => {
    e.preventDefault();
    if (!couponCode.trim()) return;
    setAppliedCouponCode(couponCode.trim().toUpperCase());
  };

  const handleRemoveCoupon = () => {
    setAppliedCouponCode(null);
    setCouponCode('');
  };

  const handleConfirmOrder = async () => {
    try {
      setOrderProcessing(true);
      setErrorMsg(null);

      // Generate random unique client Idempotency-Key
      const idempotencyKey = `idemp_${Date.now()}_${Math.random().toString(36).substring(2, 9)}`;

      const orderReq: CreateOrderRequest = {
        shippingAddressId: isAuthenticated ? selectedAddressId : null,
        guestShippingAddress: !isAuthenticated ? {
          label: 'Entrega Invitado',
          recipientName: guestAddress.recipientName,
          phoneNumber: guestAddress.phoneNumber,
          line1: guestAddress.line1,
          district: guestAddress.district,
          city: guestAddress.city,
          region: guestAddress.region,
        } : null,
        shippingMethod,
        couponCode: appliedCouponCode,
      };

      const order = await ordersApi.createOrder(orderReq, idempotencyKey);

      // Refresh cart in Zustand store
      await fetchCart();

      // Redirect directly to the order confirmation page
      router.push(`/orders/${order.publicId}`);
    } catch (err: unknown) {
      const msg = err instanceof Error ? err.message : 'Error al procesar el pedido';
      setErrorMsg(msg);
      setOrderProcessing(false);
    }
  };

  if (!cart || cart.items.length === 0) {
    return (
      <div className="min-h-[60vh] flex items-center justify-center px-4 py-16">
        <div className="max-w-md w-full text-center">
          <div className="w-20 h-20 rounded-full bg-amber-500/10 text-amber-600 flex items-center justify-center mx-auto mb-4">
            <ShoppingBag className="w-10 h-10" />
          </div>
          <h2 className="text-2xl font-serif font-bold text-stone-900 mb-2">No tienes productos por pagar</h2>
          <p className="text-sm text-stone-500 mb-6">
            Agrega muebles o artículos de decoración al carrito para iniciar el proceso de checkout.
          </p>
          <Link
            href="/products"
            className="inline-flex items-center gap-2 px-6 py-3 rounded-xl bg-amber-800 text-white font-medium text-sm hover:bg-amber-900 transition-colors"
          >
            Ir al Catálogo
            <ArrowRight className="w-4 h-4" />
          </Link>
        </div>
      </div>
    );
  }

  return (
    <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-10 lg:py-14">
      <div className="mb-8">
        <h1 className="text-3xl font-serif font-bold text-stone-900">Checkout Seguro</h1>
        <p className="text-sm text-stone-500 mt-1">
          Completa tus datos de entrega y confirma tu selección antes de procesar el pago.
        </p>
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-12 gap-8 lg:gap-12 items-start">
        {/* Left Form: Steps */}
        <div className="lg:col-span-7 space-y-6">
          {/* Step 1: Shipping Address */}
          <section className="bg-white rounded-2xl border border-stone-200/80 p-6 shadow-xs">
            <div className="flex items-center gap-3 pb-4 mb-4 border-b border-stone-100">
              <div className="w-8 h-8 rounded-full bg-amber-800 text-white flex items-center justify-center text-sm font-semibold">
                1
              </div>
              <h2 className="text-lg font-serif font-semibold text-stone-900 flex items-center gap-2">
                <MapPin className="w-5 h-5 text-amber-700" />
                Dirección de Entrega
              </h2>
            </div>

            {isAuthenticated ? (
              <div className="space-y-3">
                {addresses.length === 0 ? (
                  <div className="p-4 rounded-xl bg-stone-50 text-center text-xs text-stone-500">
                    <p className="mb-3">Aún no tienes direcciones registradas en tu cuenta.</p>
                    <Link
                      href="/account"
                      className="inline-flex items-center gap-1.5 px-4 py-2 rounded-lg bg-stone-900 text-white text-xs font-semibold hover:bg-stone-800 transition-colors"
                    >
                      <Plus className="w-3.5 h-3.5" />
                      Agregar Dirección en mi Perfil
                    </Link>
                  </div>
                ) : (
                  addresses.map((addr) => (
                    <label
                      key={addr.id}
                      className={`block p-4 rounded-xl border transition-all cursor-pointer ${
                        selectedAddressId === addr.id
                          ? 'border-amber-700 bg-amber-50/30 ring-1 ring-amber-700'
                          : 'border-stone-200 hover:border-stone-300'
                      }`}
                    >
                      <div className="flex items-start gap-3">
                        <input
                          type="radio"
                          name="selected_address"
                          checked={selectedAddressId === addr.id}
                          onChange={() => setSelectedAddressId(addr.id)}
                          className="mt-1 text-amber-700 focus:ring-amber-700"
                        />
                        <div className="text-xs space-y-0.5">
                          <div className="flex items-center gap-2">
                            <span className="font-semibold text-stone-900">{addr.label}</span>
                            {addr.isDefault && (
                              <span className="px-1.5 py-0.5 rounded text-[10px] bg-stone-200 text-stone-700 font-medium">
                                Predeterminada
                              </span>
                            )}
                          </div>
                          <p className="text-stone-700 font-medium">{addr.recipientName} • {addr.phoneNumber}</p>
                          <p className="text-stone-500">{addr.line1}{addr.line2 ? `, ${addr.line2}` : ''}</p>
                          <p className="text-stone-500">{addr.district}, {addr.city}, {addr.region}</p>
                        </div>
                      </div>
                    </label>
                  ))
                )}
              </div>
            ) : (
              /* Guest Address Form */
              <div className="space-y-3">
                <div className="p-3 bg-amber-50/60 border border-amber-200/60 rounded-xl text-xs text-amber-800 flex items-center justify-between">
                  <span>Estás comprando como invitado.</span>
                  <Link href="/login?redirect=/checkout" className="underline font-semibold hover:text-amber-900">
                    Iniciar sesión para usar direcciones guardadas
                  </Link>
                </div>

                <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
                  <div>
                    <label className="block text-xs font-semibold text-stone-700 mb-1">Nombre Completo</label>
                    <input
                      type="text"
                      placeholder="Ej: Sofia Mendoza"
                      value={guestAddress.recipientName}
                      onChange={(e) => setGuestAddress({ ...guestAddress, recipientName: e.target.value })}
                      className="w-full text-xs rounded-lg border border-stone-300 p-2.5 focus:border-amber-700 focus:outline-none"
                    />
                  </div>
                  <div>
                    <label className="block text-xs font-semibold text-stone-700 mb-1">Teléfono</label>
                    <input
                      type="tel"
                      placeholder="Ej: +51 987 654 321"
                      value={guestAddress.phoneNumber}
                      onChange={(e) => setGuestAddress({ ...guestAddress, phoneNumber: e.target.value })}
                      className="w-full text-xs rounded-lg border border-stone-300 p-2.5 focus:border-amber-700 focus:outline-none"
                    />
                  </div>
                </div>

                <div>
                  <label className="block text-xs font-semibold text-stone-700 mb-1">Dirección y Número</label>
                  <input
                    type="text"
                    placeholder="Ej: Av. Las Camelias 450, Dpto 502"
                    value={guestAddress.line1}
                    onChange={(e) => setGuestAddress({ ...guestAddress, line1: e.target.value })}
                    className="w-full text-xs rounded-lg border border-stone-300 p-2.5 focus:border-amber-700 focus:outline-none"
                  />
                </div>

                <div className="grid grid-cols-1 sm:grid-cols-3 gap-3">
                  <div>
                    <label className="block text-xs font-semibold text-stone-700 mb-1">Distrito</label>
                    <input
                      type="text"
                      placeholder="Ej: San Isidro"
                      value={guestAddress.district}
                      onChange={(e) => setGuestAddress({ ...guestAddress, district: e.target.value })}
                      className="w-full text-xs rounded-lg border border-stone-300 p-2.5 focus:border-amber-700 focus:outline-none"
                    />
                  </div>
                  <div>
                    <label className="block text-xs font-semibold text-stone-700 mb-1">Ciudad</label>
                    <input
                      type="text"
                      placeholder="Ej: Lima"
                      value={guestAddress.city}
                      onChange={(e) => setGuestAddress({ ...guestAddress, city: e.target.value })}
                      className="w-full text-xs rounded-lg border border-stone-300 p-2.5 focus:border-amber-700 focus:outline-none"
                    />
                  </div>
                  <div>
                    <label className="block text-xs font-semibold text-stone-700 mb-1">Departamento / Región</label>
                    <input
                      type="text"
                      value={guestAddress.region}
                      onChange={(e) => setGuestAddress({ ...guestAddress, region: e.target.value })}
                      className="w-full text-xs rounded-lg border border-stone-300 p-2.5 focus:border-amber-700 focus:outline-none"
                    />
                  </div>
                </div>
              </div>
            )}
          </section>

          {/* Step 2: Shipping Method */}
          <section className="bg-white rounded-2xl border border-stone-200/80 p-6 shadow-xs">
            <div className="flex items-center gap-3 pb-4 mb-4 border-b border-stone-100">
              <div className="w-8 h-8 rounded-full bg-amber-800 text-white flex items-center justify-center text-sm font-semibold">
                2
              </div>
              <h2 className="text-lg font-serif font-semibold text-stone-900 flex items-center gap-2">
                <Truck className="w-5 h-5 text-amber-700" />
                Método de Envío
              </h2>
            </div>

            <div className="space-y-3">
              <label
                className={`block p-4 rounded-xl border transition-all cursor-pointer ${
                  shippingMethod === 'standard'
                    ? 'border-amber-700 bg-amber-50/30 ring-1 ring-amber-700'
                    : 'border-stone-200 hover:border-stone-300'
                }`}
              >
                <div className="flex items-center justify-between">
                  <div className="flex items-center gap-3">
                    <input
                      type="radio"
                      name="shipping_method"
                      value="standard"
                      checked={shippingMethod === 'standard'}
                      onChange={() => setShippingMethod('standard')}
                      className="text-amber-700 focus:ring-amber-700"
                    />
                    <div>
                      <p className="text-xs font-semibold text-stone-900">Envío Estándar a Domicilio</p>
                      <p className="text-[11px] text-stone-500">Entrega de 3 a 5 días hábiles con seguimiento</p>
                    </div>
                  </div>
                  <span className="font-mono text-xs font-semibold text-stone-900">
                    {preview?.shipping.isFree ? (
                      <span className="text-emerald-700 font-bold">GRATIS</span>
                    ) : (
                      '$15.00'
                    )}
                  </span>
                </div>
              </label>

              <label
                className={`block p-4 rounded-xl border transition-all cursor-pointer ${
                  shippingMethod === 'express'
                    ? 'border-amber-700 bg-amber-50/30 ring-1 ring-amber-700'
                    : 'border-stone-200 hover:border-stone-300'
                }`}
              >
                <div className="flex items-center justify-between">
                  <div className="flex items-center gap-3">
                    <input
                      type="radio"
                      name="shipping_method"
                      value="express"
                      checked={shippingMethod === 'express'}
                      onChange={() => setShippingMethod('express')}
                      className="text-amber-700 focus:ring-amber-700"
                    />
                    <div>
                      <p className="text-xs font-semibold text-stone-900">Envío Express Prioritario</p>
                      <p className="text-[11px] text-stone-500">Entrega asegurada en 24 a 48 horas con chofer dedicado</p>
                    </div>
                  </div>
                  <span className="font-mono text-xs font-semibold text-stone-900">$45.00</span>
                </div>
              </label>
            </div>
          </section>

          {/* Step 3: Coupon Discount */}
          <section className="bg-white rounded-2xl border border-stone-200/80 p-6 shadow-xs">
            <div className="flex items-center gap-3 pb-4 mb-4 border-b border-stone-100">
              <div className="w-8 h-8 rounded-full bg-amber-800 text-white flex items-center justify-center text-sm font-semibold">
                3
              </div>
              <h2 className="text-lg font-serif font-semibold text-stone-900 flex items-center gap-2">
                <Tag className="w-5 h-5 text-amber-700" />
                Cupón Promocional
              </h2>
            </div>

            {preview?.appliedCoupon ? (
              <div className="flex items-center justify-between p-3.5 bg-emerald-50 border border-emerald-200/80 rounded-xl">
                <div className="flex items-center gap-2">
                  <CheckCircle2 className="w-5 h-5 text-emerald-600 shrink-0" />
                  <div>
                    <span className="text-xs font-bold text-emerald-900 font-mono">
                      {preview.appliedCoupon.code}
                    </span>
                    <p className="text-[11px] text-emerald-700">{preview.appliedCoupon.description}</p>
                  </div>
                </div>
                <button
                  type="button"
                  onClick={handleRemoveCoupon}
                  className="text-xs text-rose-600 hover:text-rose-800 underline font-medium"
                >
                  Quitar
                </button>
              </div>
            ) : (
              <form onSubmit={handleApplyCoupon} className="flex gap-2">
                <input
                  type="text"
                  placeholder="Ej: BIENVENIDA10, HOGAR50, VERANO15"
                  value={couponCode}
                  onChange={(e) => setCouponCode(e.target.value.toUpperCase())}
                  className="flex-1 rounded-xl border border-stone-300 px-3.5 py-2 text-xs font-mono uppercase placeholder:text-stone-400 focus:border-amber-700 focus:outline-none"
                />
                <button
                  type="submit"
                  disabled={!couponCode.trim() || loadingPreview}
                  className="px-4 py-2 rounded-xl bg-stone-900 text-white text-xs font-semibold hover:bg-stone-800 disabled:opacity-40 transition-colors"
                >
                  Aplicar
                </button>
              </form>
            )}
          </section>
        </div>

        {/* Right Form: Live Summary & Confirmation */}
        <div className="lg:col-span-5 space-y-6">
          <div className="bg-white rounded-2xl border border-stone-200/80 p-6 shadow-xs space-y-6">
            <h2 className="text-lg font-serif font-bold text-stone-900 pb-4 border-b border-stone-100 flex items-center justify-between">
              <span>Resumen de la Orden</span>
              <span className="text-xs font-mono font-normal text-stone-400">
                100% Verificado por el Servidor
              </span>
            </h2>

            {/* General Network / Server Error */}
            {errorMsg && (
              <div className="p-3.5 bg-rose-50 border border-rose-200/80 rounded-xl flex items-center gap-2 text-xs text-rose-800">
                <AlertCircle className="w-4 h-4 text-rose-600 shrink-0" />
                <span>{errorMsg}</span>
              </div>
            )}

            {/* Error or Warning Alerts from Server */}
            {preview?.validationErrors && preview.validationErrors.length > 0 && (
              <div className="p-3.5 bg-rose-50 border border-rose-200/80 rounded-xl space-y-1.5 text-xs text-rose-800">
                <div className="flex items-center gap-1.5 font-semibold text-rose-900">
                  <AlertCircle className="w-4 h-4 text-rose-600 shrink-0" />
                  Atención requerida:
                </div>
                {preview.validationErrors.map((err, idx) => (
                  <p key={idx} className="pl-5 text-[11px] list-disc">• {err}</p>
                ))}
              </div>
            )}

            {/* Items Breakdown */}
            <div className="space-y-3 max-h-72 overflow-y-auto pr-1">
              {preview?.items.map((item) => (
                <div key={item.variantId} className="flex items-center justify-between text-xs py-1 border-b border-stone-100 last:border-0">
                  <div className="flex-1 min-w-0 pr-3">
                    <p className="font-semibold text-stone-900 truncate">{item.productName}</p>
                    <p className="text-[11px] text-stone-500 font-mono">
                      {item.variantName} • {item.quantity}x ${item.unitPrice.toFixed(2)}
                    </p>
                    {!item.isAvailable && (
                      <span className="text-[10px] text-rose-600 font-medium">
                        {item.errorMessage || 'No disponible'}
                      </span>
                    )}
                  </div>
                  <span className="font-mono font-medium text-stone-900 shrink-0">
                    ${item.lineTotal.toFixed(2)}
                  </span>
                </div>
              ))}
            </div>

            {/* Monetary Breakdown */}
            <div className="pt-4 border-t border-stone-100 space-y-2 text-xs">
              <div className="flex justify-between text-stone-600">
                <span>Subtotal</span>
                <span className="font-mono font-medium text-stone-900">
                  ${(preview?.subtotal || 0).toFixed(2)}
                </span>
              </div>

              {preview && preview.discountTotal > 0 && (
                <div className="flex justify-between text-emerald-700 font-medium">
                  <span>Descuento Cupón ({preview.appliedCoupon?.code})</span>
                  <span className="font-mono">-${preview.discountTotal.toFixed(2)}</span>
                </div>
              )}

              <div className="flex justify-between text-stone-600">
                <span>Envío ({preview?.shipping.name})</span>
                <span className="font-mono font-medium text-stone-900">
                  {preview?.shipping.isFree ? (
                    <span className="text-emerald-700 font-bold">GRATIS</span>
                  ) : (
                    `$${(preview?.shipping.cost || 0).toFixed(2)}`
                  )}
                </span>
              </div>

              <div className="pt-3 border-t border-stone-200 flex justify-between items-baseline text-sm">
                <span className="font-bold text-stone-900">Total a Pagar</span>
                <span className="text-2xl font-serif font-bold text-stone-900 font-mono">
                  ${(preview?.total || 0).toFixed(2)}
                </span>
              </div>
            </div>

            {/* Confirmation CTA */}
            <button
              type="button"
              onClick={handleConfirmOrder}
              disabled={loadingPreview || orderProcessing || !preview?.canProceedToOrder}
              className="w-full py-4 px-6 rounded-xl bg-amber-800 hover:bg-amber-900 text-white font-semibold text-sm transition-all shadow-md shadow-amber-800/20 disabled:opacity-50 disabled:cursor-not-allowed flex items-center justify-center gap-2"
            >
              <CreditCard className="w-4 h-4" />
              {orderProcessing ? 'Validando orden con el servidor...' : 'Confirmar Pedido y Continuar'}
            </button>

            <div className="text-[11px] text-stone-400 text-center space-y-1">
              <p className="flex items-center justify-center gap-1">
                <ShieldCheck className="w-3.5 h-3.5 text-emerald-600" />
                Transacción protegida con cifrado SSL y recálculo anti-fraude.
              </p>
              <p>Ningún precio ni total es alterable por el navegador del cliente.</p>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}
