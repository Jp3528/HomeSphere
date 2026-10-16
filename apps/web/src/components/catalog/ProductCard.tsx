import Link from "next/link";
import Image from "next/image";
import { ProductListItem } from "@/types/catalog";
import { Sparkles } from "lucide-react";
import { WishlistHeartButton } from "@/components/wishlist/WishlistHeartButton";

interface ProductCardProps {
  product: ProductListItem;
}

export function ProductCard({ product }: ProductCardProps) {
  const formattedPrice = new Intl.NumberFormat("es-PE", {
    style: "currency",
    currency: "USD",
  }).format(product.basePrice);

  const isLowStock = product.totalStock > 0 && product.totalStock <= 5;
  const isOutOfStock = product.totalStock <= 0;

  return (
    <Link
      href={`/products/${product.slug}`}
      className="group flex flex-col overflow-hidden rounded-2xl border border-zinc-200/80 bg-white transition-all duration-300 hover:-translate-y-1 hover:border-zinc-300 hover:shadow-xl dark:border-zinc-800 dark:bg-zinc-900"
    >
      {/* Image container */}
      <div className="relative aspect-4/3 w-full overflow-hidden bg-zinc-100 dark:bg-zinc-800">
        {product.primaryImageUrl ? (
          <Image
            src={product.primaryImageUrl}
            alt={product.name}
            fill
            sizes="(max-width: 640px) 100vw, (max-width: 1024px) 50vw, 33vw"
            className="object-cover transition-transform duration-500 ease-out group-hover:scale-105"
          />
        ) : (
          <div className="flex h-full w-full items-center justify-center text-zinc-400">
            Sin imagen
          </div>
        )}

        {/* Featured badge */}
        {product.isFeatured && (
          <div className="absolute top-3 left-3 flex items-center gap-1 rounded-full bg-amber-500/90 px-2.5 py-1 text-[11px] font-semibold text-white shadow-sm backdrop-blur-xs">
            <Sparkles className="h-3 w-3" />
            Destacado
          </div>
        )}

        {/* Wishlist toggle */}
        <div className="absolute top-3 right-3 z-20">
          <WishlistHeartButton productId={product.id} size="sm" />
        </div>

        {/* Category Pill */}
        <div className="absolute bottom-3 left-3 rounded-md bg-white/90 px-2 py-0.5 text-[11px] font-medium text-zinc-800 shadow-xs backdrop-blur-xs dark:bg-zinc-900/90 dark:text-zinc-200">
          {product.categoryName}
        </div>
      </div>

      {/* Info Container */}
      <div className="flex flex-1 flex-col p-4">
        <span className="text-[11px] font-semibold tracking-wider text-amber-700 uppercase dark:text-amber-400">
          {product.brandName}
        </span>

        <h3 className="mt-1 line-clamp-1 text-base font-semibold text-zinc-900 transition-colors group-hover:text-amber-600 dark:text-zinc-100 dark:group-hover:text-amber-400">
          {product.name}
        </h3>

        <p className="mt-1 line-clamp-2 text-xs text-zinc-500 dark:text-zinc-400">
          {product.shortDescription}
        </p>

        {/* Price and Stock Row */}
        <div className="mt-auto pt-4 flex items-baseline justify-between">
          <div>
            <span className="text-xs text-zinc-400">Desde </span>
            <span className="text-lg font-bold text-zinc-900 dark:text-white">
              {formattedPrice}
            </span>
          </div>

          <div className="flex items-center gap-1.5 text-[11px] font-medium">
            {isOutOfStock ? (
              <span className="flex items-center gap-1 text-rose-600 dark:text-rose-400">
                <span className="h-2 w-2 rounded-full bg-rose-500" />
                Agotado
              </span>
            ) : isLowStock ? (
              <span className="flex items-center gap-1 text-amber-600 dark:text-amber-400">
                <span className="h-2 w-2 rounded-full bg-amber-500" />
                Pocas unidades
              </span>
            ) : (
              <span className="flex items-center gap-1 text-emerald-600 dark:text-emerald-400">
                <span className="h-2 w-2 rounded-full bg-emerald-500" />
                En stock
              </span>
            )}
          </div>
        </div>
      </div>
    </Link>
  );
}
