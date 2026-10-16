# Changelog — HomeSphere 🏠🛋️

Todas las modificaciones notables de este proyecto están documentadas en este archivo.
El formato se basa en [Keep a Changelog](https://keepachangelog.com/es-ES/1.1.0/) y este proyecto se adhiere a [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

---

## [1.0.0] - 2026-10-16

### 🚀 Novedades y Características Principales (Release Oficial)

#### Ambientes Interactivos ("Shop the Room")
- **Visualizador Fotorrealista**: Lienzo reactivo con coordenadas porcentuales normalizadas `(x: 0-100%, y: 0-100%)` inmunes a la resolución o aspecto de pantalla.
- **Degradación Elegante**: Validación en tiempo de servidor que oculta o deshabilita la compra selectiva de productos agotados o archivados sin romper la estética de la escena.
- **"Comprar Ambiente Completo"**: Funcionalidad de un solo clic que suma al carrito todas las piezas disponibles del ambiente con recálculo dinámico de subtotal.
- **Editor Visual WYSIWYG**: Panel administrativo con captura milimétrica de coordenadas por clic sobre la fotografía, búsqueda de productos con *debounce*, selector de variantes y previsualización interactiva *Live Preview*.
- **Accesibilidad WCAG 2.2 AA**: Botones de hotspots táctiles con área de impacto $\ge 44 \times 44\text{ px}$ (`h-11 w-11`), navegación por teclado (`Tab`, `Enter`, `Escape`), `aria-expanded` y foco visible.

#### Núcleo Transaccional & Control de Concurrencia
- **Máquina de Estados de Pedidos**: Transiciones controladas (`PendingPayment -> Paid -> Processing -> Shipped -> Delivered` o `Cancelled`).
- **Reserva de Stock con TTL (15 min)**: Separación estricta de inventario `OnHand` y `Reserved`. Las reservas se originan en el checkout y caducan automáticamente mediante el worker hospedado `ExpiredReservationWorker`.
- **Idempotencia Estricta**: Encabezado `Idempotency-Key` en creación de órdenes para neutralizar duplicados por doble clic, latencia o reintentos de red.
- **Validación Criptográfica de Webhooks**: Verificación de firmas HMAC-SHA256 en tiempo constante (`CryptographicOperations.FixedTimeEquals`) para pasarela de pagos desacoplada (`IPaymentProvider`).

#### Lista de Deseos (Wishlist) & Favoritos
- **Persistencia en Base de Datos**: Entidad `WishlistItem` con índice compuesto único `(user_id, product_id)` y eliminación en cascada.
- **Store Reactivo en Zustand**: Actualizaciones optimistas instantáneas y sincronización transparente al iniciar sesión.
- **Vistas en Storefront**: Botón interactivo de corazón en tarjetas de catálogo (`ProductCard`), página de detalle (`PDP`), barra de navegación y panel `/account/wishlist`.

#### Almacén, Movimientos de Inventario & Auditoría
- **Trazabilidad de Movimientos**: Registro de eventos de inventario (`InventoryMovement`) para ingresos, ajustes, mermas, cancelaciones y ventas.
- **Regla Anti-Overselling**: Validación en servidor que impide que el stock disponible resulte negativo (`availableStock = onHand - reserved >= 0`).
- **Redacción Recursiva de Secretos**: Interceptor de auditoría que sanitiza automáticamente contraseñas, tokens JWT y números de tarjeta reemplazándolos por `[REDACTED]` con codificación relajada UTF-8.
- **Dashboard Comercial con Ingreso Reconocido**: Fórmulas de facturación real que aíslan órdenes abandonadas o canceladas de los ingresos netos y del ticket promedio (AOV).

#### DevOps, Docker & CI/CD
- **Docker Multi-stage**:
  - Backend: `mcr.microsoft.com/dotnet/sdk:8.0` -> `mcr.microsoft.com/dotnet/aspnet:8.0` ejecutando bajo usuario sin privilegios `USER app`.
  - Frontend: `node:20-alpine` (deps -> builder -> runner) en modo Next.js Standalone ejecutando bajo usuario no-root `USER nextjs`.
- **Orquestación en 1 Clic**: `docker-compose.yml` que enlaza PostgreSQL 17, Backend API en puerto 5100 y Frontend Web en puerto 3000.
- **GitHub Actions**: Pipeline de validación continua (`.github/workflows/ci.yml`) ejecutando compilación de Next.js 16 y suite completa de pruebas.

---

### 🧪 Suite de Pruebas Automatizadas
- **Pruebas Unitarias (.NET 8)**: 72 pruebas cubriendo lógica de precios, cupones, concurrencia de reservas, degradación de escenas, analíticas de EBITDA, sanitización de auditoría y movimientos de inventario.
- **Pruebas de Integración (ASP.NET Core)**: 7 pruebas end-to-end con `WebApplicationFactory<Program>` validando endpoints de salud, catálogo, catálogo por filtros, flujo de autenticación (registro, login, token Bearer) y aislamiento de perfil.
- **Total**: **79/79 pruebas automáticas superadas exitosamente (0 fallos)**.

---

## [0.5.0] - 2026-10-15
### Añadido
- Flujo completo de Carrito (invitado y autenticado) con merge determinista al login.
- Motor de cupones porcentuales y fijos con validación de fechas y topes máximos.
- Preview de checkout con cálculo de envío y validación anti-IDOR.

## [0.2.0] - 2026-10-15
### Añadido
- Catálogo de productos, categorías jerárquicas y marcas.
- Páginas de detalle de producto (PDP) con microdatos estructurados Schema.org JSON-LD.
- Filtros reactivos por facetas de categoría, marca y rangos de precio.

## [0.1.0] - 2026-10-15
### Añadido
- Inicialización del monorepo (Next.js 16 App Router + ASP.NET Core 8 Web API).
- Configuración de PostgreSQL 17 con Entity Framework Core 8.
- Manejo global de excepciones (`ProblemDetails` RFC 7807) y correlación de peticiones (`X-Correlation-Id`).
