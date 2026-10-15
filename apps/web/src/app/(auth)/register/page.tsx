"use client";

import { useState } from "react";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { useAuthStore } from "@/stores/auth-store";
import { AlertCircle, Lock, Mail, User, Loader2 } from "lucide-react";

export default function RegisterPage() {
  const router = useRouter();
  const { register, isLoading, error, clearError } = useAuthStore();

  const [firstName, setFirstName] = useState("");
  const [lastName, setLastName] = useState("");
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [confirmPassword, setConfirmPassword] = useState("");
  const [validationError, setValidationError] = useState<string | null>(null);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    clearError();
    setValidationError(null);

    if (password.length < 8) {
      setValidationError("La contraseña debe tener al menos 8 caracteres.");
      return;
    }

    if (password !== confirmPassword) {
      setValidationError("Las contraseñas no coinciden.");
      return;
    }

    try {
      await register(email, password, firstName, lastName);
      router.push("/account");
    } catch {
      // Error handled by store
    }
  };

  const displayError = validationError || error;

  return (
    <div className="flex min-h-[calc(100vh-16rem)] items-center justify-center px-4 py-12 sm:px-6 lg:px-8">
      <div className="w-full max-w-md space-y-8 rounded-2xl border border-stone-200 bg-white p-8 shadow-sm">
        <div className="text-center">
          <div className="mx-auto flex h-12 w-12 items-center justify-center rounded-xl bg-amber-100 text-amber-800">
            <User className="h-6 w-6" />
          </div>
          <h2 className="mt-4 text-2xl font-bold tracking-tight text-stone-900 font-serif">
            Crear Cuenta
          </h2>
          <p className="mt-2 text-sm text-stone-500">
            Únete a HomeSphere para disfrutar de una experiencia de compra personalizada
          </p>
        </div>

        {displayError && (
          <div
            role="alert"
            className="flex items-start gap-3 rounded-xl border border-red-200 bg-red-50 p-4 text-sm text-red-800"
          >
            <AlertCircle className="h-5 w-5 shrink-0 text-red-600" />
            <div>
              <p className="font-semibold">Atención</p>
              <p className="mt-0.5">{displayError}</p>
            </div>
          </div>
        )}

        <form className="mt-8 space-y-4" onSubmit={handleSubmit}>
          <div className="grid grid-cols-2 gap-3">
            <div>
              <label
                htmlFor="firstName"
                className="block text-xs font-semibold uppercase tracking-wider text-stone-700"
              >
                Nombre
              </label>
              <input
                id="firstName"
                type="text"
                required
                value={firstName}
                onChange={(e) => setFirstName(e.target.value)}
                placeholder="Carlos"
                className="mt-1 w-full rounded-xl border border-stone-300 py-2.5 px-3.5 text-sm text-stone-900 placeholder:text-stone-400 focus:border-amber-700 focus:outline-none focus:ring-1 focus:ring-amber-700"
              />
            </div>
            <div>
              <label
                htmlFor="lastName"
                className="block text-xs font-semibold uppercase tracking-wider text-stone-700"
              >
                Apellido
              </label>
              <input
                id="lastName"
                type="text"
                required
                value={lastName}
                onChange={(e) => setLastName(e.target.value)}
                placeholder="Perez"
                className="mt-1 w-full rounded-xl border border-stone-300 py-2.5 px-3.5 text-sm text-stone-900 placeholder:text-stone-400 focus:border-amber-700 focus:outline-none focus:ring-1 focus:ring-amber-700"
              />
            </div>
          </div>

          <div>
            <label
              htmlFor="email"
              className="block text-xs font-semibold uppercase tracking-wider text-stone-700"
            >
              Correo Electrónico
            </label>
            <div className="relative mt-1">
              <Mail className="absolute left-3.5 top-1/2 -translate-y-1/2 h-4 w-4 text-stone-400" />
              <input
                id="email"
                type="email"
                autoComplete="email"
                required
                value={email}
                onChange={(e) => setEmail(e.target.value)}
                placeholder="carlos@ejemplo.com"
                className="w-full rounded-xl border border-stone-300 py-2.5 pl-10 pr-4 text-sm text-stone-900 placeholder:text-stone-400 focus:border-amber-700 focus:outline-none focus:ring-1 focus:ring-amber-700"
              />
            </div>
          </div>

          <div>
            <label
              htmlFor="password"
              className="block text-xs font-semibold uppercase tracking-wider text-stone-700"
            >
              Contraseña (mínimo 8 caracteres)
            </label>
            <div className="relative mt-1">
              <Lock className="absolute left-3.5 top-1/2 -translate-y-1/2 h-4 w-4 text-stone-400" />
              <input
                id="password"
                type="password"
                autoComplete="new-password"
                required
                value={password}
                onChange={(e) => setPassword(e.target.value)}
                placeholder="••••••••"
                className="w-full rounded-xl border border-stone-300 py-2.5 pl-10 pr-4 text-sm text-stone-900 placeholder:text-stone-400 focus:border-amber-700 focus:outline-none focus:ring-1 focus:ring-amber-700"
              />
            </div>
          </div>

          <div>
            <label
              htmlFor="confirmPassword"
              className="block text-xs font-semibold uppercase tracking-wider text-stone-700"
            >
              Confirmar Contraseña
            </label>
            <div className="relative mt-1">
              <Lock className="absolute left-3.5 top-1/2 -translate-y-1/2 h-4 w-4 text-stone-400" />
              <input
                id="confirmPassword"
                type="password"
                autoComplete="new-password"
                required
                value={confirmPassword}
                onChange={(e) => setConfirmPassword(e.target.value)}
                placeholder="••••••••"
                className="w-full rounded-xl border border-stone-300 py-2.5 pl-10 pr-4 text-sm text-stone-900 placeholder:text-stone-400 focus:border-amber-700 focus:outline-none focus:ring-1 focus:ring-amber-700"
              />
            </div>
          </div>

          <button
            type="submit"
            disabled={isLoading}
            className="flex w-full items-center justify-center gap-2 rounded-xl bg-amber-800 py-3 text-sm font-semibold text-white shadow-sm hover:bg-amber-900 focus:outline-none focus:ring-2 focus:ring-amber-700 focus:ring-offset-2 transition-colors disabled:opacity-50 mt-2"
          >
            {isLoading ? (
              <>
                <Loader2 className="h-4 w-4 animate-spin" />
                Creando cuenta...
              </>
            ) : (
              "Completar Registro"
            )}
          </button>
        </form>

        <div className="border-t border-stone-200 pt-6 text-center text-sm text-stone-600">
          ¿Ya tienes una cuenta?{" "}
          <Link
            href="/login"
            className="font-semibold text-amber-800 hover:text-amber-900 hover:underline"
          >
            Inicia sesión
          </Link>
        </div>
      </div>
    </div>
  );
}
