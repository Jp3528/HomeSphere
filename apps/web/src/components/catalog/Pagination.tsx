"use client";

import { useRouter, useSearchParams } from "next/navigation";
import { ChevronLeft, ChevronRight } from "lucide-react";

interface PaginationProps {
  currentPage: number;
  totalPages: number;
  hasPrevious: boolean;
  hasNext: boolean;
}

export function Pagination({ currentPage, totalPages, hasPrevious, hasNext }: PaginationProps) {
  const router = useRouter();
  const searchParams = useSearchParams();

  if (totalPages <= 1) return null;

  const goToPage = (page: number) => {
    const params = new URLSearchParams(searchParams.toString());
    params.set("page", String(page));
    router.push(`/products?${params.toString()}`);
  };

  return (
    <div className="flex items-center justify-center gap-2 pt-8">
      <button
        onClick={() => goToPage(currentPage - 1)}
        disabled={!hasPrevious}
        className="flex h-9 w-9 items-center justify-center rounded-xl border border-zinc-200 bg-white text-zinc-600 transition-colors hover:bg-zinc-50 disabled:opacity-40 disabled:hover:bg-white dark:border-zinc-800 dark:bg-zinc-900 dark:text-zinc-300 dark:hover:bg-zinc-800"
        aria-label="Página anterior"
      >
        <ChevronLeft className="h-4 w-4" />
      </button>

      <div className="flex items-center gap-1 text-sm font-medium">
        {Array.from({ length: totalPages }, (_, i) => i + 1).map((page) => {
          const isCurrent = page === currentPage;
          return (
            <button
              key={page}
              onClick={() => goToPage(page)}
              className={`flex h-9 w-9 items-center justify-center rounded-xl transition-colors ${
                isCurrent
                  ? "bg-zinc-900 font-bold text-white dark:bg-zinc-100 dark:text-zinc-900"
                  : "border border-zinc-200 bg-white text-zinc-600 hover:bg-zinc-50 dark:border-zinc-800 dark:bg-zinc-900 dark:text-zinc-300 dark:hover:bg-zinc-800"
              }`}
            >
              {page}
            </button>
          );
        })}
      </div>

      <button
        onClick={() => goToPage(currentPage + 1)}
        disabled={!hasNext}
        className="flex h-9 w-9 items-center justify-center rounded-xl border border-zinc-200 bg-white text-zinc-600 transition-colors hover:bg-zinc-50 disabled:opacity-40 disabled:hover:bg-white dark:border-zinc-800 dark:bg-zinc-900 dark:text-zinc-300 dark:hover:bg-zinc-800"
        aria-label="Página siguiente"
      >
        <ChevronRight className="h-4 w-4" />
      </button>
    </div>
  );
}
