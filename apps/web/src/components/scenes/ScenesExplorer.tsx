"use client";

import { useState, useMemo } from "react";
import { RoomSceneListItem } from "@/types/scenes";
import { SceneCard } from "./SceneCard";
import { PackageOpen, Sparkles, Filter } from "lucide-react";

interface ScenesExplorerProps {
  initialScenes: RoomSceneListItem[];
}

export function ScenesExplorer({ initialScenes }: ScenesExplorerProps) {
  const [selectedType, setSelectedType] = useState<string>("TODOS");

  const roomTypes = useMemo(() => {
    const types = new Set<string>();
    initialScenes.forEach((s) => {
      if (s.roomType) types.add(s.roomType);
    });
    return ["TODOS", ...Array.from(types)];
  }, [initialScenes]);

  const filteredScenes = useMemo(() => {
    if (selectedType === "TODOS") return initialScenes;
    return initialScenes.filter((s) => s.roomType?.toUpperCase() === selectedType.toUpperCase());
  }, [initialScenes, selectedType]);

  if (initialScenes.length === 0) {
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
    <div className="space-y-8">
      {/* Room Type Filter Tabs */}
      {roomTypes.length > 2 && (
        <div className="flex flex-wrap items-center gap-2 border-b border-zinc-200/80 pb-4 dark:border-zinc-800">
          <div className="flex items-center gap-1.5 text-xs font-semibold text-zinc-500 mr-2 dark:text-zinc-400">
            <Filter className="h-3.5 w-3.5" />
            <span>Espacios:</span>
          </div>
          {roomTypes.map((type) => {
            const isSelected = selectedType === type;
            const label = type === "TODOS" ? "Todos los Ambientes" : type;
            return (
              <button
                key={type}
                type="button"
                onClick={() => setSelectedType(type)}
                className={`rounded-full px-4 py-1.5 text-xs font-semibold transition-all ${
                  isSelected
                    ? "bg-zinc-900 text-white shadow-sm dark:bg-zinc-100 dark:text-zinc-900"
                    : "bg-white text-zinc-600 hover:bg-zinc-100 dark:bg-zinc-900 dark:text-zinc-400 dark:hover:bg-zinc-800"
                } border border-zinc-200/80 dark:border-zinc-800`}
              >
                {label}
              </button>
            );
          })}
        </div>
      )}

      {/* Grid */}
      {filteredScenes.length === 0 ? (
        <div className="flex flex-col items-center justify-center rounded-3xl border border-dashed border-zinc-200 bg-white p-12 text-center dark:border-zinc-800 dark:bg-zinc-900">
          <Sparkles className="h-8 w-8 text-amber-500" />
          <h4 className="mt-3 text-sm font-semibold text-zinc-800 dark:text-zinc-200">
            No se encontraron ambientes en la categoría {selectedType}
          </h4>
          <button
            type="button"
            onClick={() => setSelectedType("TODOS")}
            className="mt-3 text-xs font-semibold text-amber-600 hover:underline"
          >
            Ver todos los ambientes
          </button>
        </div>
      ) : (
        <div className="grid grid-cols-1 gap-8 md:grid-cols-2 lg:grid-cols-3">
          {filteredScenes.map((scene) => (
            <SceneCard key={scene.id} scene={scene} />
          ))}
        </div>
      )}
    </div>
  );
}
