"use client";

import { useEffect, useState } from "react";
import Link from "next/link";
import Image from "next/image";
import { useRouter } from "next/navigation";
import { useAuthStore } from "@/stores/auth-store";
import { scenesApi } from "@/lib/scenes-api";
import { RoomSceneListItem, CreateRoomScenePayload } from "@/types/scenes";
import {
  Sparkles,
  Plus,
  Compass,
  Trash2,
  ExternalLink,
  Loader2,
  ShieldAlert,
  Image as ImageIcon,
  Tag,
  CheckCircle2,
  XCircle,
  X,
} from "lucide-react";

export default function AdminScenesPage() {
  const router = useRouter();
  const { user, isAuthenticated, hasRole } = useAuthStore();
  const [scenes, setScenes] = useState<RoomSceneListItem[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  // New Scene Modal
  const [isModalOpen, setIsModalOpen] = useState(false);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [modalError, setModalError] = useState<string | null>(null);
  const [form, setForm] = useState<CreateRoomScenePayload>({
    title: "",
    slug: "",
    description: "",
    imageUrl: "",
    roomType: "Living",
    displayOrder: 1,
    isActive: true,
  });

  const canAccess = isAuthenticated && (hasRole("Admin") || hasRole("Manager"));

  const loadScenes = async () => {
    try {
      setIsLoading(true);
      setError(null);
      const data = await scenesApi.getAdminScenes();
      setScenes(data);
    } catch (err: unknown) {
      setError(err instanceof Error ? err.message : "Error al cargar los ambientes.");
    } finally {
      setIsLoading(false);
    }
  };

  useEffect(() => {
    if (canAccess) {
      loadScenes();
    } else {
      setIsLoading(false);
    }
  }, [canAccess]);

  const handleTitleChange = (val: string) => {
    const slug = val
      .toLowerCase()
      .trim()
      .replace(/[^\w\s-]/g, "")
      .replace(/[\s_-]+/g, "-")
      .replace(/^-+|-+$/g, "");

    setForm((prev) => ({
      ...prev,
      title: val,
      slug: prev.slug === "" || prev.slug === form.slug ? slug : prev.slug,
    }));
  };

  const handleCreateScene = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!form.title || !form.slug || !form.imageUrl || !form.description) {
      setModalError("Por favor completa todos los campos requeridos.");
      return;
    }

    try {
      setIsSubmitting(true);
      setModalError(null);
      const created = await scenesApi.createScene(form);
      setIsModalOpen(false);
      router.push(`/admin/scenes/${created.id}/editor`);
    } catch (err: unknown) {
      setModalError(err instanceof Error ? err.message : "Error al crear el ambiente.");
    } finally {
      setIsSubmitting(false);
    }
  };

  const handleDeleteScene = async (id: string, title: string) => {
    if (!confirm(`¿Estás seguro de eliminar el ambiente "${title}" y todos sus puntos interactivos?`)) {
      return;
    }

    try {
      await scenesApi.deleteScene(id);
      setScenes((prev) => prev.filter((s) => s.id !== id));
    } catch (err: unknown) {
      alert(err instanceof Error ? err.message : "No se pudo eliminar el ambiente.");
    }
  };

  if (!canAccess && !isLoading) {
    return (
      <div className="min-h-screen bg-zinc-50 flex items-center justify-center p-4 dark:bg-zinc-950">
        <div className="max-w-md w-full rounded-3xl border border-zinc-200 bg-white p-8 text-center shadow-lg dark:border-zinc-800 dark:bg-zinc-900">
          <ShieldAlert className="h-12 w-12 text-rose-500 mx-auto" />
          <h2 className="mt-4 text-xl font-bold text-zinc-900 dark:text-zinc-100">
            Acceso Restringido
          </h2>
          <p className="mt-2 text-xs text-zinc-500 dark:text-zinc-400">
            Esta sección administrativa requiere permisos de Administrador o Gestor de Catálogo.
          </p>
          <Link
            href="/login"
            className="mt-6 inline-flex rounded-xl bg-zinc-900 px-5 py-2.5 text-xs font-semibold text-white hover:bg-amber-700 transition-colors dark:bg-zinc-100 dark:text-zinc-900"
          >
            Iniciar Sesión
          </Link>
        </div>
      </div>
    );
  }

  return (
    <div className="min-h-screen bg-zinc-50/50 py-10 dark:bg-zinc-950">
      <div className="mx-auto max-w-7xl px-4 sm:px-6 lg:px-8 space-y-8">
        {/* Top Header */}
        <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4">
          <div>
            <div className="flex items-center gap-2 text-xs font-semibold text-amber-700 uppercase dark:text-amber-400">
              <Sparkles className="h-4 w-4" />
              <span>Panel de Administración</span>
            </div>
            <h1 className="mt-1 text-2xl font-extrabold tracking-tight text-zinc-900 sm:text-3xl dark:text-zinc-50">
              Gestor de Ambientes Interactivos
            </h1>
            <p className="mt-1 text-xs text-zinc-500 dark:text-zinc-400">
              Crea espacios de estilismo, define puntos de interés (hotspots) y vincula productos a ambientes reales.
            </p>
          </div>

          <button
            type="button"
            onClick={() => {
              setForm({
                title: "",
                slug: "",
                description: "",
                imageUrl: "",
                roomType: "Living",
                displayOrder: scenes.length + 1,
                isActive: true,
              });
              setIsModalOpen(true);
            }}
            className="inline-flex items-center gap-2 rounded-2xl bg-amber-700 px-4 py-2.5 text-xs font-bold text-white shadow-md hover:bg-amber-600 transition-all cursor-pointer"
          >
            <Plus className="h-4 w-4" />
            <span>Nuevo Ambiente</span>
          </button>
        </div>

        {/* Content Table / Cards */}
        {isLoading ? (
          <div className="flex h-64 items-center justify-center">
            <Loader2 className="h-8 w-8 animate-spin text-amber-600" />
          </div>
        ) : error ? (
          <div className="rounded-2xl border border-rose-200 bg-rose-50 p-6 text-center text-xs text-rose-700 dark:border-rose-900/50 dark:bg-rose-950/20 dark:text-rose-400">
            {error}
          </div>
        ) : scenes.length === 0 ? (
          <div className="flex flex-col items-center justify-center rounded-3xl border border-dashed border-zinc-200 bg-white p-12 text-center dark:border-zinc-800 dark:bg-zinc-900">
            <Compass className="h-12 w-12 text-zinc-400" />
            <h3 className="mt-4 text-base font-semibold text-zinc-900 dark:text-zinc-100">
              No hay ambientes configurados
            </h3>
            <p className="mt-1 text-xs text-zinc-500 dark:text-zinc-400">
              Haz clic en &ldquo;Nuevo Ambiente&rdquo; para cargar la fotografía de un salón, comedor o dormitorio y mapear sus piezas.
            </p>
          </div>
        ) : (
          <div className="grid grid-cols-1 gap-6 md:grid-cols-2 lg:grid-cols-3">
            {scenes.map((scene) => (
              <div
                key={scene.id}
                className="group relative flex flex-col overflow-hidden rounded-3xl border border-zinc-200/80 bg-white shadow-xs transition-all hover:shadow-lg dark:border-zinc-800 dark:bg-zinc-900"
              >
                {/* Image */}
                <div className="relative aspect-16/10 w-full overflow-hidden bg-zinc-100 dark:bg-zinc-800">
                  <Image
                    src={scene.imageUrl}
                    alt={scene.title}
                    fill
                    sizes="(max-width: 768px) 100vw, 33vw"
                    className="object-cover transition-transform duration-500 group-hover:scale-105"
                  />
                  <div className="absolute top-3 left-3 flex items-center gap-1 rounded-full bg-black/60 px-2.5 py-1 text-[11px] font-semibold text-white backdrop-blur-md">
                    <Tag className="h-3 w-3" />
                    <span>{scene.roomType || "Living"}</span>
                  </div>

                  <div className="absolute top-3 right-3 flex items-center gap-1 rounded-full bg-white/90 px-2.5 py-1 text-[11px] font-semibold text-zinc-900 shadow-sm backdrop-blur-md dark:bg-zinc-900/90 dark:text-zinc-100">
                    <Sparkles className="h-3 w-3 text-amber-600" />
                    <span>{scene.hotspotCount} hotspots</span>
                  </div>
                </div>

                {/* Body */}
                <div className="flex flex-1 flex-col p-5">
                  <div className="flex items-center justify-between gap-2">
                    <h3 className="text-base font-bold text-zinc-900 line-clamp-1 dark:text-zinc-100">
                      {scene.title}
                    </h3>
                    {scene.isActive ? (
                      <span className="inline-flex items-center gap-1 text-[11px] font-medium text-emerald-600">
                        <CheckCircle2 className="h-3.5 w-3.5" />
                        Activo
                      </span>
                    ) : (
                      <span className="inline-flex items-center gap-1 text-[11px] font-medium text-zinc-400">
                        <XCircle className="h-3.5 w-3.5" />
                        Borrador
                      </span>
                    )}
                  </div>

                  <p className="mt-1 line-clamp-2 text-xs text-zinc-500 dark:text-zinc-400">
                    {scene.description}
                  </p>

                  <div className="mt-auto pt-4 flex items-center justify-between border-t border-zinc-100 dark:border-zinc-800">
                    <div className="flex items-center gap-2">
                      <Link
                        href={`/admin/scenes/${scene.id}/editor`}
                        className="inline-flex items-center gap-1.5 rounded-xl bg-zinc-900 px-3 py-1.5 text-xs font-semibold text-white hover:bg-amber-700 transition-colors dark:bg-zinc-100 dark:text-zinc-900 dark:hover:bg-amber-600 dark:hover:text-white"
                      >
                        <Compass className="h-3.5 w-3.5" />
                        <span>Editor Visual</span>
                      </Link>

                      <Link
                        href={`/scenes/${scene.slug}`}
                        target="_blank"
                        className="flex h-8 w-8 items-center justify-center rounded-xl border border-zinc-200 text-zinc-600 hover:bg-zinc-100 transition-colors dark:border-zinc-700 dark:text-zinc-400"
                        title="Ver vista pública"
                      >
                        <ExternalLink className="h-3.5 w-3.5" />
                      </Link>
                    </div>

                    <button
                      type="button"
                      onClick={() => handleDeleteScene(scene.id, scene.title)}
                      className="flex h-8 w-8 items-center justify-center rounded-xl text-rose-500 hover:bg-rose-50 transition-colors dark:hover:bg-rose-950/30"
                      title="Eliminar ambiente"
                    >
                      <Trash2 className="h-4 w-4" />
                    </button>
                  </div>
                </div>
              </div>
            ))}
          </div>
        )}

        {/* Modal: Nuevo Ambiente */}
        {isModalOpen && (
          <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50 p-4 backdrop-blur-xs">
            <div className="relative w-full max-w-xl rounded-3xl border border-zinc-200 bg-white p-6 shadow-2xl dark:border-zinc-800 dark:bg-zinc-900 max-h-[90vh] overflow-y-auto">
              <div className="flex items-center justify-between border-b border-zinc-100 pb-4 dark:border-zinc-800">
                <div className="flex items-center gap-2">
                  <div className="flex h-8 w-8 items-center justify-center rounded-xl bg-amber-50 text-amber-700 dark:bg-amber-950 dark:text-amber-400">
                    <Plus className="h-4 w-4" />
                  </div>
                  <h3 className="text-base font-bold text-zinc-900 dark:text-zinc-100">
                    Crear Nuevo Ambiente Interactivo
                  </h3>
                </div>
                <button
                  type="button"
                  onClick={() => setIsModalOpen(false)}
                  className="rounded-full p-1.5 text-zinc-400 hover:bg-zinc-100 dark:hover:bg-zinc-800"
                >
                  <X className="h-4 w-4" />
                </button>
              </div>

              {modalError && (
                <div className="mt-4 rounded-xl bg-rose-50 p-3 text-xs text-rose-600 dark:bg-rose-950/30 dark:text-rose-400">
                  {modalError}
                </div>
              )}

              <form onSubmit={handleCreateScene} className="mt-5 space-y-4">
                <div>
                  <label className="block text-xs font-semibold text-zinc-700 dark:text-zinc-300">
                    Título del Ambiente *
                  </label>
                  <input
                    type="text"
                    required
                    value={form.title}
                    onChange={(e) => handleTitleChange(e.target.value)}
                    placeholder="Ej. Salón Moderno Japandi"
                    className="mt-1.5 w-full rounded-xl border border-zinc-300 bg-white px-3.5 py-2 text-xs text-zinc-900 focus:border-amber-600 focus:outline-none dark:border-zinc-700 dark:bg-zinc-800 dark:text-zinc-100"
                  />
                </div>

                <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
                  <div>
                    <label className="block text-xs font-semibold text-zinc-700 dark:text-zinc-300">
                      Slug URL *
                    </label>
                    <input
                      type="text"
                      required
                      value={form.slug}
                      onChange={(e) => setForm((prev) => ({ ...prev, slug: e.target.value }))}
                      placeholder="salon-moderno-japandi"
                      className="mt-1.5 w-full rounded-xl border border-zinc-300 bg-white px-3.5 py-2 text-xs text-zinc-900 focus:border-amber-600 focus:outline-none dark:border-zinc-700 dark:bg-zinc-800 dark:text-zinc-100"
                    />
                  </div>

                  <div>
                    <label className="block text-xs font-semibold text-zinc-700 dark:text-zinc-300">
                      Tipo de Habitación
                    </label>
                    <select
                      value={form.roomType}
                      onChange={(e) => setForm((prev) => ({ ...prev, roomType: e.target.value }))}
                      className="mt-1.5 w-full rounded-xl border border-zinc-300 bg-white px-3.5 py-2 text-xs text-zinc-900 focus:border-amber-600 focus:outline-none dark:border-zinc-700 dark:bg-zinc-800 dark:text-zinc-100"
                    >
                      <option value="Living">Living / Sala de Estar</option>
                      <option value="Comedor">Comedor</option>
                      <option value="Dormitorio">Dormitorio</option>
                      <option value="Terraza">Terraza / Exterior</option>
                      <option value="Estudio">Estudio / Oficina</option>
                    </select>
                  </div>
                </div>

                <div>
                  <label className="block text-xs font-semibold text-zinc-700 dark:text-zinc-300">
                    URL de Imagen de Alta Resolución (1920x1080 recomendado) *
                  </label>
                  <input
                    type="url"
                    required
                    value={form.imageUrl}
                    onChange={(e) => setForm((prev) => ({ ...prev, imageUrl: e.target.value }))}
                    placeholder="https://images.unsplash.com/..."
                    className="mt-1.5 w-full rounded-xl border border-zinc-300 bg-white px-3.5 py-2 text-xs text-zinc-900 focus:border-amber-600 focus:outline-none dark:border-zinc-700 dark:bg-zinc-800 dark:text-zinc-100"
                  />
                  {form.imageUrl && (
                    <div className="mt-2 relative aspect-16/9 w-full overflow-hidden rounded-xl border border-zinc-200 bg-zinc-100 dark:border-zinc-800 dark:bg-zinc-800">
                      <Image
                        src={form.imageUrl}
                        alt="Preview"
                        fill
                        sizes="500px"
                        className="object-cover"
                      />
                    </div>
                  )}
                </div>

                <div>
                  <label className="block text-xs font-semibold text-zinc-700 dark:text-zinc-300">
                    Descripción / Concepto de Diseño *
                  </label>
                  <textarea
                    rows={3}
                    required
                    value={form.description}
                    onChange={(e) => setForm((prev) => ({ ...prev, description: e.target.value }))}
                    placeholder="Describe los materiales, paleta de colores y atmósfera del espacio..."
                    className="mt-1.5 w-full rounded-xl border border-zinc-300 bg-white px-3.5 py-2 text-xs text-zinc-900 focus:border-amber-600 focus:outline-none dark:border-zinc-700 dark:bg-zinc-800 dark:text-zinc-100"
                  />
                </div>

                <div className="flex items-center gap-4 pt-2">
                  <label className="flex items-center gap-2 cursor-pointer text-xs font-semibold text-zinc-700 dark:text-zinc-300">
                    <input
                      type="checkbox"
                      checked={form.isActive}
                      onChange={(e) => setForm((prev) => ({ ...prev, isActive: e.target.checked }))}
                      className="rounded border-zinc-300 text-amber-600 focus:ring-amber-500"
                    />
                    <span>Publicar inmediatamente (Activo)</span>
                  </label>
                </div>

                <div className="mt-6 flex justify-end gap-3 border-t border-zinc-100 pt-4 dark:border-zinc-800">
                  <button
                    type="button"
                    onClick={() => setIsModalOpen(false)}
                    className="rounded-xl border border-zinc-200 px-4 py-2 text-xs font-semibold text-zinc-600 hover:bg-zinc-100 dark:border-zinc-700 dark:text-zinc-300 dark:hover:bg-zinc-800"
                  >
                    Cancelar
                  </button>
                  <button
                    type="submit"
                    disabled={isSubmitting}
                    className="inline-flex items-center gap-2 rounded-xl bg-amber-700 px-5 py-2 text-xs font-bold text-white hover:bg-amber-600 disabled:opacity-50"
                  >
                    {isSubmitting ? (
                      <>
                        <Loader2 className="h-3.5 w-3.5 animate-spin" />
                        <span>Creando...</span>
                      </>
                    ) : (
                      <span>Crear y Abrir Editor</span>
                    )}
                  </button>
                </div>
              </form>
            </div>
          </div>
        )}
      </div>
    </div>
  );
}
