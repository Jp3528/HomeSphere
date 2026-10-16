import { Suspense } from "react";
import Link from "next/link";
import { Metadata } from "next";
import { InteractiveSceneView } from "@/components/scenes/InteractiveSceneView";
import { RoomSceneDetail } from "@/types/scenes";
import { ChevronRight, Sparkles } from "lucide-react";

interface PageProps {
  params: Promise<{
    slug: string;
  }>;
}

const API_BASE = process.env.NEXT_PUBLIC_API_URL || "http://localhost:5100";

export async function generateStaticParams() {
  return [
    { slug: "salon-nordico-serena" },
    { slug: "comedor-vanguardista-stockholm" },
    { slug: "dormitorio-minimalista-valldal" },
  ];
}

async function getSceneBySlug(slug: string): Promise<RoomSceneDetail | null> {
  try {
    const res = await fetch(`${API_BASE}/api/scenes/${slug}`, {
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
  const scene = await getSceneBySlug(slug);

  if (!scene) {
    return {
      title: "Ambiente no encontrado | HomeSphere",
    };
  }

  return {
    title: `${scene.title} | Ambientes Interactivos | HomeSphere`,
    description: scene.description,
    openGraph: {
      title: `${scene.title} — HomeSphere Interiorismo`,
      description: scene.description,
      images: [
        {
          url: scene.imageUrl,
          alt: scene.title,
        },
      ],
      type: "website",
    },
  };
}

async function SceneDetailContent({ params }: PageProps) {
  const { slug } = await params;
  const scene = await getSceneBySlug(slug);

  if (!scene) {
    return (
      <div className="flex flex-col items-center justify-center rounded-3xl border border-dashed border-zinc-200 bg-white p-12 text-center dark:border-zinc-800 dark:bg-zinc-900">
        <h2 className="text-xl font-bold text-zinc-900 dark:text-zinc-100">
          Ambiente no disponible
        </h2>
        <p className="mt-2 text-sm text-zinc-500 dark:text-zinc-400">
          No pudimos cargar la experiencia de este espacio en este momento.
        </p>
        <Link
          href="/scenes"
          className="mt-4 rounded-xl bg-zinc-900 px-4 py-2 text-xs font-semibold text-white hover:bg-amber-600 transition-colors dark:bg-zinc-100 dark:text-zinc-900"
        >
          Ver Todos los Ambientes
        </Link>
      </div>
    );
  }

  return (
    <>
      {/* Breadcrumbs */}
      <nav
        aria-label="Breadcrumbs"
        className="mb-6 flex items-center gap-2 text-xs text-zinc-500 dark:text-zinc-400"
      >
        <Link href="/" className="hover:text-amber-600 transition-colors">
          Inicio
        </Link>
        <ChevronRight className="h-3 w-3 text-zinc-400" />
        <Link href="/scenes" className="hover:text-amber-600 transition-colors">
          Ambientes Interactivos
        </Link>
        <ChevronRight className="h-3 w-3 text-zinc-400" />
        <span className="font-semibold text-zinc-900 line-clamp-1 dark:text-zinc-100">
          {scene.title}
        </span>
      </nav>

      {/* Header Info */}
      <div className="mb-8 space-y-2 max-w-3xl">
        <div className="flex items-center gap-2 text-xs font-semibold text-amber-700 uppercase dark:text-amber-400">
          <Sparkles className="h-3.5 w-3.5" />
          <span>Experiencia Inmersiva</span>
        </div>

        <h1 className="text-2xl font-extrabold tracking-tight text-zinc-900 sm:text-3xl dark:text-zinc-50">
          {scene.title}
        </h1>

        <p className="text-sm leading-relaxed text-zinc-600 dark:text-zinc-400">
          {scene.description}
        </p>
      </div>

      {/* Interactive Visual Experience */}
      <InteractiveSceneView scene={scene} />
    </>
  );
}

export default function SceneDetailPage({ params }: PageProps) {
  return (
    <div className="min-h-screen bg-zinc-50/50 py-8 dark:bg-zinc-950">
      <div className="mx-auto max-w-7xl px-4 sm:px-6 lg:px-8">
        <Suspense
          fallback={
            <div className="space-y-6 animate-pulse">
              <div className="h-4 w-48 rounded bg-zinc-200/50 dark:bg-zinc-800/50" />
              <div className="h-8 w-96 rounded bg-zinc-200/50 dark:bg-zinc-800/50" />
              <div className="aspect-16/10 w-full rounded-3xl bg-zinc-200/50 dark:bg-zinc-800/50" />
            </div>
          }
        >
          <SceneDetailContent params={params} />
        </Suspense>
      </div>
    </div>
  );
}
