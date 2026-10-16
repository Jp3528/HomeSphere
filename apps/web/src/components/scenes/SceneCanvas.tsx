"use client";

import { useState, useRef, useEffect, useCallback } from "react";
import Image from "next/image";
import Link from "next/link";
import { SceneHotspot } from "@/types/scenes";
import { useCartStore } from "@/stores/cart-store";
import { ArrowUpRight, X, ShoppingBag } from "lucide-react";

interface SceneCanvasProps {
  imageUrl: string;
  sceneTitle: string;
  hotspots: SceneHotspot[];
  activeHotspotId?: string | null;
  onSelectHotspot?: (id: string | null) => void;
}

export function SceneCanvas({
  imageUrl,
  sceneTitle,
  hotspots,
  activeHotspotId: controlledActiveId,
  onSelectHotspot,
}: SceneCanvasProps) {
  const [internalActiveId, setInternalActiveId] = useState<string | null>(null);
  const containerRef = useRef<HTMLDivElement>(null);
  const { addItem } = useCartStore();

  const activeId = controlledActiveId !== undefined ? controlledActiveId : internalActiveId;

  const setActiveHotspot = useCallback((id: string | null) => {
    if (onSelectHotspot) {
      onSelectHotspot(id);
    } else {
      setInternalActiveId(id);
    }
  }, [onSelectHotspot]);

  // Close popover on Escape key
  useEffect(() => {
    const handleKeyDown = (e: KeyboardEvent) => {
      if (e.key === "Escape") {
        setActiveHotspot(null);
      }
    };
    window.addEventListener("keydown", handleKeyDown);
    return () => window.removeEventListener("keydown", handleKeyDown);
  }, [setActiveHotspot]);

  return (
    <div
      ref={containerRef}
      className="relative aspect-16/10 w-full overflow-hidden rounded-3xl border border-zinc-200/80 bg-zinc-950 shadow-2xl select-none dark:border-zinc-800"
    >
      {/* Background Room Photo */}
      <Image
        src={imageUrl}
        alt={sceneTitle}
        fill
        priority
        sizes="(max-width: 1280px) 100vw, 1200px"
        className="object-cover transition-transform duration-700 ease-out"
        onClick={() => setActiveHotspot(null)}
      />

      {/* Subtle Dark Vignette for contrast */}
      <div
        className="pointer-events-none absolute inset-0 bg-radial-gradient from-transparent via-transparent to-black/40"
        aria-hidden="true"
      />

      {/* Hotspots Pins */}
      {hotspots.map((hotspot, index) => {
        const isSelected = hotspot.id === activeId;
        const price = hotspot.productVariantPrice ?? hotspot.productBasePrice;
        const formattedPrice = new Intl.NumberFormat("es-PE", {
          style: "currency",
          currency: "USD",
        }).format(price);

        // Calculate if tooltip should open to top or bottom, left or right
        const openTop = hotspot.yCoordinate > 55;
        const openLeft = hotspot.xCoordinate > 65;

        return (
          <div
            key={hotspot.id}
            style={{
              left: `${hotspot.xCoordinate}%`,
              top: `${hotspot.yCoordinate}%`,
            }}
            className="absolute -translate-x-1/2 -translate-y-1/2 z-20"
          >
            {/* Pulsing Pin Button (44x44px accessible target) */}
            <button
              type="button"
              onClick={(e) => {
                e.stopPropagation();
                setActiveHotspot(isSelected ? null : hotspot.id);
              }}
              aria-label={`Ver producto: ${hotspot.title || hotspot.productName}${hotspot.isAvailable === false ? ' (No disponible)' : ''}`}
              aria-expanded={isSelected}
              aria-haspopup="dialog"
              className={`group relative flex h-11 w-11 items-center justify-center rounded-full transition-transform focus-visible:ring-4 focus-visible:ring-amber-500/70 focus-visible:outline-none ${
                isSelected ? "scale-110" : "hover:scale-110"
              }`}
            >
              {/* Pulse animation ring */}
              <span
                className={`absolute inline-flex h-9 w-9 rounded-full bg-white opacity-75 ${
                  isSelected ? "animate-none scale-125" : "animate-ping"
                }`}
              />

              {/* Pin outer circle */}
              <span
                className={`relative flex h-9 w-9 items-center justify-center rounded-full shadow-lg backdrop-blur-md transition-colors ${
                  isSelected
                    ? "bg-amber-600 text-white ring-4 ring-white/70"
                    : hotspot.isAvailable === false
                    ? "bg-zinc-700/90 text-zinc-300 ring-2 ring-zinc-500/50"
                    : "bg-white/95 text-zinc-900 group-hover:bg-amber-500 group-hover:text-white"
                }`}
              >
                <span className="text-xs font-bold">{index + 1}</span>
              </span>
            </button>

            {/* Interactive Tooltip Card */}
            {isSelected && (
              <div
                role="dialog"
                aria-modal="false"
                aria-label={hotspot.productName}
                onClick={(e) => e.stopPropagation()}
                className={`absolute z-30 w-72 rounded-2xl border border-zinc-200/90 bg-white/95 p-3.5 shadow-2xl backdrop-blur-md animate-in fade-in zoom-in-95 duration-200 dark:border-zinc-700 dark:bg-zinc-900/95 ${
                  openTop ? "bottom-full mb-3" : "top-full mt-3"
                } ${openLeft ? "right-0" : "left-0"}`}
              >
                {/* Close Button */}
                <button
                  type="button"
                  onClick={() => setActiveHotspot(null)}
                  className="absolute top-2.5 right-2.5 flex h-7 w-7 items-center justify-center rounded-full text-zinc-400 hover:bg-zinc-100 hover:text-zinc-600 focus-visible:ring-2 focus-visible:ring-amber-500 focus-visible:outline-none dark:hover:bg-zinc-800 dark:hover:text-zinc-200"
                  aria-label="Cerrar detalle"
                >
                  <X className="h-3.5 w-3.5" />
                </button>

                <div className="flex gap-3">
                  {/* Thumbnail */}
                  <div className="relative aspect-square h-16 w-16 shrink-0 overflow-hidden rounded-xl bg-zinc-100 dark:bg-zinc-800">
                    {hotspot.productPrimaryImageUrl ? (
                      <Image
                        src={hotspot.productPrimaryImageUrl}
                        alt={hotspot.productName}
                        fill
                        sizes="64px"
                        className="object-cover"
                      />
                    ) : (
                      <div className="flex h-full w-full items-center justify-center text-[10px] text-zinc-400">
                        Foto
                      </div>
                    )}
                  </div>

                  {/* Info */}
                  <div className="flex flex-1 flex-col justify-center pr-4">
                    <span className="line-clamp-1 text-xs font-bold text-zinc-900 dark:text-zinc-50">
                      {hotspot.title || hotspot.productName}
                    </span>
                    <span className="mt-0.5 text-sm font-extrabold text-amber-700 dark:text-amber-400">
                      {formattedPrice}
                    </span>
                    {hotspot.isAvailable === false && (
                      <span className="mt-0.5 inline-block text-[10px] font-semibold text-rose-600 dark:text-rose-400">
                        {hotspot.unavailableReason || "Agotado"}
                      </span>
                    )}
                  </div>
                </div>

                {/* CTA Buttons */}
                <div className="mt-3 flex gap-2">
                  <Link
                    href={`/products/${hotspot.productSlug}`}
                    className="flex-1 flex items-center justify-center gap-1 rounded-xl border border-zinc-200 py-2 text-[11px] font-semibold text-zinc-700 transition-colors hover:bg-zinc-100 focus-visible:ring-2 focus-visible:ring-amber-500 focus-visible:outline-none dark:border-zinc-700 dark:text-zinc-200 dark:hover:bg-zinc-800"
                  >
                    <span>Detalles</span>
                    <ArrowUpRight className="h-3 w-3" />
                  </Link>
                  {hotspot.isAvailable === false ? (
                    <div className="flex-1 flex items-center justify-center rounded-xl bg-zinc-100 py-2 text-[11px] font-semibold text-zinc-400 dark:bg-zinc-800 dark:text-zinc-500">
                      <span>No disponible</span>
                    </div>
                  ) : hotspot.productVariantId ? (
                    <button
                      type="button"
                      onClick={async () => {
                        try {
                          await addItem(hotspot.productVariantId!, 1);
                        } catch {
                          // Handled by store
                        }
                      }}
                      className="flex-1 flex items-center justify-center gap-1 rounded-xl bg-amber-700 py-2 text-[11px] font-semibold text-white transition-colors hover:bg-amber-600 shadow-sm focus-visible:ring-2 focus-visible:ring-amber-500 focus-visible:outline-none"
                    >
                      <ShoppingBag className="h-3 w-3" />
                      <span>Comprar</span>
                    </button>
                  ) : null}
                </div>
              </div>
            )}
          </div>
        );
      })}
    </div>
  );
}
