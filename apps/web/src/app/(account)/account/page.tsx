"use client";

import { useEffect, useState, useCallback } from "react";
import { useRouter } from "next/navigation";
import { useAuthStore } from "@/stores/auth-store";
import { apiClient } from "@/lib/api-client";
import {
  User,
  MapPin,
  LogOut,
  Plus,
  Trash2,
  CheckCircle,
  Phone,
  Mail,
  Shield,
  Loader2,
  Home,
} from "lucide-react";

interface Address {
  id: string;
  label: string;
  recipientName: string;
  phoneNumber: string;
  line1: string;
  line2?: string;
  district: string;
  city: string;
  region: string;
  reference?: string;
  isDefault: boolean;
}

export default function AccountPage() {
  const router = useRouter();
  const { user, isAuthenticated, logout, fetchProfile } = useAuthStore();
  const [activeTab, setActiveTab] = useState<"profile" | "addresses">("profile");

  // Profile Edit State initialized from user
  const [firstName, setFirstName] = useState(user?.firstName || "");
  const [lastName, setLastName] = useState(user?.lastName || "");
  const [phoneNumber, setPhoneNumber] = useState("");
  const [profileMessage, setProfileMessage] = useState<string | null>(null);
  const [isUpdatingProfile, setIsUpdatingProfile] = useState(false);

  // Addresses State
  const [addresses, setAddresses] = useState<Address[]>([]);
  const [isLoadingAddresses, setIsLoadingAddresses] = useState(false);
  const [showAddModal, setShowAddModal] = useState(false);
  const [addressError, setAddressError] = useState<string | null>(null);

  // New Address Form State
  const [newLabel, setNewLabel] = useState("Casa");
  const [newRecipient, setNewRecipient] = useState("");
  const [newPhone, setNewPhone] = useState("");
  const [newLine1, setNewLine1] = useState("");
  const [newDistrict, setNewDistrict] = useState("");
  const [newCity, setNewCity] = useState("Lima");
  const [newRegion, setNewRegion] = useState("Lima");
  const [newIsDefault, setNewIsDefault] = useState(false);
  const [isCreatingAddress, setIsCreatingAddress] = useState(false);

  const loadAddresses = useCallback(async () => {
    setIsLoadingAddresses(true);
    try {
      const data = await apiClient.get<Address[]>("/api/addresses");
      setAddresses(data);
    } catch {
      // Ignored
    } finally {
      setIsLoadingAddresses(false);
    }
  }, []);

  // Redirect if not authenticated and load initial data
  useEffect(() => {
    if (!isAuthenticated) {
      router.push("/login?redirect=/account");
    } else {
      fetchProfile();
      loadAddresses();
    }
  }, [isAuthenticated, router, fetchProfile, loadAddresses]);

  const handleUpdateProfile = async (e: React.FormEvent) => {
    e.preventDefault();
    setIsUpdatingProfile(true);
    setProfileMessage(null);
    try {
      await apiClient.put("/api/me", {
        firstName,
        lastName,
        phoneNumber: phoneNumber || null,
      });
      await fetchProfile();
      setProfileMessage("Perfil actualizado exitosamente.");
    } catch (err: unknown) {
      setProfileMessage(err instanceof Error ? err.message : "Error al actualizar perfil");
    } finally {
      setIsUpdatingProfile(false);
    }
  };

  const handleCreateAddress = async (e: React.FormEvent) => {
    e.preventDefault();
    setIsCreatingAddress(true);
    setAddressError(null);
    try {
      await apiClient.post("/api/addresses", {
        label: newLabel,
        recipientName: newRecipient,
        phoneNumber: newPhone,
        line1: newLine1,
        district: newDistrict,
        city: newCity,
        region: newRegion,
        isDefault: newIsDefault,
      });
      setShowAddModal(false);
      setNewLine1("");
      setNewRecipient("");
      setNewPhone("");
      setNewDistrict("");
      await loadAddresses();
    } catch (err: unknown) {
      setAddressError(err instanceof Error ? err.message : "Error al crear dirección");
    } finally {
      setIsCreatingAddress(false);
    }
  };

  const handleDeleteAddress = async (id: string) => {
    if (!confirm("¿Seguro que deseas eliminar esta dirección?")) return;
    try {
      await apiClient.delete(`/api/addresses/${id}`);
      await loadAddresses();
    } catch (err: unknown) {
      alert(err instanceof Error ? err.message : "Error al eliminar");
    }
  };

  const handleSetDefault = async (id: string) => {
    try {
      await apiClient.patch(`/api/addresses/${id}/default`);
      await loadAddresses();
    } catch (err: unknown) {
      alert(err instanceof Error ? err.message : "Error al configurar dirección");
    }
  };

  const handleLogout = async () => {
    await logout();
    router.push("/");
  };

  if (!isAuthenticated || !user) {
    return (
      <div className="flex h-96 items-center justify-center">
        <Loader2 className="h-8 w-8 animate-spin text-amber-800" />
      </div>
    );
  }

  return (
    <div className="mx-auto max-w-7xl px-4 py-12 sm:px-6 lg:px-8">
      {/* Account Header */}
      <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between border-b border-stone-200 pb-8 gap-4">
        <div>
          <h1 className="text-3xl font-bold tracking-tight text-stone-900 font-serif">
            Mi Cuenta
          </h1>
          <p className="mt-1 text-sm text-stone-500">
            Bienvenido, <span className="font-semibold text-stone-800">{user.fullName}</span> ({user.email})
          </p>
        </div>

        <div className="flex items-center gap-3">
          {user.roles.includes("ADMIN") && (
            <span className="inline-flex items-center gap-1.5 rounded-full bg-purple-100 px-3 py-1 text-xs font-semibold text-purple-800">
              <Shield className="h-3.5 w-3.5" />
              Administrador
            </span>
          )}
          <button
            onClick={handleLogout}
            className="flex items-center gap-2 rounded-xl border border-stone-300 bg-white px-4 py-2 text-sm font-semibold text-stone-700 hover:bg-stone-50 transition-colors"
          >
            <LogOut className="h-4 w-4" />
            Cerrar Sesión
          </button>
        </div>
      </div>

      {/* Tabs */}
      <div className="mt-8 flex gap-4 border-b border-stone-200">
        <button
          onClick={() => setActiveTab("profile")}
          className={`flex items-center gap-2 border-b-2 py-3 px-4 text-sm font-semibold transition-colors ${
            activeTab === "profile"
              ? "border-amber-800 text-amber-900"
              : "border-transparent text-stone-500 hover:text-stone-700"
          }`}
        >
          <User className="h-4 w-4" />
          Mi Perfil
        </button>

        <button
          onClick={() => setActiveTab("addresses")}
          className={`flex items-center gap-2 border-b-2 py-3 px-4 text-sm font-semibold transition-colors ${
            activeTab === "addresses"
              ? "border-amber-800 text-amber-900"
              : "border-transparent text-stone-500 hover:text-stone-700"
          }`}
        >
          <MapPin className="h-4 w-4" />
          Mis Direcciones ({addresses.length})
        </button>
      </div>

      {/* Profile Tab Content */}
      {activeTab === "profile" && (
        <div className="mt-8 max-w-2xl">
          <div className="rounded-2xl border border-stone-200 bg-white p-6 shadow-sm">
            <h2 className="text-lg font-bold text-stone-900">Datos Personales</h2>
            <p className="text-sm text-stone-500 mt-0.5">
              Actualiza tus nombres y datos de contacto para envíos.
            </p>

            {profileMessage && (
              <div className="mt-4 rounded-xl border border-stone-200 bg-stone-50 p-3 text-sm text-stone-800">
                {profileMessage}
              </div>
            )}

            <form onSubmit={handleUpdateProfile} className="mt-6 space-y-4">
              <div className="grid grid-cols-2 gap-4">
                <div>
                  <label className="block text-xs font-semibold uppercase tracking-wider text-stone-700">
                    Nombre
                  </label>
                  <input
                    type="text"
                    required
                    value={firstName}
                    onChange={(e) => setFirstName(e.target.value)}
                    className="mt-1 w-full rounded-xl border border-stone-300 py-2 px-3 text-sm text-stone-900 focus:border-amber-700 focus:outline-none"
                  />
                </div>
                <div>
                  <label className="block text-xs font-semibold uppercase tracking-wider text-stone-700">
                    Apellido
                  </label>
                  <input
                    type="text"
                    required
                    value={lastName}
                    onChange={(e) => setLastName(e.target.value)}
                    className="mt-1 w-full rounded-xl border border-stone-300 py-2 px-3 text-sm text-stone-900 focus:border-amber-700 focus:outline-none"
                  />
                </div>
              </div>

              <div>
                <label className="block text-xs font-semibold uppercase tracking-wider text-stone-700">
                  Correo Electrónico (No modificable)
                </label>
                <div className="relative mt-1">
                  <Mail className="absolute left-3.5 top-1/2 -translate-y-1/2 h-4 w-4 text-stone-400" />
                  <input
                    type="email"
                    disabled
                    value={user.email}
                    className="w-full rounded-xl border border-stone-200 bg-stone-100 py-2 pl-10 pr-4 text-sm text-stone-500 cursor-not-allowed"
                  />
                </div>
              </div>

              <div>
                <label className="block text-xs font-semibold uppercase tracking-wider text-stone-700">
                  Teléfono de Contacto
                </label>
                <div className="relative mt-1">
                  <Phone className="absolute left-3.5 top-1/2 -translate-y-1/2 h-4 w-4 text-stone-400" />
                  <input
                    type="tel"
                    value={phoneNumber}
                    onChange={(e) => setPhoneNumber(e.target.value)}
                    placeholder="+51 987 654 321"
                    className="w-full rounded-xl border border-stone-300 py-2 pl-10 pr-4 text-sm text-stone-900 focus:border-amber-700 focus:outline-none"
                  />
                </div>
              </div>

              <button
                type="submit"
                disabled={isUpdatingProfile}
                className="mt-4 rounded-xl bg-amber-800 px-5 py-2.5 text-sm font-semibold text-white hover:bg-amber-900 transition-colors disabled:opacity-50"
              >
                {isUpdatingProfile ? "Guardando..." : "Guardar Cambios"}
              </button>
            </form>
          </div>
        </div>
      )}

      {/* Addresses Tab Content */}
      {activeTab === "addresses" && (
        <div className="mt-8 space-y-6">
          <div className="flex items-center justify-between">
            <p className="text-sm text-stone-500">
              Direcciones guardadas para autocompletar tus compras durante el checkout.
            </p>
            <button
              onClick={() => setShowAddModal(true)}
              className="flex items-center gap-1.5 rounded-xl bg-stone-900 px-4 py-2 text-sm font-semibold text-white hover:bg-stone-800 transition-colors shadow-sm"
            >
              <Plus className="h-4 w-4" />
              Nueva Dirección
            </button>
          </div>

          {isLoadingAddresses ? (
            <div className="py-12 text-center text-sm text-stone-500">
              <Loader2 className="mx-auto h-6 w-6 animate-spin text-amber-800 mb-2" />
              Cargando tus direcciones...
            </div>
          ) : addresses.length === 0 ? (
            <div className="rounded-2xl border border-dashed border-stone-300 p-12 text-center">
              <Home className="mx-auto h-10 w-10 text-stone-400 mb-3" />
              <h3 className="text-base font-semibold text-stone-800">No tienes direcciones registradas</h3>
              <p className="mt-1 text-sm text-stone-500">
                Agrega tu primera dirección para agilizar tus envíos.
              </p>
            </div>
          ) : (
            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
              {addresses.map((addr) => (
                <div
                  key={addr.id}
                  className={`relative rounded-2xl border p-5 transition-shadow ${
                    addr.isDefault
                      ? "border-amber-800/60 bg-amber-50/20 shadow-sm"
                      : "border-stone-200 bg-white"
                  }`}
                >
                  <div className="flex items-start justify-between">
                    <div>
                      <span className="inline-flex items-center gap-1 text-xs font-bold uppercase tracking-wider text-amber-900">
                        {addr.label}
                      </span>
                      <h4 className="mt-1 font-semibold text-stone-900">{addr.recipientName}</h4>
                    </div>

                    {addr.isDefault ? (
                      <span className="inline-flex items-center gap-1 rounded-full bg-amber-100 px-2.5 py-0.5 text-xs font-semibold text-amber-800">
                        <CheckCircle className="h-3 w-3" />
                        Predeterminada
                      </span>
                    ) : (
                      <button
                        onClick={() => handleSetDefault(addr.id)}
                        className="text-xs font-semibold text-stone-500 hover:text-amber-800 transition-colors underline"
                      >
                        Hacer Predeterminada
                      </button>
                    )}
                  </div>

                  <p className="mt-3 text-sm text-stone-600 leading-relaxed">
                    {addr.line1}
                    {addr.line2 ? `, ${addr.line2}` : ""}
                    <br />
                    {addr.district}, {addr.city} ({addr.region})
                  </p>

                  <p className="mt-2 text-xs text-stone-500 flex items-center gap-1">
                    <Phone className="h-3.5 w-3.5" />
                    {addr.phoneNumber}
                  </p>

                  <div className="mt-4 flex justify-end border-t border-stone-100 pt-3">
                    <button
                      onClick={() => handleDeleteAddress(addr.id)}
                      className="flex items-center gap-1 text-xs font-medium text-red-600 hover:text-red-700 transition-colors"
                      title="Eliminar dirección"
                    >
                      <Trash2 className="h-3.5 w-3.5" />
                      Eliminar
                    </button>
                  </div>
                </div>
              ))}
            </div>
          )}

          {/* Add Address Modal */}
          {showAddModal && (
            <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/40 backdrop-blur-sm p-4">
              <div className="w-full max-w-lg rounded-2xl bg-white p-6 shadow-xl space-y-4">
                <div className="flex items-center justify-between border-b border-stone-200 pb-3">
                  <h3 className="text-lg font-bold text-stone-900">Nueva Dirección</h3>
                  <button
                    onClick={() => setShowAddModal(false)}
                    className="text-stone-400 hover:text-stone-600 font-bold"
                  >
                    ✕
                  </button>
                </div>

                {addressError && (
                  <div className="rounded-xl border border-red-200 bg-red-50 p-3 text-xs text-red-800">
                    {addressError}
                  </div>
                )}

                <form onSubmit={handleCreateAddress} className="space-y-3">
                  <div className="grid grid-cols-2 gap-3">
                    <div>
                      <label className="block text-xs font-semibold text-stone-700">Etiqueta</label>
                      <input
                        type="text"
                        required
                        value={newLabel}
                        onChange={(e) => setNewLabel(e.target.value)}
                        placeholder="Casa, Oficina, etc."
                        className="mt-1 w-full rounded-xl border border-stone-300 py-1.5 px-3 text-sm focus:border-amber-700 focus:outline-none"
                      />
                    </div>
                    <div>
                      <label className="block text-xs font-semibold text-stone-700">Teléfono</label>
                      <input
                        type="tel"
                        required
                        value={newPhone}
                        onChange={(e) => setNewPhone(e.target.value)}
                        placeholder="987654321"
                        className="mt-1 w-full rounded-xl border border-stone-300 py-1.5 px-3 text-sm focus:border-amber-700 focus:outline-none"
                      />
                    </div>
                  </div>

                  <div>
                    <label className="block text-xs font-semibold text-stone-700">Nombre del Destinatario</label>
                    <input
                      type="text"
                      required
                      value={newRecipient}
                      onChange={(e) => setNewRecipient(e.target.value)}
                      placeholder="Nombre y Apellido"
                      className="mt-1 w-full rounded-xl border border-stone-300 py-1.5 px-3 text-sm focus:border-amber-700 focus:outline-none"
                    />
                  </div>

                  <div>
                    <label className="block text-xs font-semibold text-stone-700">Dirección (Calle, Av, Número)</label>
                    <input
                      type="text"
                      required
                      value={newLine1}
                      onChange={(e) => setNewLine1(e.target.value)}
                      placeholder="Av. Las Flores 789"
                      className="mt-1 w-full rounded-xl border border-stone-300 py-1.5 px-3 text-sm focus:border-amber-700 focus:outline-none"
                    />
                  </div>

                  <div className="grid grid-cols-3 gap-2">
                    <div>
                      <label className="block text-xs font-semibold text-stone-700">Distrito</label>
                      <input
                        type="text"
                        required
                        value={newDistrict}
                        onChange={(e) => setNewDistrict(e.target.value)}
                        placeholder="San Borja"
                        className="mt-1 w-full rounded-xl border border-stone-300 py-1.5 px-3 text-sm focus:border-amber-700 focus:outline-none"
                      />
                    </div>
                    <div>
                      <label className="block text-xs font-semibold text-stone-700">Ciudad</label>
                      <input
                        type="text"
                        required
                        value={newCity}
                        onChange={(e) => setNewCity(e.target.value)}
                        className="mt-1 w-full rounded-xl border border-stone-300 py-1.5 px-3 text-sm focus:border-amber-700 focus:outline-none"
                      />
                    </div>
                    <div>
                      <label className="block text-xs font-semibold text-stone-700">Región</label>
                      <input
                        type="text"
                        required
                        value={newRegion}
                        onChange={(e) => setNewRegion(e.target.value)}
                        className="mt-1 w-full rounded-xl border border-stone-300 py-1.5 px-3 text-sm focus:border-amber-700 focus:outline-none"
                      />
                    </div>
                  </div>

                  <div className="flex items-center gap-2 pt-2">
                    <input
                      type="checkbox"
                      id="isDefault"
                      checked={newIsDefault}
                      onChange={(e) => setNewIsDefault(e.target.checked)}
                      className="rounded border-stone-300 text-amber-800 focus:ring-amber-700"
                    />
                    <label htmlFor="isDefault" className="text-xs text-stone-700 font-medium">
                      Establecer como dirección de entrega predeterminada
                    </label>
                  </div>

                  <div className="mt-6 flex justify-end gap-3 pt-3 border-t border-stone-200">
                    <button
                      type="button"
                      onClick={() => setShowAddModal(false)}
                      className="rounded-xl border border-stone-300 px-4 py-2 text-xs font-semibold text-stone-700 hover:bg-stone-50"
                    >
                      Cancelar
                    </button>
                    <button
                      type="submit"
                      disabled={isCreatingAddress}
                      className="rounded-xl bg-amber-800 px-4 py-2 text-xs font-semibold text-white hover:bg-amber-900 disabled:opacity-50"
                    >
                      {isCreatingAddress ? "Guardando..." : "Guardar Dirección"}
                    </button>
                  </div>
                </form>
              </div>
            </div>
          )}
        </div>
      )}
    </div>
  );
}
