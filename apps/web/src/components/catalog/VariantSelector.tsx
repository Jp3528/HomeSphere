"use client";

import { useState } from "react";
import { ProductVariant } from "@/types/catalog";
import { ShoppingBag, Check, ShieldCheck, Truck, RefreshCw } from "lucide-react";

interface VariantSelectorProps {
  variants: ProductVariant[];
  basePrice: number;
  productName: string;
}

export function VariantSelector({ variants, basePrice }: VariantSelectorProps) {
  const activeVariants = variants.filter((v) => v.isActive);
  const initialVariant = activeVariants[0] || variants[0];

  const [selectedVariantId, setSelectedVariantId] = useState<string>(
    initialVariant ? initialVariant.id : ""
  );
  const [quantity, setQuantity] = useState(1);
  const [addedFeedback, setAddedFeedback] = useState(false);

  const selectedVariant =
    activeVariants.find((v) => v.id === selectedVariantId) || initialVariant;

  const currentPrice = selectedVariant ? selectedVariant.price : basePrice;
  const currentStock = selectedVariant ? selectedVariant.stockQuantity : 0;
  const isOutOfStock = currentStock <= 0;

  // Format currency
  const formattedPrice = new Intl.NumberFormat("es-PE", {
    style: "currency",
    currency: "USD",
  }).format(currentPrice);

  // Parse attributes
  let attributesMap: Record<string, string> = {};
  if (selectedVariant?.attributesJson) {
    try {
      attributesMap = JSON.parse(selectedVariant.attributesJson);
    } catch {
      // Invalid JSON fallback
    }
  }

  const handleAddToCart = () => {
    if (isOutOfStock) return;

    setAddedFeedback(true);
    setTimeout(() => {
      setAddedFeedback(false);
    }, 2500);
  };

  return (
    <div className="space-y-6">
      {/* Price & Stock Header */}
      <div className="flex items-baseline justify-between border-b border-zinc-200/80 pb-4 dark:border-zinc-800">
        <div>
          <span className="text-3xl font-extrabold text-zinc-900 dark:text-zinc-50">
            {formattedPrice}
          </span>
          <span className="ml-2 text-xs text-zinc-400">Impuestos incluidos</span>
        </div>

        <div>
          {isOutOfStock ? (
            <span className="inline-flex items-center gap-1.5 rounded-full bg-rose-50 px-3 py-1 text-xs font-semibold text-rose-700 dark:bg-rose-950/40 dark:text-rose-300">
              <span className="h-2 w-2 rounded-full bg-rose-500" />
              Agotado
            </span>
          ) : currentStock <= 5 ? (
            <span className="inline-flex items-center gap-1.5 rounded-full bg-amber-50 px-3 py-1 text-xs font-semibold text-amber-700 dark:bg-amber-950/40 dark:text-amber-300">
              <span className="h-2 w-2 rounded-full bg-amber-500" />
              ¡Últimas {currentStock} unidades!
            </span>
          ) : (
            <span className="inline-flex items-center gap-1.5 rounded-full bg-emerald-50 px-3 py-1 text-xs font-semibold text-emerald-700 dark:bg-emerald-950/40 dark:text-emerald-300">
              <span className="h-2 w-2 rounded-full bg-emerald-500" />
              En stock ({currentStock} disponibles)
            </span>
          )}
        </div>
      </div>

      {/* Variants Selection */}
      {activeVariants.length > 1 && (
        <div className="space-y-3">
          <label className="text-xs font-bold uppercase tracking-wider text-zinc-500 dark:text-zinc-400">
            Selecciona una Variante / Acabado:
          </label>
          <div className="grid grid-cols-1 gap-2.5 sm:grid-cols-2">
            {activeVariants.map((variant) => {
              const isSelected = variant.id === selectedVariant?.id;
              const isVarOutOfStock = variant.stockQuantity <= 0;

              return (
                <button
                  key={variant.id}
                  type="button"
                  onClick={() => {
                    setSelectedVariantId(variant.id);
                    setQuantity(1);
                  }}
                  className={`flex flex-col rounded-xl border p-3 text-left transition-all ${
                    isSelected
                      ? "border-amber-600 bg-amber-50/50 shadow-xs ring-2 ring-amber-600/20 dark:border-amber-500 dark:bg-amber-950/20"
                      : "border-zinc-200 bg-white hover:border-zinc-300 dark:border-zinc-800 dark:bg-zinc-900"
                  }`}
                >
                  <div className="flex items-center justify-between">
                    <span className="text-sm font-semibold text-zinc-900 dark:text-zinc-100">
                      {variant.name}
                    </span>
                    {isSelected && (
                      <Check className="h-4 w-4 text-amber-600 dark:text-amber-400" />
                    )}
                  </div>

                  <div className="mt-1 flex items-center justify-between text-xs text-zinc-500 dark:text-zinc-400">
                    <span>
                      {new Intl.NumberFormat("es-PE", {
                        style: "currency",
                        currency: "USD",
                      }).format(variant.price)}
                    </span>
                    <span className={isVarOutOfStock ? "text-rose-500 font-medium" : ""}>
                      {isVarOutOfStock ? "Agotado" : `${variant.stockQuantity} unid.`}
                    </span>
                  </div>
                </button>
              );
            })}
          </div>
        </div>
      )}

      {/* Attributes Specifications Table */}
      {Object.keys(attributesMap).length > 0 && (
        <div className="rounded-2xl border border-zinc-200/80 bg-zinc-50/60 p-4 dark:border-zinc-800 dark:bg-zinc-900/40">
          <h4 className="text-xs font-bold uppercase tracking-wider text-zinc-400 mb-2">
            Especificaciones del Modelo
          </h4>
          <dl className="grid grid-cols-1 gap-x-4 gap-y-2 text-xs sm:grid-cols-2">
            {Object.entries(attributesMap).map(([key, val]) => (
              <div key={key} className="flex justify-between border-b border-zinc-200/40 pb-1 dark:border-zinc-800">
                <dt className="text-zinc-500 capitalize">{key}:</dt>
                <dd className="font-medium text-zinc-800 dark:text-zinc-200">{val}</dd>
              </div>
            ))}
            {selectedVariant?.sku && (
              <div className="flex justify-between border-b border-zinc-200/40 pb-1 dark:border-zinc-800">
                <dt className="text-zinc-500">SKU:</dt>
                <dd className="font-mono text-zinc-800 dark:text-zinc-200">{selectedVariant.sku}</dd>
              </div>
            )}
          </dl>
        </div>
      )}

      {/* Quantity & CTA Row */}
      <div className="flex flex-col gap-3 sm:flex-row sm:items-center">
        {/* Quantity selector */}
        <div className="flex h-12 items-center rounded-xl border border-zinc-200 bg-white dark:border-zinc-800 dark:bg-zinc-900">
          <button
            type="button"
            onClick={() => setQuantity((q) => Math.max(1, q - 1))}
            disabled={quantity <= 1 || isOutOfStock}
            className="flex h-full w-10 items-center justify-center text-zinc-500 hover:text-zinc-900 disabled:opacity-30 dark:hover:text-zinc-100"
          >
            -
          </button>
          <span className="w-12 text-center text-sm font-semibold text-zinc-800 dark:text-zinc-200">
            {quantity}
          </span>
          <button
            type="button"
            onClick={() => setQuantity((q) => Math.min(currentStock, q + 1))}
            disabled={quantity >= currentStock || isOutOfStock}
            className="flex h-full w-10 items-center justify-center text-zinc-500 hover:text-zinc-900 disabled:opacity-30 dark:hover:text-zinc-100"
          >
            +
          </button>
        </div>

        {/* Add to cart CTA */}
        <button
          type="button"
          onClick={handleAddToCart}
          disabled={isOutOfStock}
          className={`flex h-12 flex-1 items-center justify-center gap-2 rounded-xl text-sm font-bold shadow-md transition-all ${
            addedFeedback
              ? "bg-emerald-600 text-white"
              : isOutOfStock
              ? "cursor-not-allowed bg-zinc-200 text-zinc-400 dark:bg-zinc-800 dark:text-zinc-600"
              : "bg-zinc-900 text-white hover:bg-amber-600 active:scale-98 dark:bg-zinc-100 dark:text-zinc-900 dark:hover:bg-amber-400"
          }`}
        >
          {addedFeedback ? (
            <>
              <Check className="h-5 w-5" />
              ¡Añadido a la bolsa!
            </>
          ) : (
            <>
              <ShoppingBag className="h-5 w-5" />
              {isOutOfStock ? "Producto Agotado" : "Añadir a la Bolsa"}
            </>
          )}
        </button>
      </div>

      {/* Trust & Guarantee Chips */}
      <div className="grid grid-cols-1 gap-3 pt-4 border-t border-zinc-100 dark:border-zinc-800 sm:grid-cols-3">
        <div className="flex items-center gap-2 text-xs text-zinc-600 dark:text-zinc-400">
          <Truck className="h-4 w-4 text-amber-600 shrink-0" />
          <span>Envío asegurado en 2-4 días</span>
        </div>
        <div className="flex items-center gap-2 text-xs text-zinc-600 dark:text-zinc-400">
          <ShieldCheck className="h-4 w-4 text-amber-600 shrink-0" />
          <span>Garantía oficial de 2 años</span>
        </div>
        <div className="flex items-center gap-2 text-xs text-zinc-600 dark:text-zinc-400">
          <RefreshCw className="h-4 w-4 text-amber-600 shrink-0" />
          <span>Devolución gratuita (30 días)</span>
        </div>
      </div>
    </div>
  );
}
