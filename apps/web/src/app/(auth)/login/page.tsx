"use client";

import { Suspense, useState } from "react";
import Link from "next/link";
import { useRouter, useSearchParams } from "next/navigation";
import { useAuthStore } from "@/stores/auth-store";
import { AlertCircle, Lock, Mail, Loader2 } from "lucide-react";

function LoginForm() {
  const router = useRouter();
  const searchParams = useSearchParams();
  const redirect = searchParams.get("redirect") || "/account";

  const { login, isLoading, error, clearError } = useAuthStore();

  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    clearError();
    try {
      await login(email, password);
      router.push(redirect);
    } catch {
      // Error handled by store
    }
  };

  return (
    <div className="w-full max-w-md space-y-8 rounded-2xl border border-stone-200 bg-white p-8 shadow-sm">
      <div className="text-center">
        <div className="mx-auto flex h-12 w-12 items-center justify-center rounded-xl bg-amber-100 text-amber-800">
          <Lock className="h-6 w-6" />
        </div>
        <h2 className="mt-4 text-2xl font-bold tracking-tight text-stone-900 font-serif">
          Iniciar Sesión
        </h2>
        <p className="mt-2 text-sm text-stone-500">
          Ingresa a tu cuenta de HomeSphere para gestionar tus pedidos y direcciones
        </p>
      </div>

      {error && (
        <div
          role="alert"
          className="flex items-start gap-3 rounded-xl border border-red-200 bg-red-50 p-4 text-sm text-red-800"
        >
          <AlertCircle className="h-5 w-5 shrink-0 text-red-600" />
          <div>
            <p className="font-semibold">Error de autenticación</p>
            <p className="mt-0.5">{error}</p>
          </div>
        </div>
      )}

      <form className="mt-8 space-y-5" onSubmit={handleSubmit}>
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
              placeholder="ejemplo@correo.com"
              className="w-full rounded-xl border border-stone-300 py-2.5 pl-10 pr-4 text-sm text-stone-900 placeholder:text-stone-400 focus:border-amber-700 focus:outline-none focus:ring-1 focus:ring-amber-700"
            />
          </div>
        </div>

        <div>
          <label
            htmlFor="password"
            className="block text-xs font-semibold uppercase tracking-wider text-stone-700"
          >
            Contraseña
          </label>
          <div className="relative mt-1">
            <Lock className="absolute left-3.5 top-1/2 -translate-y-1/2 h-4 w-4 text-stone-400" />
            <input
              id="password"
              type="password"
              autoComplete="current-password"
              required
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              placeholder="••••••••"
              className="w-full rounded-xl border border-stone-300 py-2.5 pl-10 pr-4 text-sm text-stone-900 placeholder:text-stone-400 focus:border-amber-700 focus:outline-none focus:ring-1 focus:ring-amber-700"
            />
          </div>
        </div>

        <button
          type="submit"
          disabled={isLoading}
          className="flex w-full items-center justify-center gap-2 rounded-xl bg-amber-800 py-3 text-sm font-semibold text-white shadow-sm hover:bg-amber-900 focus:outline-none focus:ring-2 focus:ring-amber-700 focus:ring-offset-2 transition-colors disabled:opacity-50"
        >
          {isLoading ? (
            <>
              <Loader2 className="h-4 w-4 animate-spin" />
              Iniciando sesión...
            </>
          ) : (
            "Ingresar"
          )}
        </button>
      </form>

      <div className="border-t border-stone-200 pt-6 text-center text-sm text-stone-600">
        ¿No tienes una cuenta aún?{" "}
        <Link
          href="/register"
          className="font-semibold text-amber-800 hover:text-amber-900 hover:underline"
        >
          Regístrate aquí
        </Link>
      </div>
    </div>
  );
}

export default function LoginPage() {
  return (
    <div className="flex min-h-[calc(100vh-16rem)] items-center justify-center px-4 py-12 sm:px-6 lg:px-8">
      <Suspense fallback={<div className="text-center p-8 text-stone-500">Cargando formulario...</div>}>
        <LoginForm />
      </Suspense>
    </div>
  );
}
