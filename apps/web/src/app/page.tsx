import Link from "next/link";
import { Sparkles, ArrowRight, ShieldCheck, Truck } from "lucide-react";

export default function HomePage() {
  return (
    <div className="flex flex-col">
      {/* Hero Section */}
      <section className="relative overflow-hidden bg-gradient-to-b from-amber-50/60 to-white py-20 sm:py-28">
        <div className="mx-auto max-w-7xl px-4 sm:px-6 lg:px-8">
          <div className="mx-auto max-w-3xl text-center">
            <div className="inline-flex items-center gap-2 rounded-full bg-amber-100/80 px-4 py-1.5 text-xs font-semibold text-amber-900 mb-6">
              <Sparkles className="h-3.5 w-3.5 text-amber-700" />
              Experiencia Inmersiva v1.0
            </div>

            <h1 className="text-4xl font-extrabold tracking-tight text-stone-900 sm:text-6xl font-serif">
              Diseño para tu hogar con{" "}
              <span className="text-amber-800 underline decoration-amber-300 underline-offset-8">
                ambientes interactivos
              </span>
            </h1>

            <p className="mt-6 text-lg leading-8 text-stone-600">
              Explora salas, dormitorios y comedores completamente ambientados. Haz clic en cualquier mueble para ver sus variantes, precios en tiempo real y comprar con confianza.
            </p>

            <div className="mt-10 flex flex-wrap items-center justify-center gap-4">
              <Link
                href="/scenes"
                className="flex items-center gap-2 rounded-xl bg-amber-800 px-6 py-3.5 text-base font-semibold text-white shadow-sm hover:bg-amber-900 transition-colors"
              >
                <Sparkles className="h-5 w-5" />
                Explorar Ambientes Interactivos
              </Link>
              <Link
                href="/catalog"
                className="flex items-center gap-2 rounded-xl border border-stone-300 bg-white px-6 py-3.5 text-base font-semibold text-stone-800 shadow-sm hover:bg-stone-50 transition-colors"
              >
                Ver Catálogo
                <ArrowRight className="h-4 w-4" />
              </Link>
            </div>
          </div>
        </div>
      </section>

      {/* Feature Highlights */}
      <section className="border-y border-stone-200 bg-white py-12">
        <div className="mx-auto max-w-7xl px-4 sm:px-6 lg:px-8">
          <div className="grid grid-cols-1 md:grid-cols-3 gap-8">
            <div className="flex items-start gap-4">
              <div className="flex h-12 w-12 shrink-0 items-center justify-center rounded-xl bg-amber-50 text-amber-800">
                <Sparkles className="h-6 w-6" />
              </div>
              <div>
                <h3 className="font-semibold text-stone-900">Hotspots Responsive</h3>
                <p className="mt-1 text-sm text-stone-500">
                  Exploración interactiva con posición porcentual exacta, accesible por teclado y compatible con dispositivos móviles.
                </p>
              </div>
            </div>

            <div className="flex items-start gap-4">
              <div className="flex h-12 w-12 shrink-0 items-center justify-center rounded-xl bg-amber-50 text-amber-800">
                <ShieldCheck className="h-6 w-6" />
              </div>
              <div>
                <h3 className="font-semibold text-stone-900">Consistencia Transaccional</h3>
                <p className="mt-1 text-sm text-stone-500">
                  Precios y stock calculados 100% en backend con reservas temporales e idempotencia garantizada.
                </p>
              </div>
            </div>

            <div className="flex items-start gap-4">
              <div className="flex h-12 w-12 shrink-0 items-center justify-center rounded-xl bg-amber-50 text-amber-800">
                <Truck className="h-6 w-6" />
              </div>
              <div>
                <h3 className="font-semibold text-stone-900">Fulfillment Confiable</h3>
                <p className="mt-1 text-sm text-stone-500">
                  Seguimiento de pedidos con máquina de estados finita y snapshots inmutables del detalle de compra.
                </p>
              </div>
            </div>
          </div>
        </div>
      </section>
    </div>
  );
}
