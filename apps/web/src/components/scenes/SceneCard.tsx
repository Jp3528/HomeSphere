import Link from "next/link";
import Image from "next/image";
import { RoomSceneListItem } from "@/types/scenes";
import { Sparkles, ArrowRight } from "lucide-react";

interface SceneCardProps {
  scene: RoomSceneListItem;
}

export function SceneCard({ scene }: SceneCardProps) {
  return (
    <Link
      href={`/scenes/${scene.slug}`}
      className="group relative flex flex-col overflow-hidden rounded-3xl border border-zinc-200/80 bg-zinc-950 shadow-md transition-all duration-500 hover:-translate-y-1 hover:shadow-2xl dark:border-zinc-800"
    >
      {/* Background Image Container */}
      <div className="relative aspect-16/10 w-full overflow-hidden">
        <Image
          src={scene.imageUrl}
          alt={scene.title}
          fill
          sizes="(max-width: 768px) 100vw, 50vw"
          className="object-cover transition-transform duration-700 ease-out group-hover:scale-105"
        />

        {/* Gradient Overlay */}
        <div className="absolute inset-0 bg-gradient-to-t from-black/80 via-black/30 to-black/10" />

        {/* Hotspot Count & RoomType Badges */}
        <div className="absolute top-4 left-4 flex items-center gap-1.5 rounded-full bg-white/90 px-3 py-1 text-xs font-semibold text-zinc-900 shadow-sm backdrop-blur-md dark:bg-zinc-900/90 dark:text-zinc-100">
          <Sparkles className="h-3.5 w-3.5 text-amber-600" />
          <span>{scene.hotspotCount} {scene.hotspotCount === 1 ? "pieza" : "piezas integradas"}</span>
        </div>

        {scene.roomType && (
          <div className="absolute top-4 right-4 rounded-full bg-black/60 px-3 py-1 text-xs font-medium text-white shadow-sm backdrop-blur-md">
            <span>{scene.roomType}</span>
          </div>
        )}
      </div>

      {/* Content Meta */}
      <div className="absolute bottom-0 inset-x-0 p-6 text-white">
        <h3 className="text-xl font-bold tracking-tight text-white transition-colors group-hover:text-amber-400 sm:text-2xl">
          {scene.title}
        </h3>

        <p className="mt-1 line-clamp-2 text-xs leading-relaxed text-zinc-300">
          {scene.description}
        </p>

        <div className="mt-4 flex items-center gap-2 text-xs font-semibold text-amber-400 group-hover:underline">
          <span>Explorar Ambiente Interactivo</span>
          <ArrowRight className="h-4 w-4 transition-transform group-hover:translate-x-1" />
        </div>
      </div>
    </Link>
  );
}
