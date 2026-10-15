import Link from "next/link";
import { ShoppingBag, Sparkles, User, Search } from "lucide-react";

export function Navbar() {
  return (
    <header className="sticky top-0 z-40 w-full border-b border-stone-200 bg-white/90 backdrop-blur-md">
      <div className="mx-auto flex h-16 max-w-7xl items-center justify-between px-4 sm:px-6 lg:px-8">
        {/* Brand Logo */}
        <div className="flex items-center gap-8">
          <Link href="/" className="flex items-center gap-2 group">
            <div className="flex h-10 w-10 items-center justify-center rounded-xl bg-amber-700 text-white font-bold text-lg shadow-sm transition-transform group-hover:scale-105">
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
              href="/catalog"
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
            <Link
              href="/categories"
              className="transition-colors hover:text-amber-800"
            >
              Categorías
            </Link>
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

        {/* Actions (Cart & Account) */}
        <div className="flex items-center gap-4">
          <Link
            href="/account"
            className="flex items-center gap-2 rounded-full p-2 text-stone-700 hover:bg-stone-100 hover:text-stone-900 transition-colors"
            title="Mi Cuenta"
          >
            <User className="h-5 w-5" />
            <span className="hidden sm:inline text-sm font-medium">Cuenta</span>
          </Link>

          <Link
            href="/cart"
            className="relative flex items-center gap-2 rounded-full bg-stone-900 px-4 py-2 text-white hover:bg-stone-800 transition-colors shadow-sm"
          >
            <ShoppingBag className="h-4 w-4" />
            <span className="text-sm font-medium">Carrito</span>
            <span className="flex h-5 w-5 items-center justify-center rounded-full bg-amber-600 text-[11px] font-bold text-white">
              0
            </span>
          </Link>
        </div>
      </div>
    </header>
  );
}
