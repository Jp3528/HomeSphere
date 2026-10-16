import { Suspense } from "react";
import { Metadata } from "next";
import { ProductCard } from "@/components/catalog/ProductCard";
import { CatalogSidebar } from "@/components/catalog/CatalogSidebar";
import { CatalogToolbar } from "@/components/catalog/CatalogToolbar";
import { Pagination } from "@/components/catalog/Pagination";
import { CatalogFilters, PagedResult, ProductListItem } from "@/types/catalog";
import { PackageOpen } from "lucide-react";

export const metadata: Metadata = {
  title: "Catálogo de Mobiliario y Diseño | HomeSphere",
  description:
    "Explora nuestra cuidada colección de mobiliario contemporáneo, sofás modulares, mesas de roble, lámparas escultóricas y decoración para el hogar.",
  openGraph: {
    title: "Catálogo de Mobiliario y Diseño | HomeSphere",
    description:
      "Mobiliario arquitectónico y piezas atemporales diseñadas para transformar tus espacios.",
    type: "website",
  },
};

const API_BASE = process.env.NEXT_PUBLIC_API_URL || "http://localhost:5100";

async function getCatalogData(params: {
  search?: string;
  categorySlug?: string;
  brandSlug?: string;
  minPrice?: string;
  maxPrice?: string;
  sort?: string;
  page?: string;
}) {
  const query = new URLSearchParams();
  if (params.search) query.set("search", params.search);
  if (params.categorySlug) query.set("categorySlug", params.categorySlug);
  if (params.brandSlug) query.set("brandSlug", params.brandSlug);
  if (params.minPrice) query.set("minPrice", params.minPrice);
  if (params.maxPrice) query.set("maxPrice", params.maxPrice);
  if (params.sort) query.set("sort", params.sort);
  query.set("page", params.page || "1");
  query.set("pageSize", "12");

  try {
    const [productsRes, filtersRes] = await Promise.all([
      fetch(`${API_BASE}/api/products?${query.toString()}`, { cache: "no-store" }),
      fetch(`${API_BASE}/api/products/filters`, { cache: "no-store" }),
    ]);

    if (!productsRes.ok || !filtersRes.ok) {
      throw new Error("Failed to fetch catalog data");
    }

    const products: PagedResult<ProductListItem> = await productsRes.json();
    const filters: CatalogFilters = await filtersRes.json();

    return { products, filters };
  } catch {
    return {
      products: {
        items: [],
        pageNumber: 1,
        pageSize: 12,
        totalCount: 0,
        totalPages: 0,
        hasPreviousPage: false,
        hasNextPage: false,
      },
      filters: {
        minPrice: 0,
        maxPrice: 2000,
        categories: [],
        brands: [],
      },
    };
  }
}

interface PageProps {
  searchParams: Promise<{
    search?: string;
    categorySlug?: string;
    brandSlug?: string;
    minPrice?: string;
    maxPrice?: string;
    sort?: string;
    page?: string;
  }>;
}

async function CatalogContent({ searchParams }: PageProps) {
  const resolvedParams = await searchParams;
  const { products, filters } = await getCatalogData(resolvedParams);

  return (
    <div className="grid grid-cols-1 gap-8 lg:grid-cols-4">
      {/* Filters Sidebar */}
      <div className="lg:col-span-1">
        <CatalogSidebar filters={filters} />
      </div>

      {/* Main Content */}
      <div className="space-y-6 lg:col-span-3">
        <CatalogToolbar totalCount={products.totalCount} />

        {products.items.length === 0 ? (
          <div className="flex flex-col items-center justify-center rounded-3xl border border-dashed border-zinc-200 bg-white p-12 text-center dark:border-zinc-800 dark:bg-zinc-900">
            <PackageOpen className="h-12 w-12 text-zinc-400" />
            <h3 className="mt-4 text-base font-semibold text-zinc-900 dark:text-zinc-100">
              No se encontraron productos
            </h3>
            <p className="mt-1 text-xs text-zinc-500 dark:text-zinc-400">
              Prueba a ajustar tus filtros de búsqueda o restablecer los precios seleccionados.
            </p>
          </div>
        ) : (
          <div className="grid grid-cols-1 gap-6 sm:grid-cols-2 xl:grid-cols-3">
            {products.items.map((product) => (
              <ProductCard key={product.id} product={product} />
            ))}
          </div>
        )}

        <Pagination
          currentPage={products.pageNumber}
          totalPages={products.totalPages}
          hasPrevious={products.hasPreviousPage}
          hasNext={products.hasNextPage}
        />
      </div>
    </div>
  );
}

export default function ProductsPage({ searchParams }: PageProps) {
  return (
    <div className="min-h-screen bg-zinc-50/50 py-8 dark:bg-zinc-950">
      <div className="mx-auto max-w-7xl px-4 sm:px-6 lg:px-8">
        {/* Header */}
        <div className="mb-8 space-y-2">
          <p className="text-xs font-semibold tracking-wider text-amber-700 uppercase dark:text-amber-400">
            Colección Integral
          </p>
          <h1 className="text-3xl font-extrabold tracking-tight text-zinc-900 sm:text-4xl dark:text-zinc-50">
            Mobiliario y Piezas de Autor
          </h1>
          <p className="max-w-2xl text-sm text-zinc-500 dark:text-zinc-400">
            Descubre piezas diseñadas con materiales nobles, acabados artesanales y una estética
            atemporal para elevar cada rincón de tu hogar.
          </p>
        </div>

        {/* Streaming Catalog Content */}
        <Suspense
          fallback={
            <div className="grid grid-cols-1 gap-8 lg:grid-cols-4">
              <div className="h-96 rounded-2xl bg-zinc-200/50 animate-pulse dark:bg-zinc-800/50" />
              <div className="space-y-6 lg:col-span-3">
                <div className="h-16 rounded-2xl bg-zinc-200/50 animate-pulse dark:bg-zinc-800/50" />
                <div className="grid grid-cols-1 gap-6 sm:grid-cols-2 xl:grid-cols-3">
                  {Array.from({ length: 6 }).map((_, i) => (
                    <div
                      key={i}
                      className="h-80 rounded-2xl bg-zinc-200/50 animate-pulse dark:bg-zinc-800/50"
                    />
                  ))}
                </div>
              </div>
            </div>
          }
        >
          <CatalogContent searchParams={searchParams} />
        </Suspense>
      </div>
    </div>
  );
}
