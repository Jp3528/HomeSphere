"use client";

import { useEffect, useState } from "react";
import Link from "next/link";
import { useAuthStore } from "@/stores/auth-store";
import { scenesApi } from "@/lib/scenes-api";
import { RoomSceneDetail } from "@/types/scenes";
import { SceneWysiwygEditor } from "./SceneWysiwygEditor";
import { Loader2, ShieldAlert, ArrowLeft } from "lucide-react";

interface AdminSceneEditorClientProps {
  id: string;
}

export function AdminSceneEditorClient({ id }: AdminSceneEditorClientProps) {
  const { isAuthenticated, hasRole } = useAuthStore();
  const [scene, setScene] = useState<RoomSceneDetail | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const canAccess = isAuthenticated && (hasRole("Admin") || hasRole("Manager"));

  useEffect(() => {
    if (!canAccess) {
      setIsLoading(false);
      return;
    }

    const loadScene = async () => {
      try {
        setIsLoading(true);
        setError(null);
        const data = await scenesApi.getAdminSceneById(id);
        setScene(data);
      } catch (err: unknown) {
        setError(err instanceof Error ? err.message : "Error al cargar el ambiente para edición.");
      } finally {
        setIsLoading(false);
      }
    };

    loadScene();
  }, [id, canAccess]);

  if (!canAccess && !isLoading) {
    return (
      <div className="min-h-screen bg-zinc-950 flex items-center justify-center p-4">
        <div className="max-w-md w-full rounded-3xl border border-zinc-800 bg-zinc-900 p-8 text-center shadow-lg">
          <ShieldAlert className="h-12 w-12 text-rose-500 mx-auto" />
          <h2 className="mt-4 text-xl font-bold text-white">Acceso Restringido</h2>
          <p className="mt-2 text-xs text-zinc-400">
            Esta sección administrativa requiere permisos de Administrador o Gestor de Catálogo.
          </p>
          <Link
            href="/login"
            className="mt-6 inline-flex rounded-xl bg-amber-700 px-5 py-2.5 text-xs font-semibold text-white hover:bg-amber-600 transition-colors"
          >
            Iniciar Sesión
          </Link>
        </div>
      </div>
    );
  }

  if (isLoading) {
    return (
      <div className="min-h-screen bg-zinc-950 flex flex-col items-center justify-center gap-3">
        <Loader2 className="h-8 w-8 animate-spin text-amber-500" />
        <p className="text-xs text-zinc-400 font-medium">Cargando editor de ambiente...</p>
      </div>
    );
  }

  if (error || !scene) {
    return (
      <div className="min-h-screen bg-zinc-950 flex items-center justify-center p-4">
        <div className="max-w-md w-full rounded-3xl border border-zinc-800 bg-zinc-900 p-8 text-center shadow-lg">
          <h2 className="text-lg font-bold text-white">Ambiente No Encontrado</h2>
          <p className="mt-2 text-xs text-zinc-400">
            {error || "El identificador del ambiente especificado no existe o fue eliminado."}
          </p>
          <Link
            href="/admin/scenes"
            className="mt-6 inline-flex items-center gap-2 rounded-xl bg-zinc-800 px-4 py-2 text-xs font-semibold text-white hover:bg-zinc-700"
          >
            <ArrowLeft className="h-4 w-4" />
            <span>Volver a Ambientes</span>
          </Link>
        </div>
      </div>
    );
  }

  return <SceneWysiwygEditor initialScene={scene} />;
}
