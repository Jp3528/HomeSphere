"use client";

import { useState } from "react";
import { useRouter, useSearchParams } from "next/navigation";
import { Search, X } from "lucide-react";

interface CatalogToolbarProps {
  totalCount: number;
}

export function CatalogToolbar({ totalCount }: CatalogToolbarProps) {
  const router = useRouter();
  const searchParams = useSearchParams();

  const currentSearch = searchParams.get("search") || "";
  const currentSort = searchParams.get("sort") || "featured";

  const [searchInput, setSearchInput] = useState(currentSearch);

  const handleSearchSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    const params = new URLSearchParams(searchParams.toString());
    if (searchInput.trim()) {
      params.set("search", searchInput.trim());
    } else {
      params.delete("search");
    }
    params.set("page", "1");
    router.push(`/products?${params.toString()}`);
  };

  const handleClearSearch = () => {
    setSearchInput("");
    const params = new URLSearchParams(searchParams.toString());
    params.delete("search");
    params.set("page", "1");
    router.push(`/products?${params.toString()}`);
  };

  const handleSortChange = (newSort: string) => {
    const params = new URLSearchParams(searchParams.toString());
    params.set("sort", newSort);
    params.set("page", "1");
    router.push(`/products?${params.toString()}`);
  };

  return (
    <div className="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between rounded-2xl border border-zinc-200/80 bg-white p-4 shadow-xs dark:border-zinc-800 dark:bg-zinc-900">
      {/* Search Input */}
      <form onSubmit={handleSearchSubmit} className="relative flex-1 max-w-md">
        <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-zinc-400" />
        <input
          type="text"
          value={searchInput}
          onChange={(e) => setSearchInput(e.target.value)}
          placeholder="Buscar sofás, mesas, lámparas..."
          className="w-full rounded-xl border border-zinc-200 bg-zinc-50 py-2 pl-9 pr-9 text-sm text-zinc-800 placeholder-zinc-400 focus:border-amber-500 focus:bg-white focus:outline-hidden dark:border-zinc-700 dark:bg-zinc-800 dark:text-zinc-200"
        />
        {searchInput && (
          <button
            type="button"
            onClick={handleClearSearch}
            className="absolute right-3 top-1/2 -translate-y-1/2 text-zinc-400 hover:text-zinc-600 dark:hover:text-zinc-200"
          >
            <X className="h-4 w-4" />
          </button>
        )}
      </form>

      {/* Counter & Sort Select */}
      <div className="flex items-center justify-between gap-4 sm:justify-end">
        <span className="text-xs text-zinc-500 dark:text-zinc-400">
          <strong className="text-zinc-800 dark:text-zinc-200">{totalCount}</strong>{" "}
          {totalCount === 1 ? "resultado" : "resultados"}
        </span>

        <div className="flex items-center gap-2">
          <label htmlFor="sort-select" className="text-xs text-zinc-500 dark:text-zinc-400">
            Ordenar:
          </label>
          <select
            id="sort-select"
            value={currentSort}
            onChange={(e) => handleSortChange(e.target.value)}
            className="rounded-xl border border-zinc-200 bg-zinc-50 px-3 py-1.5 text-xs font-medium text-zinc-800 focus:border-amber-500 focus:bg-white focus:outline-hidden dark:border-zinc-700 dark:bg-zinc-800 dark:text-zinc-200"
          >
            <option value="featured">Destacados</option>
            <option value="price_asc">Precio: menor a mayor</option>
            <option value="price_desc">Precio: mayor a menor</option>
            <option value="newest">Más recientes</option>
            <option value="name_asc">Nombre: A - Z</option>
          </select>
        </div>
      </div>
    </div>
  );
}
