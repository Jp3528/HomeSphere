"use client";

import Link from "next/link";
import { useAuthStore } from "@/stores/auth-store";
import { useCartStore } from "@/stores/cart-store";
import { ShoppingBag, Sparkles, Search, Shield, LogIn } from "lucide-react";

export function Navbar() {
  const { user, isAuthenticated, hasRole } = useAuthStore();
  const { cart, openDrawer } = useCartStore();
  const isAdmin = hasRole("ADMIN");
  const totalItems = cart?.totalItems ?? 0;

  return (
    <header className="sticky top-0 z-40 w-full border-b border-stone-200 bg-white/90 backdrop-blur-md">
      <div className="mx-auto flex h-16 max-w-7xl items-center justify-between px-4 sm:px-6 lg:px-8">
        {/* Brand Logo */}
        <div className="flex items-center gap-8">
          <Link href="/" className="flex items-center gap-2 group">
            <div className="flex h-10 w-10 items-center justify-center rounded-xl bg-amber-800 text-white font-bold text-lg shadow-sm transition-transform group-hover:scale-105">
              H
            </div>
            <div className="flex flex-col">
              <span className="text-xl font-bold tracking-tight text-stone-900">
                HomeSphere
              </span>
              <span className="text-[10px] uppercase tracking-widest text-amber-800 font-semibold">
                Hogar & Ambientes
              </span>
            </div>
          </Link>

          {/* Navigation Links */}
          <nav className="hidden md:flex items-center gap-6 text-sm font-medium text-stone-700">
            <Link
              href="/products"
              className="transition-colors hover:text-amber-800"
            >
              Catálogo
            </Link>
            <Link
              href="/scenes"
              className="flex items-center gap-1.5 text-amber-800 font-semibold transition-colors hover:text-amber-900"
            >
              <Sparkles className="h-4 w-4" />
              Ambientes Interactivos
            </Link>

            {isAdmin && (
              <Link
                href="/admin"
                className="flex items-center gap-1 rounded-full bg-purple-50 px-2.5 py-1 text-xs font-semibold text-purple-700 hover:bg-purple-100 transition-colors"
              >
                <Shield className="h-3 w-3" />
                Panel Admin
              </Link>
            )}
          </nav>
        </div>

        {/* Search Bar Placeholder */}
        <div className="hidden lg:flex flex-1 max-w-md mx-8">
          <div className="relative w-full">
            <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-stone-400" />
            <input
              type="search"
              placeholder="Buscar muebles, salas, lámparas..."
              className="w-full rounded-full border border-stone-300 bg-stone-50 py-2 pl-9 pr-4 text-sm text-stone-900 placeholder:text-stone-400 focus:border-amber-700 focus:bg-white focus:outline-none focus:ring-1 focus:ring-amber-700"
            />
          </div>
        </div>

        {/* Actions (Auth, Cart) */}
        <div className="flex items-center gap-3">
          {isAuthenticated && user ? (
            <Link
              href="/account"
              className="flex items-center gap-2 rounded-full border border-stone-200 bg-stone-50 py-1.5 px-3 text-stone-800 hover:bg-stone-100 transition-colors"
            >
              <div className="flex h-6 w-6 items-center justify-center rounded-full bg-amber-800 text-white text-xs font-bold">
                {user.firstName[0]}
              </div>
              <span className="text-xs font-semibold hidden sm:inline">
                {user.firstName}
              </span>
            </Link>
          ) : (
            <div className="flex items-center gap-2">
              <Link
                href="/login"
                className="flex items-center gap-1 rounded-xl px-3 py-1.5 text-xs font-semibold text-stone-700 hover:bg-stone-100 transition-colors"
              >
                <LogIn className="h-3.5 w-3.5" />
                Ingresar
              </Link>
              <Link
                href="/register"
                className="rounded-xl bg-amber-800 px-3 py-1.5 text-xs font-semibold text-white hover:bg-amber-900 transition-colors hidden sm:inline-block shadow-sm"
              >
                Registrarse
              </Link>
            </div>
          )}

          <button
            type="button"
            onClick={openDrawer}
            className="relative flex items-center gap-2 rounded-full bg-stone-900 px-3.5 py-1.5 text-white hover:bg-stone-800 transition-colors shadow-sm cursor-pointer"
            aria-label={`Ver carrito de compras con ${totalItems} artículos`}
          >
            <ShoppingBag className="h-4 w-4" />
            <span className="text-xs font-medium hidden sm:inline">Carrito</span>
            {totalItems > 0 && (
              <span className="flex h-4 min-w-4 px-1 items-center justify-center rounded-full bg-amber-600 text-[10px] font-bold text-white">
                {totalItems}
              </span>
            )}
          </button>
        </div>
      </div>
    </header>
  );
}
