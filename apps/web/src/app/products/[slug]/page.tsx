import { Suspense } from "react";
import Link from "next/link";
import { Metadata } from "next";
import { ProductGallery } from "@/components/catalog/ProductGallery";
import { VariantSelector } from "@/components/catalog/VariantSelector";
import { ProductDetail } from "@/types/catalog";
import { ChevronRight, Sparkles } from "lucide-react";

interface PageProps {
  params: Promise<{
    slug: string;
  }>;
}

const API_BASE = process.env.NEXT_PUBLIC_API_URL || "http://localhost:5100";

export async function generateStaticParams() {
  return [
    { slug: "sofa-modular-nordhavn-3-cuerpos" },
    { slug: "mesa-comedor-stockholm-roble" },
  ];
}

async function getProductBySlug(slug: string): Promise<ProductDetail | null> {
  try {
    const res = await fetch(`${API_BASE}/api/products/${slug}`, {
      cache: "no-store",
    });

    if (!res.ok) {
      return null;
    }

    return await res.json();
  } catch {
    return null;
  }
}

export async function generateMetadata({ params }: PageProps): Promise<Metadata> {
  const { slug } = await params;
  const product = await getProductBySlug(slug);

  if (!product) {
    return {
      title: "Producto | HomeSphere",
    };
  }

  const primaryImage =
    product.images.find((i) => i.isPrimary)?.url ||
    product.images[0]?.url ||
    "/placeholder.jpg";

  return {
    title: `${product.name} | ${product.brandName} | HomeSphere`,
    description: product.shortDescription,
    openGraph: {
      title: `${product.name} — ${product.brandName}`,
      description: product.shortDescription,
      images: [
        {
          url: primaryImage,
          alt: product.name,
        },
      ],
      type: "website",
    },
  };
}

async function ProductDetailContent({ params }: PageProps) {
  const { slug } = await params;
  const product = await getProductBySlug(slug);

  if (!product) {
    return (
      <div className="flex flex-col items-center justify-center rounded-3xl border border-dashed border-zinc-200 bg-white p-12 text-center dark:border-zinc-800 dark:bg-zinc-900">
        <h2 className="text-xl font-bold text-zinc-900 dark:text-zinc-100">
          Producto no disponible
        </h2>
        <p className="mt-2 text-sm text-zinc-500 dark:text-zinc-400">
          No pudimos cargar la información de esta pieza en este momento.
        </p>
        <Link
          href="/products"
          className="mt-4 rounded-xl bg-zinc-900 px-4 py-2 text-xs font-semibold text-white hover:bg-amber-600 transition-colors dark:bg-zinc-100 dark:text-zinc-900"
        >
          Explorar el Catálogo
        </Link>
      </div>
    );
  }

  // Structured Data (JSON-LD) for Google Rich Snippets
  const jsonLd = {
    "@context": "https://schema.org",
    "@type": "Product",
    name: product.name,
    image: product.images.map((img) => img.url),
    description: product.shortDescription,
    brand: {
      "@type": "Brand",
      name: product.brandName,
    },
    offers: {
      "@type": "AggregateOffer",
      priceCurrency: "USD",
      lowPrice: Math.min(...product.variants.map((v) => v.price), product.basePrice),
      highPrice: Math.max(...product.variants.map((v) => v.price), product.basePrice),
      offerCount: product.variants.length,
      availability:
        product.variants.some((v) => v.stockQuantity > 0)
          ? "https://schema.org/InStock"
          : "https://schema.org/OutOfStock",
    },
  };

  return (
    <>
      {/* Inject JSON-LD */}
      <script
        type="application/ld+json"
        dangerouslySetInnerHTML={{ __html: JSON.stringify(jsonLd) }}
      />

      {/* Breadcrumbs */}
      <nav
        aria-label="Breadcrumbs"
        className="mb-8 flex items-center gap-2 text-xs text-zinc-500 dark:text-zinc-400"
      >
        <Link href="/" className="hover:text-amber-600 transition-colors">
          Inicio
        </Link>
        <ChevronRight className="h-3 w-3 text-zinc-400" />
        <Link href="/products" className="hover:text-amber-600 transition-colors">
          Catálogo
        </Link>
        <ChevronRight className="h-3 w-3 text-zinc-400" />
        <Link
          href={`/products?categorySlug=${product.categorySlug}`}
          className="hover:text-amber-600 transition-colors"
        >
          {product.categoryName}
        </Link>
        <ChevronRight className="h-3 w-3 text-zinc-400" />
        <span className="font-semibold text-zinc-900 line-clamp-1 dark:text-zinc-100">
          {product.name}
        </span>
      </nav>

      {/* Main Product Section */}
      <div className="grid grid-cols-1 gap-12 lg:grid-cols-12">
        {/* Gallery (Left: 7 cols) */}
        <div className="lg:col-span-7">
          <ProductGallery images={product.images} productName={product.name} />
        </div>

        {/* Product Meta & Variant Selection (Right: 5 cols) */}
        <div className="flex flex-col lg:col-span-5">
          {/* Brand & Category badges */}
          <div className="flex items-center justify-between">
            <Link
              href={`/products?brandSlug=${product.brandSlug}`}
              className="text-xs font-bold uppercase tracking-wider text-amber-700 hover:underline dark:text-amber-400"
            >
              {product.brandName}
            </Link>

            {product.isFeatured && (
              <span className="flex items-center gap-1 rounded-full bg-amber-100 px-2.5 py-0.5 text-[11px] font-semibold text-amber-800 dark:bg-amber-950/60 dark:text-amber-300">
                <Sparkles className="h-3 w-3" />
                Destacado
              </span>
            )}
          </div>

          {/* Title & Short Description */}
          <h1 className="mt-2 text-2xl font-extrabold tracking-tight text-zinc-900 sm:text-3xl dark:text-zinc-50">
            {product.name}
          </h1>

          <p className="mt-3 text-sm leading-relaxed text-zinc-600 dark:text-zinc-400">
            {product.shortDescription}
          </p>

          {/* Variant Selector & CTA Component */}
          <div className="mt-6">
            <VariantSelector
              variants={product.variants}
              basePrice={product.basePrice}
              productName={product.name}
            />
          </div>
        </div>
      </div>

      {/* Detailed Specifications / Story Section */}
      <div className="mt-16 rounded-3xl border border-zinc-200/80 bg-white p-8 shadow-xs dark:border-zinc-800 dark:bg-zinc-900">
        <h2 className="text-xl font-bold text-zinc-900 dark:text-zinc-50">
          Descripción y Filosofía de Diseño
        </h2>
        <div className="mt-4 prose prose-zinc max-w-none text-sm leading-relaxed text-zinc-600 dark:prose-invert dark:text-zinc-300">
          <p>{product.description}</p>
        </div>

        <div className="mt-8 grid grid-cols-1 gap-6 border-t border-zinc-100 pt-8 sm:grid-cols-3 dark:border-zinc-800">
          <div>
            <h3 className="text-xs font-bold uppercase tracking-wider text-zinc-400">
              Materiales & Acabados
            </h3>
            <p className="mt-2 text-xs text-zinc-600 dark:text-zinc-400">
              Seleccionados por su durabilidad y calidez táctil, tratados con protectores ecológicos
              de bajo impacto ambiental.
            </p>
          </div>

          <div>
            <h3 className="text-xs font-bold uppercase tracking-wider text-zinc-400">
              Cuidado y Mantenimiento
            </h3>
            <p className="mt-2 text-xs text-zinc-600 dark:text-zinc-400">
              Limpieza con paño de microfibra ligeramente humedecido. Evitar la exposición directa
              a fuentes intensas de calor o humedad extrema.
            </p>
          </div>

          <div>
            <h3 className="text-xs font-bold uppercase tracking-wider text-zinc-400">
              Sostenibilidad
            </h3>
            <p className="mt-2 text-xs text-zinc-600 dark:text-zinc-400">
              Empaques 100% libres de plásticos de un solo uso, con cartón corrugado reciclable
              certificado FSC.
            </p>
          </div>
        </div>
      </div>
    </>
  );
}

export default function ProductDetailPage({ params }: PageProps) {
  return (
    <div className="min-h-screen bg-zinc-50/50 py-8 dark:bg-zinc-950">
      <div className="mx-auto max-w-7xl px-4 sm:px-6 lg:px-8">
        <Suspense
          fallback={
            <div className="grid grid-cols-1 gap-12 lg:grid-cols-12 animate-pulse">
              <div className="h-96 rounded-3xl bg-zinc-200/50 lg:col-span-7 dark:bg-zinc-800/50" />
              <div className="space-y-4 lg:col-span-5">
                <div className="h-4 w-24 rounded bg-zinc-200/50 dark:bg-zinc-800/50" />
                <div className="h-8 w-3/4 rounded bg-zinc-200/50 dark:bg-zinc-800/50" />
                <div className="h-20 w-full rounded bg-zinc-200/50 dark:bg-zinc-800/50" />
                <div className="h-48 w-full rounded-2xl bg-zinc-200/50 dark:bg-zinc-800/50" />
              </div>
            </div>
          }
        >
          <ProductDetailContent params={params} />
        </Suspense>
      </div>
    </div>
  );
}
