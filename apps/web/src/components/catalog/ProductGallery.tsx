"use client";

import { useState } from "react";
import Image from "next/image";
import { ProductImage } from "@/types/catalog";

interface ProductGalleryProps {
  images: ProductImage[];
  productName: string;
}

export function ProductGallery({ images, productName }: ProductGalleryProps) {
  const sortedImages = [...images].sort((a, b) => {
    if (a.isPrimary) return -1;
    if (b.isPrimary) return 1;
    return a.displayOrder - b.displayOrder;
  });

  const [activeIndex, setActiveIndex] = useState(0);
  const activeImage = sortedImages[activeIndex] || sortedImages[0];

  return (
    <div className="flex flex-col gap-4">
      {/* Main Image View */}
      <div className="relative aspect-4/3 w-full overflow-hidden rounded-3xl border border-zinc-200/80 bg-zinc-100 shadow-sm dark:border-zinc-800 dark:bg-zinc-900">
        {activeImage ? (
          <Image
            src={activeImage.url}
            alt={activeImage.altText || productName}
            fill
            priority
            sizes="(max-width: 1024px) 100vw, 50vw"
            className="object-cover transition-all duration-300"
          />
        ) : (
          <div className="flex h-full w-full items-center justify-center text-zinc-400">
            Sin imagen disponible
          </div>
        )}
      </div>

      {/* Thumbnails strip */}
      {sortedImages.length > 1 && (
        <div className="flex gap-3 overflow-x-auto pb-2">
          {sortedImages.map((img, index) => {
            const isSelected = index === activeIndex;
            return (
              <button
                key={img.id || index}
                onClick={() => setActiveIndex(index)}
                className={`relative aspect-square h-20 w-20 shrink-0 overflow-hidden rounded-xl border-2 transition-all ${
                  isSelected
                    ? "border-amber-600 ring-2 ring-amber-600/20"
                    : "border-transparent opacity-70 hover:opacity-100"
                }`}
              >
                <Image
                  src={img.url}
                  alt={img.altText || `${productName} miniatura ${index + 1}`}
                  fill
                  sizes="80px"
                  className="object-cover"
                />
              </button>
            );
          })}
        </div>
      )}
    </div>
  );
}
