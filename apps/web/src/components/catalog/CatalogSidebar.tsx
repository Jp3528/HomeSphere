"use client";

import { useState } from "react";
import { useRouter, useSearchParams } from "next/navigation";
import { CatalogFilters } from "@/types/catalog";
import { Filter, RotateCcw } from "lucide-react";

interface CatalogSidebarProps {
  filters: CatalogFilters;
}

export function CatalogSidebar({ filters }: CatalogSidebarProps) {
  const router = useRouter();
  const searchParams = useSearchParams();

  const currentCategory = searchParams.get("categorySlug") || "";
  const currentBrand = searchParams.get("brandSlug") || "";
  const currentMinPrice = searchParams.get("minPrice") || "";
  const currentMaxPrice = searchParams.get("maxPrice") || "";

  const [minPrice, setMinPrice] = useState(currentMinPrice);
  const [maxPrice, setMaxPrice] = useState(currentMaxPrice);

  const updateParam = (key: string, value: string) => {
    const params = new URLSearchParams(searchParams.toString());
    if (value) {
      params.set(key, value);
    } else {
      params.delete(key);
    }
    params.set("page", "1"); // Reset to page 1 on filter change
    router.push(`/products?${params.toString()}`);
  };

  const handlePriceSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    const params = new URLSearchParams(searchParams.toString());
    if (minPrice) params.set("minPrice", minPrice);
    else params.delete("minPrice");

    if (maxPrice) params.set("maxPrice", maxPrice);
    else params.delete("maxPrice");

    params.set("page", "1");
    router.push(`/products?${params.toString()}`);
  };

  const handleClearFilters = () => {
    setMinPrice("");
    setMaxPrice("");
    router.push("/products");
  };

  const hasActiveFilters = Boolean(
    currentCategory || currentBrand || currentMinPrice || currentMaxPrice || searchParams.get("search")
  );

  return (
    <aside className="w-full space-y-6 rounded-2xl border border-zinc-200/80 bg-white p-5 shadow-xs dark:border-zinc-800 dark:bg-zinc-900">
      <div className="flex items-center justify-between pb-3 border-b border-zinc-100 dark:border-zinc-800">
        <div className="flex items-center gap-2 text-zinc-900 font-semibold dark:text-zinc-100">
          <Filter className="h-4 w-4 text-amber-600" />
          <span>Filtros</span>
        </div>

        {hasActiveFilters && (
          <button
            onClick={handleClearFilters}
            className="flex items-center gap-1 text-xs text-zinc-500 hover:text-amber-600 dark:text-zinc-400 dark:hover:text-amber-400"
          >
            <RotateCcw className="h-3 w-3" />
            Limpiar
          </button>
        )}
      </div>

      {/* Categorías */}
      <div className="space-y-3">
        <h4 className="text-xs font-bold uppercase tracking-wider text-zinc-400">
          Categorías
        </h4>
        <div className="flex flex-col gap-1 text-sm">
          <button
            onClick={() => updateParam("categorySlug", "")}
            className={`flex items-center justify-between rounded-lg px-2.5 py-1.5 text-left transition-colors ${
              !currentCategory
                ? "bg-amber-50 font-semibold text-amber-800 dark:bg-amber-950/40 dark:text-amber-300"
                : "text-zinc-600 hover:bg-zinc-50 dark:text-zinc-300 dark:hover:bg-zinc-800/60"
            }`}
          >
            <span>Todas</span>
          </button>

          {filters.categories.map((cat) => {
            const isActive = currentCategory === cat.slug;
            return (
              <button
                key={cat.slug}
                onClick={() => updateParam("categorySlug", isActive ? "" : cat.slug)}
                className={`flex items-center justify-between rounded-lg px-2.5 py-1.5 text-left transition-colors ${
                  isActive
                    ? "bg-amber-50 font-semibold text-amber-800 dark:bg-amber-950/40 dark:text-amber-300"
                    : "text-zinc-600 hover:bg-zinc-50 dark:text-zinc-300 dark:hover:bg-zinc-800/60"
                }`}
              >
                <span>{cat.name}</span>
                <span className="text-xs text-zinc-400">({cat.count})</span>
              </button>
            );
          })}
        </div>
      </div>

      {/* Marcas */}
      <div className="space-y-3 pt-3 border-t border-zinc-100 dark:border-zinc-800">
        <h4 className="text-xs font-bold uppercase tracking-wider text-zinc-400">
          Marcas de Diseño
        </h4>
        <div className="flex flex-col gap-1 text-sm">
          <button
            onClick={() => updateParam("brandSlug", "")}
            className={`flex items-center justify-between rounded-lg px-2.5 py-1.5 text-left transition-colors ${
              !currentBrand
                ? "bg-amber-50 font-semibold text-amber-800 dark:bg-amber-950/40 dark:text-amber-300"
                : "text-zinc-600 hover:bg-zinc-50 dark:text-zinc-300 dark:hover:bg-zinc-800/60"
            }`}
          >
            <span>Todas</span>
          </button>

          {filters.brands.map((b) => {
            const isActive = currentBrand === b.slug;
            return (
              <button
                key={b.slug}
                onClick={() => updateParam("brandSlug", isActive ? "" : b.slug)}
                className={`flex items-center justify-between rounded-lg px-2.5 py-1.5 text-left transition-colors ${
                  isActive
                    ? "bg-amber-50 font-semibold text-amber-800 dark:bg-amber-950/40 dark:text-amber-300"
                    : "text-zinc-600 hover:bg-zinc-50 dark:text-zinc-300 dark:hover:bg-zinc-800/60"
                }`}
              >
                <span>{b.name}</span>
                <span className="text-xs text-zinc-400">({b.count})</span>
              </button>
            );
          })}
        </div>
      </div>

      {/* Rango de Precios */}
      <div className="space-y-3 pt-3 border-t border-zinc-100 dark:border-zinc-800">
        <h4 className="text-xs font-bold uppercase tracking-wider text-zinc-400">
          Precio (USD)
        </h4>

        <form onSubmit={handlePriceSubmit} className="space-y-2">
          <div className="grid grid-cols-2 gap-2">
            <div>
              <label className="text-[11px] text-zinc-500 dark:text-zinc-400">Mínimo</label>
              <input
                type="number"
                placeholder={String(filters.minPrice || 0)}
                value={minPrice}
                onChange={(e) => setMinPrice(e.target.value)}
                className="w-full rounded-lg border border-zinc-200 bg-zinc-50 px-2.5 py-1.5 text-xs text-zinc-800 focus:border-amber-500 focus:bg-white focus:outline-hidden dark:border-zinc-700 dark:bg-zinc-800 dark:text-zinc-200"
              />
            </div>

            <div>
              <label className="text-[11px] text-zinc-500 dark:text-zinc-400">Máximo</label>
              <input
                type="number"
                placeholder={String(filters.maxPrice || 2000)}
                value={maxPrice}
                onChange={(e) => setMaxPrice(e.target.value)}
                className="w-full rounded-lg border border-zinc-200 bg-zinc-50 px-2.5 py-1.5 text-xs text-zinc-800 focus:border-amber-500 focus:bg-white focus:outline-hidden dark:border-zinc-700 dark:bg-zinc-800 dark:text-zinc-200"
              />
            </div>
          </div>

          <button
            type="submit"
            className="w-full rounded-lg bg-zinc-900 py-2 text-xs font-semibold text-white transition-colors hover:bg-amber-600 dark:bg-zinc-100 dark:text-zinc-900 dark:hover:bg-amber-400"
          >
            Aplicar Precio
          </button>
        </form>
      </div>
    </aside>
  );
}
