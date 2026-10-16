'use client';

import React, { useEffect } from 'react';
import Link from 'next/link';
import Image from 'next/image';
import { useWishlistStore } from '@/stores/wishlist-store';
import { useAuthStore } from '@/stores/auth-store';
import {
  Heart,
  ArrowLeft,
  Trash2,
  ExternalLink,
  ShoppingBag,
  Sparkles,
} from 'lucide-react';

export default function WishlistPage() {
  const { isAuthenticated } = useAuthStore();
  const { items, isLoading, fetchWishlist, removeItem } = useWishlistStore();

  useEffect(() => {
    if (isAuthenticated) {
      fetchWishlist();
    }
  }, [isAuthenticated, fetchWishlist]);

  if (!isAuthenticated) {
    return (
      <div className="mx-auto max-w-4xl px-4 py-16 text-center sm:px-6 lg:px-8">
        <div className="mx-auto flex h-16 w-16 items-center justify-center rounded-full bg-rose-100 text-rose-600 dark:bg-rose-950/60 dark:text-rose-400">
          <Heart className="h-8 w-8" />
        </div>
        <h1 className="mt-4 text-2xl font-bold tracking-tight text-zinc-900 sm:text-3xl dark:text-zinc-100">
          Inicia sesión para ver tus Favoritos
        </h1>
        <p className="mt-2 text-sm text-zinc-500 dark:text-zinc-400">
          Guarda tus piezas preferidas y consúltalas en cualquier momento desde cualquier dispositivo.
        </p>
        <div className="mt-6">
          <Link
            href="/login?redirect=/account/wishlist"
            className="inline-flex items-center justify-center rounded-xl bg-amber-600 px-6 py-3 text-sm font-semibold text-white shadow-sm hover:bg-amber-500 transition-colors"
          >
            Iniciar Sesión
          </Link>
        </div>
      </div>
    );
  }

  return (
    <div className="mx-auto max-w-7xl px-4 py-8 sm:px-6 lg:px-8">
      {/* Header & Navigation */}
      <div className="mb-8 flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
        <div>
          <Link
            href="/account"
            className="inline-flex items-center gap-2 text-xs font-medium text-zinc-500 hover:text-amber-600 transition-colors dark:text-zinc-400"
          >
            <ArrowLeft className="h-3.5 w-3.5" />
            Volver a Mi Cuenta
          </Link>
          <div className="mt-2 flex items-center gap-3">
            <h1 className="text-2xl font-bold tracking-tight text-zinc-900 sm:text-3xl dark:text-zinc-100">
              Mi Lista de Deseos
            </h1>
            <span className="rounded-full bg-rose-100 px-3 py-1 text-xs font-semibold text-rose-700 dark:bg-rose-950/60 dark:text-rose-400">
              {items.length} {items.length === 1 ? 'pieza' : 'piezas'}
            </span>
          </div>
        </div>

        <Link
          href="/products"
          className="inline-flex items-center gap-2 rounded-xl border border-zinc-200 bg-white px-4 py-2 text-xs font-semibold text-zinc-700 shadow-2xs hover:bg-zinc-50 transition-colors dark:border-zinc-800 dark:bg-zinc-900 dark:text-zinc-300"
        >
          <ShoppingBag className="h-3.5 w-3.5 text-amber-600" />
          Seguir Explorando
        </Link>
      </div>

      {/* Content */}
      {isLoading && items.length === 0 ? (
        <div className="flex h-64 items-center justify-center">
          <div className="h-8 w-8 animate-spin rounded-full border-2 border-amber-600 border-t-transparent" />
        </div>
      ) : items.length === 0 ? (
        <div className="rounded-3xl border border-dashed border-zinc-200 bg-zinc-50/50 p-12 text-center dark:border-zinc-800 dark:bg-zinc-900/30">
          <div className="mx-auto flex h-14 w-14 items-center justify-center rounded-full bg-rose-50 text-rose-500 dark:bg-rose-950/40">
            <Heart className="h-7 w-7" />
          </div>
          <h2 className="mt-4 text-lg font-semibold text-zinc-900 dark:text-zinc-100">
            Aún no has guardado favoritos
          </h2>
          <p className="mx-auto mt-2 max-w-sm text-sm text-zinc-500 dark:text-zinc-400">
            Explora nuestro catálogo de muebles y ambientes interactivos. Haz clic en el corazón de cualquier pieza para guardarla aquí.
          </p>
          <div className="mt-6 flex flex-wrap items-center justify-center gap-3">
            <Link
              href="/products"
              className="rounded-xl bg-amber-600 px-5 py-2.5 text-xs font-semibold text-white shadow-sm hover:bg-amber-500 transition-colors"
            >
              Ver Catálogo Completo
            </Link>
            <Link
              href="/scenes"
              className="rounded-xl border border-zinc-300 bg-white px-5 py-2.5 text-xs font-semibold text-zinc-700 shadow-2xs hover:bg-zinc-50 transition-colors dark:border-zinc-700 dark:bg-zinc-800 dark:text-zinc-200"
            >
              Explorar Ambientes
            </Link>
          </div>
        </div>
      ) : (
        <div className="grid grid-cols-1 gap-6 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4">
          {items.map((item) => (
            <div
              key={item.id}
              className="group flex flex-col overflow-hidden rounded-2xl border border-zinc-200/80 bg-white transition-all duration-300 hover:shadow-lg dark:border-zinc-800 dark:bg-zinc-900"
            >
              {/* Image Container */}
              <div className="relative aspect-4/3 w-full overflow-hidden bg-zinc-100 dark:bg-zinc-800">
                {item.primaryImageUrl ? (
                  <Image
                    src={item.primaryImageUrl}
                    alt={item.productName}
                    fill
                    sizes="(max-width: 640px) 100vw, (max-width: 1024px) 50vw, 25vw"
                    className="object-cover transition-transform duration-500 group-hover:scale-105"
                  />
                ) : (
                  <div className="flex h-full w-full items-center justify-center text-xs text-zinc-400">
                    Sin fotografía
                  </div>
                )}

                {/* Remove from wishlist button */}
                <button
                  type="button"
                  onClick={() => removeItem(item.productId)}
                  aria-label={`Eliminar ${item.productName} de favoritos`}
                  className="absolute top-3 right-3 flex h-9 w-9 items-center justify-center rounded-full bg-white/90 text-zinc-500 shadow-sm backdrop-blur-xs transition-colors hover:bg-rose-50 hover:text-rose-600 dark:bg-zinc-900/90 dark:text-zinc-400 dark:hover:bg-rose-950/60 dark:hover:text-rose-400"
                >
                  <Trash2 className="h-4 w-4" />
                </button>

                {/* Category badge */}
                {item.categoryName && (
                  <div className="absolute bottom-3 left-3 rounded-md bg-white/90 px-2 py-0.5 text-[11px] font-medium text-zinc-800 shadow-2xs backdrop-blur-xs dark:bg-zinc-900/90 dark:text-zinc-200">
                    {item.categoryName}
                  </div>
                )}
              </div>

              {/* Product Info */}
              <div className="flex flex-1 flex-col p-4">
                <Link
                  href={`/products/${item.productSlug}`}
                  className="font-semibold text-zinc-900 hover:text-amber-600 transition-colors line-clamp-1 dark:text-zinc-100"
                >
                  {item.productName}
                </Link>

                <div className="mt-3 flex items-baseline justify-between">
                  <span className="text-lg font-bold text-zinc-900 dark:text-white">
                    {new Intl.NumberFormat('es-PE', {
                      style: 'currency',
                      currency: 'USD',
                    }).format(item.basePrice)}
                  </span>

                  <span
                    className={`text-[11px] font-semibold ${
                      item.isAvailable
                        ? 'text-emerald-600 dark:text-emerald-400'
                        : 'text-rose-600 dark:text-rose-400'
                    }`}
                  >
                    {item.isAvailable ? 'Disponible' : 'Agotado'}
                  </span>
                </div>

                {/* CTA */}
                <div className="mt-4 pt-3 border-t border-zinc-100 dark:border-zinc-800/80">
                  <Link
                    href={`/products/${item.productSlug}`}
                    className="flex w-full items-center justify-center gap-1.5 rounded-xl bg-zinc-900 px-4 py-2 text-xs font-semibold text-white hover:bg-amber-600 transition-colors dark:bg-zinc-100 dark:text-zinc-900 dark:hover:bg-amber-500"
                  >
                    Ver Detalles
                    <ExternalLink className="h-3 w-3" />
                  </Link>
                </div>
              </div>
            </div>
          ))}
        </div>
      )}
    </div>
  );
}
