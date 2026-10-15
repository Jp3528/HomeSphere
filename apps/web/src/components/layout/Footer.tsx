export function Footer() {
  return (
    <footer className="mt-auto border-t border-stone-200 bg-stone-50 py-12 text-stone-600">
      <div className="mx-auto max-w-7xl px-4 sm:px-6 lg:px-8">
        <div className="grid grid-cols-1 md:grid-cols-4 gap-8">
          <div className="space-y-4">
            <span className="text-lg font-bold text-stone-900">HomeSphere</span>
            <p className="text-sm text-stone-500 leading-relaxed">
              Muebles de diseño y experiencias visuales interactivas para transformar tu hogar con confianza y estilo.
            </p>
          </div>

          <div>
            <h4 className="text-xs font-semibold uppercase tracking-wider text-stone-900 mb-3">
              Explorar
            </h4>
            <ul className="space-y-2 text-sm">
              <li><a href="/catalog" className="hover:text-amber-800 transition-colors">Catálogo de Productos</a></li>
              <li><a href="/scenes" className="hover:text-amber-800 transition-colors">Ambientes Interactivos</a></li>
              <li><a href="/categories" className="hover:text-amber-800 transition-colors">Salas y Dormitorios</a></li>
            </ul>
          </div>

          <div>
            <h4 className="text-xs font-semibold uppercase tracking-wider text-stone-900 mb-3">
              Garantía y Confianza
            </h4>
            <ul className="space-y-2 text-sm text-stone-500">
              <li>Precios transparentes garantizados</li>
              <li>Envíos a todo el país</li>
              <li>Pago seguro y protegido</li>
            </ul>
          </div>

          <div>
            <h4 className="text-xs font-semibold uppercase tracking-wider text-stone-900 mb-3">
              Ingeniería
            </h4>
            <p className="text-xs text-stone-500 leading-relaxed">
              Arquitectura Clean Monolith, ASP.NET Core 8 Web API, PostgreSQL 17 y Next.js 16 con App Router.
            </p>
          </div>
        </div>

        <div className="mt-8 border-t border-stone-200 pt-6 text-center text-xs text-stone-400">
          &copy; 2026 HomeSphere. Todos los derechos reservados. Proyecto de portafolio para Ingeniería de Sistemas.
        </div>
      </div>
    </footer>
  );
}
