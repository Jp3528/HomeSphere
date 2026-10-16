"use client";

import { useState } from "react";
import Image from "next/image";
import Link from "next/link";
import { RoomSceneDetail } from "@/types/scenes";
import { SceneCanvas } from "./SceneCanvas";
import { useCartStore } from "@/stores/cart-store";
import { ArrowRight, ShoppingBag, CheckCircle, Loader2, Sparkles, Tag } from "lucide-react";

interface InteractiveSceneViewProps {
  scene: RoomSceneDetail;
}

export function InteractiveSceneView({ scene }: InteractiveSceneViewProps) {
  const [activeHotspotId, setActiveHotspotId] = useState<string | null>(null);
  const [isAddingAll, setIsAddingAll] = useState(false);
  const [addedSuccess, setAddedSuccess] = useState(false);
  const { addItem, openDrawer } = useCartStore();

  const availableHotspots = scene.hotspots.filter(
    (h) => h.isAvailable !== false && !!h.productVariantId
  );

  const availableTotalPrice = availableHotspots.reduce((sum, h) => {
    const price = h.productVariantPrice ?? h.productBasePrice;
    return sum + price;
  }, 0);

  const formattedTotalPrice = new Intl.NumberFormat("es-PE", {
    style: "currency",
    currency: "USD",
  }).format(availableTotalPrice);

  const handleBuyWholeRoom = async () => {
    if (availableHotspots.length === 0 || isAddingAll) return;

    setIsAddingAll(true);
    setAddedSuccess(false);

    try {
      for (const hotspot of availableHotspots) {
        if (hotspot.productVariantId) {
          await addItem(hotspot.productVariantId, 1);
        }
      }
      setAddedSuccess(true);
      openDrawer();
    } catch {
      // Handled in cart store
    } finally {
      setIsAddingAll(false);
    }
  };

  return (
    <div className="grid grid-cols-1 gap-10 lg:grid-cols-12">
      {/* Left Column: Interactive Canvas (8 cols) */}
      <div className="lg:col-span-8 space-y-4">
        <SceneCanvas
          imageUrl={scene.imageUrl}
          sceneTitle={scene.title}
          hotspots={scene.hotspots}
          activeHotspotId={activeHotspotId}
          onSelectHotspot={(id) => setActiveHotspotId(id)}
        />

        <div className="flex flex-wrap items-center justify-between gap-2 text-xs text-zinc-500 dark:text-zinc-400">
          <span>
            Haz clic o pulsa en cualquiera de los números sobre la imagen para explorar la pieza y sus acabados.
          </span>
          {scene.roomType && (
            <span className="inline-flex items-center gap-1 rounded-full bg-zinc-100 px-2.5 py-0.5 text-[11px] font-medium text-zinc-700 dark:bg-zinc-800 dark:text-zinc-300">
              <Tag className="h-3 w-3" />
              {scene.roomType}
            </span>
          )}
        </div>
      </div>

      {/* Right Column: Curated Products List (4 cols) */}
      <div className="lg:col-span-4 flex flex-col space-y-6">
        <div className="rounded-3xl border border-zinc-200/80 bg-white p-6 shadow-xs dark:border-zinc-800 dark:bg-zinc-900">
          <div className="flex items-center justify-between border-b border-zinc-100 pb-4 dark:border-zinc-800">
            <div>
              <h3 className="text-base font-bold text-zinc-900 dark:text-zinc-50">
                Piezas del Ambiente
              </h3>
              <p className="text-xs text-zinc-500 dark:text-zinc-400">
                {availableHotspots.length} de {scene.hotspots.length} piezas disponibles
              </p>
            </div>

            <div className="text-right">
              <span className="text-[10px] text-zinc-400 block">Total Estilismo</span>
              <span className="text-sm font-extrabold text-amber-700 dark:text-amber-400">
                {formattedTotalPrice}
              </span>
            </div>
          </div>

          {/* List of Hotspot Products */}
          <div className="mt-4 flex flex-col gap-3">
            {scene.hotspots.map((h, idx) => {
              const isSelected = h.id === activeHotspotId;
              const isAvailable = h.isAvailable !== false;
              const price = h.productVariantPrice ?? h.productBasePrice;
              const formattedPrice = new Intl.NumberFormat("es-PE", {
                style: "currency",
                currency: "USD",
              }).format(price);

              return (
                <div
                  key={h.id}
                  onClick={() => setActiveHotspotId(isSelected ? null : h.id)}
                  onMouseEnter={() => setActiveHotspotId(h.id)}
                  className={`flex cursor-pointer items-center gap-3.5 rounded-2xl border p-3 transition-all ${
                    !isAvailable
                      ? "opacity-60 border-zinc-200/60 bg-zinc-50/30 dark:border-zinc-800/60 dark:bg-zinc-900/40"
                      : isSelected
                      ? "border-amber-600 bg-amber-50/50 shadow-xs ring-2 ring-amber-600/20 dark:border-amber-500 dark:bg-amber-950/20"
                      : "border-zinc-200/80 bg-zinc-50/50 hover:border-zinc-300 dark:border-zinc-800 dark:bg-zinc-800/40"
                  }`}
                >
                  {/* Pin badge number */}
                  <div
                    className={`flex h-7 w-7 shrink-0 items-center justify-center rounded-full text-xs font-bold transition-colors ${
                      !isAvailable
                        ? "bg-zinc-300 text-zinc-500 dark:bg-zinc-800 dark:text-zinc-500"
                        : isSelected
                        ? "bg-amber-600 text-white"
                        : "bg-zinc-200 text-zinc-700 dark:bg-zinc-700 dark:text-zinc-200"
                    }`}
                  >
                    {idx + 1}
                  </div>

                  {/* Thumbnail */}
                  <div className="relative aspect-square h-12 w-12 shrink-0 overflow-hidden rounded-xl bg-zinc-100 dark:bg-zinc-800">
                    {h.productPrimaryImageUrl ? (
                      <Image
                        src={h.productPrimaryImageUrl}
                        alt={h.productName}
                        fill
                        sizes="48px"
                        className="object-cover"
                      />
                    ) : (
                      <div className="flex h-full w-full items-center justify-center text-[9px] text-zinc-400">
                        Foto
                      </div>
                    )}
                  </div>

                  {/* Meta */}
                  <div className="flex flex-1 flex-col overflow-hidden">
                    <span className="line-clamp-1 text-xs font-semibold text-zinc-900 dark:text-zinc-100">
                      {h.title || h.productName}
                    </span>
                    <div className="flex items-center gap-2">
                      <span className="text-xs font-extrabold text-amber-700 dark:text-amber-400">
                        {formattedPrice}
                      </span>
                      {!isAvailable && (
                        <span className="text-[10px] font-medium text-rose-500">
                          {h.unavailableReason || "Agotado"}
                        </span>
                      )}
                    </div>
                  </div>

                  {/* Link Button */}
                  <Link
                    href={`/products/${h.productSlug}`}
                    onClick={(e) => e.stopPropagation()}
                    className="flex h-8 w-8 items-center justify-center rounded-xl bg-white text-zinc-700 shadow-xs transition-colors hover:bg-amber-600 hover:text-white dark:bg-zinc-800 dark:text-zinc-200"
                    aria-label={`Ver ${h.productName}`}
                  >
                    <ArrowRight className="h-4 w-4" />
                  </Link>
                </div>
              );
            })}
          </div>

          {/* Buy Whole Room Action */}
          <div className="mt-6 border-t border-zinc-100 pt-5 dark:border-zinc-800">
            <button
              type="button"
              disabled={availableHotspots.length === 0 || isAddingAll}
              onClick={handleBuyWholeRoom}
              className="flex w-full items-center justify-center gap-2 rounded-2xl bg-zinc-900 px-4 py-3.5 text-xs font-bold text-white shadow-md transition-all hover:bg-amber-700 disabled:opacity-50 disabled:cursor-not-allowed dark:bg-zinc-100 dark:text-zinc-900 dark:hover:bg-amber-600 dark:hover:text-white"
            >
              {isAddingAll ? (
                <>
                  <Loader2 className="h-4 w-4 animate-spin" />
                  <span>Añadiendo ambiente al carrito...</span>
                </>
              ) : addedSuccess ? (
                <>
                  <CheckCircle className="h-4 w-4 text-emerald-400" />
                  <span>¡Ambiente añadido al carrito!</span>
                </>
              ) : (
                <>
                  <ShoppingBag className="h-4 w-4" />
                  <span>Comprar Ambiente Completo ({formattedTotalPrice})</span>
                </>
              )}
            </button>

            {addedSuccess && (
              <p className="mt-2 text-center text-[11px] font-medium text-emerald-600 dark:text-emerald-400">
                Se han añadido las piezas disponibles al carrito con éxito.
              </p>
            )}
          </div>

          {/* Quick Info */}
          <div className="mt-4 rounded-2xl bg-zinc-50 p-4 text-xs text-zinc-600 dark:bg-zinc-800/50 dark:text-zinc-400">
            <div className="flex items-center gap-2 font-semibold text-zinc-900 dark:text-zinc-200">
              <Sparkles className="h-4 w-4 text-amber-600" />
              <span>Garantía de Estilismo HomeSphere</span>
            </div>
            <p className="mt-1 text-[11px] leading-relaxed">
              Comprando el conjunto completo accedes a asesoría de montaje y entrega coordinada en un único despacho preferencial.
            </p>
          </div>
        </div>
      </div>
    </div>
  );
}

