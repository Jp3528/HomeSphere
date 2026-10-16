"use client";

import { useState, useRef, useEffect } from "react";
import Image from "next/image";
import Link from "next/link";
import { scenesApi } from "@/lib/scenes-api";
import { apiClient } from "@/lib/api-client";
import {
  RoomSceneDetail,
  SceneHotspot,
  CreateHotspotPayload,
  UpdateHotspotPayload,
  UpdateRoomScenePayload,
} from "@/types/scenes";
import { ProductListItem, ProductDetail, PagedResult } from "@/types/catalog";
import { InteractiveSceneView } from "@/components/scenes/InteractiveSceneView";
import {
  ArrowLeft,
  Eye,
  Sliders,
  Plus,
  Trash2,
  Save,
  X,
  Search,
  CheckCircle2,
  Loader2,
  Sparkles,
  ExternalLink,
  MapPin,
  Move,
  Tag,
  Package,
} from "lucide-react";

interface SceneWysiwygEditorProps {
  initialScene: RoomSceneDetail;
}

export function SceneWysiwygEditor({ initialScene }: SceneWysiwygEditorProps) {
  const [scene, setScene] = useState<RoomSceneDetail>(initialScene);
  const [mode, setMode] = useState<"edit" | "preview">("edit");
  const [selectedHotspot, setSelectedHotspot] = useState<SceneHotspot | null>(null);
  const [newPinCoords, setNewPinCoords] = useState<{ x: number; y: number } | null>(null);

  // Hotspot Drawer Form State
  const [isHotspotDrawerOpen, setIsHotspotDrawerOpen] = useState(false);
  const [hotspotForm, setHotspotForm] = useState<{
    x: number;
    y: number;
    productId: string;
    productVariantId: string | undefined;
    title: string;
    displayOrder: number;
    isActive: boolean;
  }>({
    x: 50,
    y: 50,
    productId: "",
    productVariantId: undefined,
    title: "",
    displayOrder: 1,
    isActive: true,
  });

  // Product Search State
  const [productSearch, setProductSearch] = useState("");
  const [searchResults, setSearchResults] = useState<ProductListItem[]>([]);
  const [isSearchingProducts, setIsSearchingProducts] = useState(false);
  const [selectedProductDetail, setSelectedProductDetail] = useState<ProductDetail | null>(null);
  const [isLoadingProductDetail, setIsLoadingProductDetail] = useState(false);
  const [isSavingHotspot, setIsSavingHotspot] = useState(false);
  const [isDeletingHotspot, setIsDeletingHotspot] = useState(false);

  // Scene Settings Drawer State
  const [isSettingsOpen, setIsSettingsOpen] = useState(false);
  const [isSavingSettings, setIsSavingSettings] = useState(false);
  const [settingsSuccess, setSettingsSuccess] = useState(false);
  const [settingsForm, setSettingsForm] = useState<UpdateRoomScenePayload>({
    title: scene.title,
    slug: scene.slug,
    description: scene.description,
    imageUrl: scene.imageUrl,
    roomType: scene.roomType || "Living",
    displayOrder: scene.displayOrder,
    isActive: scene.isActive,
  });

  const canvasRef = useRef<HTMLDivElement>(null);

  // Refresh scene data
  const refreshScene = async () => {
    try {
      const updated = await scenesApi.getAdminSceneById(scene.id);
      setScene(updated);
    } catch {
      // Keep existing on error
    }
  };

  // Search products debounced
  useEffect(() => {
    if (!productSearch.trim()) {
      setSearchResults([]);
      return;
    }

    const timer = setTimeout(async () => {
      setIsSearchingProducts(true);
      try {
        const res = await apiClient.get<PagedResult<ProductListItem>>("/api/products", {
          params: { search: productSearch.trim(), pageSize: 8 },
        });
        setSearchResults(res.items);
      } catch {
        setSearchResults([]);
      } finally {
        setIsSearchingProducts(false);
      }
    }, 300);

    return () => clearTimeout(timer);
  }, [productSearch]);

  // Load product detail and variants when product is selected
  const handleSelectProduct = async (product: ProductListItem) => {
    setHotspotForm((prev) => ({
      ...prev,
      productId: product.id,
      productVariantId: undefined,
      title: prev.title || product.name,
    }));

    try {
      setIsLoadingProductDetail(true);
      const detail = await apiClient.get<ProductDetail>(`/api/products/${product.slug}`);
      setSelectedProductDetail(detail);

      // Auto-select first variant if available
      if (detail.variants && detail.variants.length > 0) {
        setHotspotForm((prev) => ({
          ...prev,
          productVariantId: detail.variants[0].id,
        }));
      }
    } catch {
      setSelectedProductDetail(null);
    } finally {
      setIsLoadingProductDetail(false);
    }
  };

  // Canvas click handler
  const handleCanvasClick = (e: React.MouseEvent<HTMLDivElement>) => {
    if (mode !== "edit" || !canvasRef.current) return;

    const rect = canvasRef.current.getBoundingClientRect();
    const clickX = parseFloat((((e.clientX - rect.left) / rect.width) * 100).toFixed(2));
    const clickY = parseFloat((((e.clientY - rect.top) / rect.height) * 100).toFixed(2));

    // Constrain to 0-100%
    const boundedX = Math.max(1, Math.min(99, clickX));
    const boundedY = Math.max(1, Math.min(99, clickY));

    setSelectedHotspot(null);
    setNewPinCoords({ x: boundedX, y: boundedY });
    setHotspotForm({
      x: boundedX,
      y: boundedY,
      productId: "",
      productVariantId: undefined,
      title: "",
      displayOrder: scene.hotspots.length + 1,
      isActive: true,
    });
    setSelectedProductDetail(null);
    setProductSearch("");
    setIsHotspotDrawerOpen(true);
  };

  // Open existing hotspot for editing
  const handleEditHotspot = async (hotspot: SceneHotspot, e: React.MouseEvent) => {
    e.stopPropagation();
    setSelectedHotspot(hotspot);
    setNewPinCoords(null);
    setHotspotForm({
      x: hotspot.xCoordinate,
      y: hotspot.yCoordinate,
      productId: hotspot.productId,
      productVariantId: hotspot.productVariantId,
      title: hotspot.title || hotspot.productName,
      displayOrder: hotspot.displayOrder,
      isActive: hotspot.isActive,
    });

    setIsHotspotDrawerOpen(true);

    // Fetch product detail to populate variants
    try {
      setIsLoadingProductDetail(true);
      const detail = await apiClient.get<ProductDetail>(`/api/products/${hotspot.productSlug}`);
      setSelectedProductDetail(detail);
    } catch {
      setSelectedProductDetail(null);
    } finally {
      setIsLoadingProductDetail(false);
    }
  };

  // Save Hotspot (Create or Update)
  const handleSaveHotspot = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!hotspotForm.productId) {
      alert("Por favor selecciona un producto para vincular al punto interactivo.");
      return;
    }

    try {
      setIsSavingHotspot(true);

      if (selectedHotspot) {
        // Update
        const payload: UpdateHotspotPayload = {
          productId: hotspotForm.productId,
          productVariantId: hotspotForm.productVariantId,
          xCoordinate: hotspotForm.x,
          yCoordinate: hotspotForm.y,
          title: hotspotForm.title.trim() || undefined,
          displayOrder: hotspotForm.displayOrder,
          isActive: hotspotForm.isActive,
        };
        await scenesApi.updateHotspot(scene.id, selectedHotspot.id, payload);
      } else {
        // Create
        const payload: CreateHotspotPayload = {
          productId: hotspotForm.productId,
          productVariantId: hotspotForm.productVariantId,
          xCoordinate: hotspotForm.x,
          yCoordinate: hotspotForm.y,
          title: hotspotForm.title.trim() || undefined,
          displayOrder: hotspotForm.displayOrder,
          isActive: hotspotForm.isActive,
        };
        await scenesApi.addHotspot(scene.id, payload);
      }

      await refreshScene();
      setIsHotspotDrawerOpen(false);
      setSelectedHotspot(null);
      setNewPinCoords(null);
    } catch (err: unknown) {
      alert(err instanceof Error ? err.message : "Error al guardar el punto interactivo.");
    } finally {
      setIsSavingHotspot(false);
    }
  };

  // Delete Hotspot
  const handleDeleteHotspot = async () => {
    if (!selectedHotspot) return;
    if (!confirm(`¿Eliminar el punto interactivo "${selectedHotspot.title || selectedHotspot.productName}"?`)) {
      return;
    }

    try {
      setIsDeletingHotspot(true);
      await scenesApi.deleteHotspot(scene.id, selectedHotspot.id);
      await refreshScene();
      setIsHotspotDrawerOpen(false);
      setSelectedHotspot(null);
    } catch (err: unknown) {
      alert(err instanceof Error ? err.message : "Error al eliminar el hotspot.");
    } finally {
      setIsDeletingHotspot(false);
    }
  };

  // Save Scene Settings
  const handleSaveSettings = async (e: React.FormEvent) => {
    e.preventDefault();
    try {
      setIsSavingSettings(true);
      setSettingsSuccess(false);
      const updated = await scenesApi.updateScene(scene.id, settingsForm);
      setScene(updated);
      setSettingsSuccess(true);
      setTimeout(() => setSettingsSuccess(false), 3000);
    } catch (err: unknown) {
      alert(err instanceof Error ? err.message : "Error al guardar la configuración del ambiente.");
    } finally {
      setIsSavingSettings(false);
    }
  };

  return (
    <div className="min-h-screen bg-zinc-950 text-zinc-100 flex flex-col">
      {/* Top Navbar */}
      <header className="sticky top-0 z-40 flex items-center justify-between border-b border-zinc-800 bg-zinc-900/90 px-4 py-3 backdrop-blur-md sm:px-6">
        <div className="flex items-center gap-3">
          <Link
            href="/admin/scenes"
            className="flex h-9 w-9 items-center justify-center rounded-xl border border-zinc-700 bg-zinc-800 text-zinc-300 hover:bg-zinc-700 hover:text-white transition-colors"
            title="Volver al catálogo de ambientes"
          >
            <ArrowLeft className="h-4 w-4" />
          </Link>

          <div>
            <div className="flex items-center gap-2">
              <h1 className="text-sm font-bold text-white sm:text-base line-clamp-1">
                {scene.title}
              </h1>
              <span className="rounded-full bg-amber-500/20 px-2 py-0.5 text-[10px] font-bold text-amber-400">
                {scene.roomType || "Living"}
              </span>
            </div>
            <p className="text-[11px] text-zinc-400">
              {scene.hotspots.length} {scene.hotspots.length === 1 ? "punto configurado" : "puntos configurados"}
            </p>
          </div>
        </div>

        {/* Toolbar Center: Mode Switcher */}
        <div className="flex items-center rounded-2xl bg-zinc-800/90 p-1 border border-zinc-700">
          <button
            type="button"
            onClick={() => {
              setMode("edit");
              setSelectedHotspot(null);
            }}
            className={`flex items-center gap-1.5 rounded-xl px-3 py-1.5 text-xs font-semibold transition-all ${
              mode === "edit"
                ? "bg-amber-700 text-white shadow-sm"
                : "text-zinc-400 hover:text-zinc-200"
            }`}
          >
            <MapPin className="h-3.5 w-3.5" />
            <span>Editor Visual</span>
          </button>

          <button
            type="button"
            onClick={() => {
              setMode("preview");
              setIsHotspotDrawerOpen(false);
              setNewPinCoords(null);
            }}
            className={`flex items-center gap-1.5 rounded-xl px-3 py-1.5 text-xs font-semibold transition-all ${
              mode === "preview"
                ? "bg-amber-700 text-white shadow-sm"
                : "text-zinc-400 hover:text-zinc-200"
            }`}
          >
            <Eye className="h-3.5 w-3.5" />
            <span>Vista Previa en Vivo</span>
          </button>
        </div>

        {/* Actions */}
        <div className="flex items-center gap-2">
          <button
            type="button"
            onClick={() => setIsSettingsOpen(true)}
            className="flex items-center gap-1.5 rounded-xl border border-zinc-700 bg-zinc-800 px-3 py-2 text-xs font-semibold text-zinc-200 hover:bg-zinc-700 hover:text-white transition-colors"
          >
            <Sliders className="h-3.5 w-3.5" />
            <span className="hidden sm:inline">Configurar Ambiente</span>
          </button>

          <Link
            href={`/scenes/${scene.slug}`}
            target="_blank"
            className="flex h-9 w-9 items-center justify-center rounded-xl border border-zinc-700 bg-zinc-800 text-zinc-300 hover:bg-zinc-700 hover:text-white transition-colors"
            title="Abrir página pública en nueva pestaña"
          >
            <ExternalLink className="h-4 w-4" />
          </Link>
        </div>
      </header>

      {/* Main Workspace */}
      <main className="flex-1 p-4 sm:p-6 lg:p-8 flex flex-col items-center justify-center">
        {mode === "preview" ? (
          /* Live Customer View */
          <div className="w-full max-w-7xl rounded-3xl border border-zinc-800 bg-zinc-900/60 p-6 shadow-2xl backdrop-blur-md">
            <div className="mb-4 flex items-center justify-between border-b border-zinc-800 pb-3">
              <div className="flex items-center gap-2 text-xs font-semibold text-amber-400">
                <Sparkles className="h-4 w-4" />
                <span>Simulación de la Experiencia del Cliente</span>
              </div>
              <span className="text-xs text-zinc-400">
                Prueba cómo interactúa el comprador con los hotspots y el carrito
              </span>
            </div>
            <InteractiveSceneView scene={scene} />
          </div>
        ) : (
          /* Interactive WYSIWYG Pin Canvas */
          <div className="w-full max-w-6xl space-y-3">
            <div className="flex items-center justify-between text-xs text-zinc-400 px-1">
              <span className="flex items-center gap-1.5">
                <Move className="h-3.5 w-3.5 text-amber-500" />
                <strong>Haz clic sobre la fotografía</strong> para ubicar un nuevo punto interactivo o haz clic en un pin existente para editarlo.
              </span>
              <span>{scene.hotspots.length} pines colocados</span>
            </div>

            <div
              ref={canvasRef}
              onClick={handleCanvasClick}
              className="relative aspect-16/10 w-full overflow-hidden rounded-3xl border-2 border-zinc-800 bg-zinc-900 shadow-2xl cursor-crosshair select-none group"
            >
              {/* Background Scene Image */}
              <Image
                src={scene.imageUrl}
                alt={scene.title}
                fill
                priority
                sizes="(max-width: 1280px) 100vw, 1200px"
                className="object-cover pointer-events-none"
              />

              {/* Dark Overlay on hover */}
              <div className="pointer-events-none absolute inset-0 bg-black/20 group-hover:bg-black/10 transition-colors" />

              {/* Existing Hotspot Pins */}
              {scene.hotspots.map((hotspot, idx) => {
                const isSelected = selectedHotspot?.id === hotspot.id;
                return (
                  <div
                    key={hotspot.id}
                    style={{
                      left: `${hotspot.xCoordinate}%`,
                      top: `${hotspot.yCoordinate}%`,
                    }}
                    onClick={(e) => handleEditHotspot(hotspot, e)}
                    className="absolute -translate-x-1/2 -translate-y-1/2 z-20 cursor-pointer"
                  >
                    <div
                      className={`group/pin relative flex h-10 w-10 items-center justify-center rounded-full transition-transform hover:scale-110 ${
                        isSelected ? "scale-110 ring-4 ring-amber-400" : ""
                      }`}
                    >
                      <span
                        className={`absolute inline-flex h-full w-full rounded-full opacity-75 ${
                          isSelected ? "bg-amber-400 animate-ping" : "bg-white"
                        }`}
                      />
                      <span
                        className={`relative flex h-8 w-8 items-center justify-center rounded-full text-xs font-extrabold shadow-lg transition-colors ${
                          isSelected
                            ? "bg-amber-600 text-white"
                            : "bg-white text-zinc-950 group-hover/pin:bg-amber-500 group-hover/pin:text-white"
                        }`}
                      >
                        {idx + 1}
                      </span>
                    </div>

                    {/* Mini Hover Label */}
                    <div className="pointer-events-none absolute left-1/2 top-full mt-1.5 -translate-x-1/2 whitespace-nowrap rounded-lg bg-black/90 px-2 py-0.5 text-[10px] font-semibold text-white shadow-md backdrop-blur-xs opacity-0 transition-opacity group-hover/pin:opacity-100">
                      {hotspot.title || hotspot.productName}
                    </div>
                  </div>
                );
              })}

              {/* Prospective New Pin Indicator */}
              {newPinCoords && (
                <div
                  style={{
                    left: `${newPinCoords.x}%`,
                    top: `${newPinCoords.y}%`,
                  }}
                  className="pointer-events-none absolute -translate-x-1/2 -translate-y-1/2 z-30"
                >
                  <div className="relative flex h-10 w-10 items-center justify-center rounded-full ring-4 ring-emerald-400 animate-pulse">
                    <span className="relative flex h-8 w-8 items-center justify-center rounded-full bg-emerald-500 text-xs font-bold text-white shadow-lg">
                      +
                    </span>
                  </div>
                </div>
              )}
            </div>
          </div>
        )}
      </main>

      {/* Hotspot Drawer (Slide-Over Right) */}
      {isHotspotDrawerOpen && (
        <div className="fixed inset-0 z-50 flex justify-end bg-black/50 backdrop-blur-xs">
          <div className="relative w-full max-w-md bg-zinc-900 border-l border-zinc-800 p-6 shadow-2xl flex flex-col overflow-y-auto animate-in slide-in-from-right duration-200">
            <div className="flex items-center justify-between border-b border-zinc-800 pb-4">
              <div className="flex items-center gap-2">
                <div className="flex h-8 w-8 items-center justify-center rounded-xl bg-amber-500/20 text-amber-400">
                  <MapPin className="h-4 w-4" />
                </div>
                <div>
                  <h3 className="text-sm font-bold text-white">
                    {selectedHotspot ? "Editar Punto Interactivo" : "Nuevo Punto Interactivo"}
                  </h3>
                  <p className="text-[11px] text-zinc-400">
                    Coordenadas: X: {hotspotForm.x}% · Y: {hotspotForm.y}%
                  </p>
                </div>
              </div>

              <button
                type="button"
                onClick={() => {
                  setIsHotspotDrawerOpen(false);
                  setSelectedHotspot(null);
                  setNewPinCoords(null);
                }}
                className="rounded-full p-1.5 text-zinc-400 hover:bg-zinc-800 hover:text-white"
              >
                <X className="h-4 w-4" />
              </button>
            </div>

            <form onSubmit={handleSaveHotspot} className="mt-5 space-y-5 flex-1 flex flex-col">
              {/* Coordinate Fine-Tuning */}
              <div className="rounded-2xl border border-zinc-800 bg-zinc-950/60 p-3.5 space-y-2">
                <span className="text-[11px] font-bold tracking-wider text-zinc-400 uppercase">
                  Ajuste Fino de Coordenadas
                </span>
                <div className="grid grid-cols-2 gap-3">
                  <div>
                    <label className="text-[10px] text-zinc-400 block mb-1">Eje X (%)</label>
                    <input
                      type="number"
                      step="0.1"
                      min="0"
                      max="100"
                      value={hotspotForm.x}
                      onChange={(e) =>
                        setHotspotForm((prev) => ({
                          ...prev,
                          x: parseFloat(e.target.value) || 0,
                        }))
                      }
                      className="w-full rounded-xl border border-zinc-700 bg-zinc-800 px-3 py-1.5 text-xs text-white"
                    />
                  </div>
                  <div>
                    <label className="text-[10px] text-zinc-400 block mb-1">Eje Y (%)</label>
                    <input
                      type="number"
                      step="0.1"
                      min="0"
                      max="100"
                      value={hotspotForm.y}
                      onChange={(e) =>
                        setHotspotForm((prev) => ({
                          ...prev,
                          y: parseFloat(e.target.value) || 0,
                        }))
                      }
                      className="w-full rounded-xl border border-zinc-700 bg-zinc-800 px-3 py-1.5 text-xs text-white"
                    />
                  </div>
                </div>
              </div>

              {/* Product Search & Picker */}
              <div className="space-y-2">
                <label className="block text-xs font-semibold text-zinc-200">
                  Producto Vinculado *
                </label>

                {/* Search Input */}
                <div className="relative">
                  <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-zinc-400" />
                  <input
                    type="text"
                    value={productSearch}
                    onChange={(e) => setProductSearch(e.target.value)}
                    placeholder="Buscar producto por nombre o SKU..."
                    className="w-full rounded-xl border border-zinc-700 bg-zinc-800 py-2 pl-9 pr-3 text-xs text-white placeholder:text-zinc-500 focus:border-amber-500 focus:outline-none"
                  />
                  {isSearchingProducts && (
                    <Loader2 className="absolute right-3 top-1/2 -translate-y-1/2 h-4 w-4 animate-spin text-amber-500" />
                  )}
                </div>

                {/* Search Results Dropdown */}
                {searchResults.length > 0 && (
                  <div className="max-h-48 overflow-y-auto rounded-xl border border-zinc-700 bg-zinc-800 p-1 divide-y divide-zinc-700/50 shadow-xl">
                    {searchResults.map((product) => (
                      <div
                        key={product.id}
                        onClick={() => {
                          handleSelectProduct(product);
                          setSearchResults([]);
                          setProductSearch(product.name);
                        }}
                        className="flex cursor-pointer items-center gap-3 p-2 rounded-lg hover:bg-zinc-700/60 transition-colors"
                      >
                        <div className="relative h-9 w-9 shrink-0 overflow-hidden rounded-md bg-zinc-700">
                          {product.primaryImageUrl && (
                            <Image
                              src={product.primaryImageUrl}
                              alt={product.name}
                              fill
                              sizes="36px"
                              className="object-cover"
                            />
                          )}
                        </div>
                        <div className="flex-1 overflow-hidden">
                          <p className="text-xs font-semibold text-white line-clamp-1">
                            {product.name}
                          </p>
                          <p className="text-[10px] text-amber-400 font-bold">
                            ${product.basePrice.toFixed(2)} USD
                          </p>
                        </div>
                      </div>
                    ))}
                  </div>
                )}

                {/* Selected Product Card */}
                {hotspotForm.productId && (
                  <div className="rounded-xl border border-amber-600/50 bg-amber-950/20 p-3 flex items-center justify-between">
                    <div className="flex items-center gap-2.5 overflow-hidden">
                      <Package className="h-5 w-5 text-amber-400 shrink-0" />
                      <div className="overflow-hidden">
                        <span className="text-[10px] font-bold text-amber-400 uppercase tracking-wider block">
                          Producto Seleccionado
                        </span>
                        <span className="text-xs font-semibold text-white line-clamp-1">
                          {selectedHotspot?.productName || hotspotForm.title || "Producto"}
                        </span>
                      </div>
                    </div>
                  </div>
                )}
              </div>

              {/* Variant Picker */}
              {isLoadingProductDetail ? (
                <div className="flex items-center justify-center p-4">
                  <Loader2 className="h-5 w-5 animate-spin text-amber-500" />
                </div>
              ) : selectedProductDetail && selectedProductDetail.variants.length > 0 ? (
                <div className="space-y-1.5">
                  <label className="block text-xs font-semibold text-zinc-200">
                    Variante de Acabado / Medida
                  </label>
                  <select
                    value={hotspotForm.productVariantId || ""}
                    onChange={(e) =>
                      setHotspotForm((prev) => ({
                        ...prev,
                        productVariantId: e.target.value || undefined,
                      }))
                    }
                    className="w-full rounded-xl border border-zinc-700 bg-zinc-800 px-3 py-2 text-xs text-white focus:border-amber-500 focus:outline-none"
                  >
                    <option value="">Cualquier variante / Precio base</option>
                    {selectedProductDetail.variants.map((v) => (
                      <option key={v.id} value={v.id}>
                        {v.name} · ${v.price.toFixed(2)} USD ({v.stockQuantity} unid. en stock)
                      </option>
                    ))}
                  </select>
                </div>
              ) : null}

              {/* Title Override */}
              <div>
                <label className="block text-xs font-semibold text-zinc-200">
                  Título Personalizado del Punto (Opcional)
                </label>
                <input
                  type="text"
                  value={hotspotForm.title}
                  onChange={(e) => setHotspotForm((prev) => ({ ...prev, title: e.target.value }))}
                  placeholder="Ej. Sofá 3 Cuerpos en Lino Crudo"
                  className="mt-1 w-full rounded-xl border border-zinc-700 bg-zinc-800 px-3.5 py-2 text-xs text-white placeholder:text-zinc-500 focus:border-amber-500 focus:outline-none"
                />
              </div>

              {/* Display Order */}
              <div>
                <label className="block text-xs font-semibold text-zinc-200">
                  Orden de Visualización (#)
                </label>
                <input
                  type="number"
                  min="1"
                  value={hotspotForm.displayOrder}
                  onChange={(e) =>
                    setHotspotForm((prev) => ({
                      ...prev,
                      displayOrder: parseInt(e.target.value) || 1,
                    }))
                  }
                  className="mt-1 w-full rounded-xl border border-zinc-700 bg-zinc-800 px-3.5 py-2 text-xs text-white focus:border-amber-500 focus:outline-none"
                />
              </div>

              {/* Active Toggle */}
              <div className="pt-1">
                <label className="flex items-center gap-2 cursor-pointer text-xs font-semibold text-zinc-300">
                  <input
                    type="checkbox"
                    checked={hotspotForm.isActive}
                    onChange={(e) =>
                      setHotspotForm((prev) => ({ ...prev, isActive: e.target.checked }))
                    }
                    className="rounded border-zinc-700 text-amber-600 focus:ring-amber-500"
                  />
                  <span>Punto activo y visible para los clientes</span>
                </label>
              </div>

              {/* Actions Footer */}
              <div className="mt-auto pt-6 border-t border-zinc-800 flex items-center justify-between gap-3">
                {selectedHotspot ? (
                  <button
                    type="button"
                    disabled={isDeletingHotspot}
                    onClick={handleDeleteHotspot}
                    className="flex items-center gap-1.5 rounded-xl border border-rose-900/50 bg-rose-950/30 px-3 py-2 text-xs font-semibold text-rose-400 hover:bg-rose-900/50 transition-colors"
                  >
                    {isDeletingHotspot ? (
                      <Loader2 className="h-3.5 w-3.5 animate-spin" />
                    ) : (
                      <Trash2 className="h-3.5 w-3.5" />
                    )}
                    <span>Eliminar</span>
                  </button>
                ) : (
                  <div />
                )}

                <div className="flex items-center gap-2">
                  <button
                    type="button"
                    onClick={() => {
                      setIsHotspotDrawerOpen(false);
                      setNewPinCoords(null);
                    }}
                    className="rounded-xl border border-zinc-700 px-4 py-2 text-xs font-semibold text-zinc-400 hover:bg-zinc-800 hover:text-white"
                  >
                    Cancelar
                  </button>
                  <button
                    type="submit"
                    disabled={isSavingHotspot || !hotspotForm.productId}
                    className="flex items-center gap-1.5 rounded-xl bg-amber-700 px-5 py-2 text-xs font-bold text-white hover:bg-amber-600 disabled:opacity-50"
                  >
                    {isSavingHotspot ? (
                      <>
                        <Loader2 className="h-3.5 w-3.5 animate-spin" />
                        <span>Guardando...</span>
                      </>
                    ) : (
                      <>
                        <Save className="h-3.5 w-3.5" />
                        <span>Guardar Punto</span>
                      </>
                    )}
                  </button>
                </div>
              </div>
            </form>
          </div>
        </div>
      )}

      {/* Scene Settings Drawer */}
      {isSettingsOpen && (
        <div className="fixed inset-0 z-50 flex justify-end bg-black/50 backdrop-blur-xs">
          <div className="relative w-full max-w-md bg-zinc-900 border-l border-zinc-800 p-6 shadow-2xl flex flex-col overflow-y-auto animate-in slide-in-from-right duration-200">
            <div className="flex items-center justify-between border-b border-zinc-800 pb-4">
              <div className="flex items-center gap-2">
                <Sliders className="h-4 w-4 text-amber-400" />
                <h3 className="text-sm font-bold text-white">Configuración del Ambiente</h3>
              </div>
              <button
                type="button"
                onClick={() => setIsSettingsOpen(false)}
                className="rounded-full p-1.5 text-zinc-400 hover:bg-zinc-800 hover:text-white"
              >
                <X className="h-4 w-4" />
              </button>
            </div>

            {settingsSuccess && (
              <div className="mt-4 flex items-center gap-2 rounded-xl bg-emerald-950/40 border border-emerald-800 p-3 text-xs text-emerald-400">
                <CheckCircle2 className="h-4 w-4 shrink-0" />
                <span>Configuración actualizada con éxito.</span>
              </div>
            )}

            <form onSubmit={handleSaveSettings} className="mt-5 space-y-4 flex-1 flex flex-col">
              <div>
                <label className="block text-xs font-semibold text-zinc-300">Título</label>
                <input
                  type="text"
                  required
                  value={settingsForm.title}
                  onChange={(e) =>
                    setSettingsForm((prev) => ({ ...prev, title: e.target.value }))
                  }
                  className="mt-1 w-full rounded-xl border border-zinc-700 bg-zinc-800 px-3.5 py-2 text-xs text-white"
                />
              </div>

              <div>
                <label className="block text-xs font-semibold text-zinc-300">Slug</label>
                <input
                  type="text"
                  required
                  value={settingsForm.slug}
                  onChange={(e) =>
                    setSettingsForm((prev) => ({ ...prev, slug: e.target.value }))
                  }
                  className="mt-1 w-full rounded-xl border border-zinc-700 bg-zinc-800 px-3.5 py-2 text-xs text-white"
                />
              </div>

              <div>
                <label className="block text-xs font-semibold text-zinc-300">
                  Tipo de Habitación
                </label>
                <select
                  value={settingsForm.roomType}
                  onChange={(e) =>
                    setSettingsForm((prev) => ({ ...prev, roomType: e.target.value }))
                  }
                  className="mt-1 w-full rounded-xl border border-zinc-700 bg-zinc-800 px-3.5 py-2 text-xs text-white"
                >
                  <option value="Living">Living / Sala de Estar</option>
                  <option value="Comedor">Comedor</option>
                  <option value="Dormitorio">Dormitorio</option>
                  <option value="Terraza">Terraza / Exterior</option>
                  <option value="Estudio">Estudio / Oficina</option>
                </select>
              </div>

              <div>
                <label className="block text-xs font-semibold text-zinc-300">URL de Imagen</label>
                <input
                  type="url"
                  required
                  value={settingsForm.imageUrl}
                  onChange={(e) =>
                    setSettingsForm((prev) => ({ ...prev, imageUrl: e.target.value }))
                  }
                  className="mt-1 w-full rounded-xl border border-zinc-700 bg-zinc-800 px-3.5 py-2 text-xs text-white"
                />
              </div>

              <div>
                <label className="block text-xs font-semibold text-zinc-300">Descripción</label>
                <textarea
                  rows={4}
                  required
                  value={settingsForm.description}
                  onChange={(e) =>
                    setSettingsForm((prev) => ({ ...prev, description: e.target.value }))
                  }
                  className="mt-1 w-full rounded-xl border border-zinc-700 bg-zinc-800 px-3.5 py-2 text-xs text-white"
                />
              </div>

              <div className="pt-2">
                <label className="flex items-center gap-2 cursor-pointer text-xs font-semibold text-zinc-300">
                  <input
                    type="checkbox"
                    checked={settingsForm.isActive}
                    onChange={(e) =>
                      setSettingsForm((prev) => ({ ...prev, isActive: e.target.checked }))
                    }
                    className="rounded border-zinc-700 text-amber-600 focus:ring-amber-500"
                  />
                  <span>Ambiente publicado (Activo en catálogo)</span>
                </label>
              </div>

              <div className="mt-auto pt-6 border-t border-zinc-800 flex justify-end gap-2">
                <button
                  type="button"
                  onClick={() => setIsSettingsOpen(false)}
                  className="rounded-xl border border-zinc-700 px-4 py-2 text-xs font-semibold text-zinc-400 hover:bg-zinc-800 hover:text-white"
                >
                  Cerrar
                </button>
                <button
                  type="submit"
                  disabled={isSavingSettings}
                  className="inline-flex items-center gap-1.5 rounded-xl bg-amber-700 px-5 py-2 text-xs font-bold text-white hover:bg-amber-600 disabled:opacity-50"
                >
                  {isSavingSettings ? (
                    <>
                      <Loader2 className="h-3.5 w-3.5 animate-spin" />
                      <span>Guardando...</span>
                    </>
                  ) : (
                    <span>Guardar Cambios</span>
                  )}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}
    </div>
  );
}
