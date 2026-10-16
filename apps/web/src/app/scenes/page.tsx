import { Suspense } from "react";
import { Metadata } from "next";
import { SceneCard } from "@/components/scenes/SceneCard";
import { RoomSceneListItem } from "@/types/scenes";
import { Sparkles, PackageOpen } from "lucide-react";

export const metadata: Metadata = {
  title: "Ambientes Interactivos & Estilismo | HomeSphere",
  description:
    "Explora nuestros espacios diseñados por interioristas. Descubre piezas contemporáneas integradas en salones, comedores y dormitorios interactivos.",
  openGraph: {
    title: "Ambientes Interactivos & Estilismo | HomeSphere",
    description: "Espacios de diseño con puntos interactivos para explorar y comprar cada elemento.",
    type: "website",
  },
};

const API_BASE = process.env.NEXT_PUBLIC_API_URL || "http://localhost:5100";

async function getScenes(): Promise<RoomSceneListItem[]> {
  try {
    const res = await fetch(`${API_BASE}/api/scenes`, { cache: "no-store" });
    if (!res.ok) return [];
    return await res.json();
  } catch {
    return [];
  }
}

async function ScenesContent() {
  const scenes = await getScenes();

  if (scenes.length === 0) {
    return (
      <div className="flex flex-col items-center justify-center rounded-3xl border border-dashed border-zinc-200 bg-white p-12 text-center dark:border-zinc-800 dark:bg-zinc-900">
        <PackageOpen className="h-12 w-12 text-zinc-400" />
        <h3 className="mt-4 text-base font-semibold text-zinc-900 dark:text-zinc-100">
          No hay ambientes publicados
        </h3>
        <p className="mt-1 text-xs text-zinc-500 dark:text-zinc-400">
          Pronto agregaremos nuevas escenas interactivas para inspirar tus proyectos.
        </p>
      </div>
    );
  }

  return (
    <div className="grid grid-cols-1 gap-8 md:grid-cols-2 lg:grid-cols-3">
      {scenes.map((scene) => (
        <SceneCard key={scene.id} scene={scene} />
      ))}
    </div>
  );
}

export default function ScenesPage() {
  return (
    <div className="min-h-screen bg-zinc-50/50 py-10 dark:bg-zinc-950">
      <div className="mx-auto max-w-7xl px-4 sm:px-6 lg:px-8">
        {/* Header */}
        <div className="mb-10 max-w-3xl space-y-3">
          <div className="flex items-center gap-2 text-xs font-semibold tracking-wider text-amber-700 uppercase dark:text-amber-400">
            <Sparkles className="h-4 w-4" />
            <span>Experiencia de Interiorismo</span>
          </div>
          <h1 className="text-3xl font-extrabold tracking-tight text-zinc-900 sm:text-4xl dark:text-zinc-50">
            Ambientes Interactivos & Espacios de Autor
          </h1>
          <p className="text-sm leading-relaxed text-zinc-600 dark:text-zinc-400">
            Navega por composiciones reales creadas por nuestros interioristas. Interactúa con cada
            objeto para descubrir sus materiales, dimensiones y añadirlo directamente a tu proyecto.
          </p>
        </div>

        {/* Content with Suspense streaming */}
        <Suspense
          fallback={
            <div className="grid grid-cols-1 gap-8 md:grid-cols-2 lg:grid-cols-3">
              {Array.from({ length: 3 }).map((_, i) => (
                <div
                  key={i}
                  className="aspect-16/10 w-full rounded-3xl bg-zinc-200/50 animate-pulse dark:bg-zinc-800/50"
                />
              ))}
            </div>
          }
        >
          <ScenesContent />
        </Suspense>
      </div>
    </div>
  );
}
