# HomeSphere — Catalog & SEO Architecture Reference (Session 2R)

Este documento detalla la arquitectura de catálogo, la optimización de consultas en base de datos, el modelado de variantes con autoridad del servidor, y la estrategia de SEO estructurado implementada en HomeSphere durante la **Semana 2**.

---

## 1. Modelo de Datos y Relaciones de Catálogo

```
      ┌────────────────┐
      │    Category    │◄───┐ (Jerárquica / Parent-Child)
      └───────┬────────┘    │
              │ 1:N         │
              ▼             │
      ┌────────────────┐    │
      │    Product     ├────┘
      └───┬───┬──────┬─┘
      1:N │   │ 1:N  │ N:1
          │   │      ▼
          │   │ ┌──────────────┐
          │   │ │    Brand     │
          │   │ └──────────────┘
          │   ▼
          │ ┌──────────────────┐
          │ │  ProductVariant  │ (SKU único, precio, stock, JSON attributes)
          │ └──────────────────┘
          ▼
   ┌───────────────┐
   │ ProductImage  │ (DisplayOrder, IsPrimary)
   └───────────────┘
```

### 1.1 Entidades Principales
- **Category**: Soporte jerárquico recursivo (`ParentId` referenciando a `Category`). Índices en `slug` único y `parent_id`.
- **Brand**: Marcas de diseño (`Nordic Living`, `Casa Moderna`, `Atelier Madera`, etc.). Índice en `slug` único.
- **Product**: Entidad central con estados (`Draft = 0`, `Active = 1`, `Archived = 2`), precio base y flag destacado (`IsFeatured`).
- **ProductVariant**: Gestión de variantes físicas (color, dimensiones, material) identificadas por un `SKU` único indexado. Almacena precio real y `StockQuantity` en servidor.
- **ProductImage**: Galería multimedia con ordenación explícita (`DisplayOrder`) y bandera de imagen principal (`IsPrimary`).

---

## 2. Índices y Optimización de Consultas en PostgreSQL

Para garantizar búsquedas instantáneas y navegación por facetas a escala, se crearon los siguientes índices en la migración `20261015173619_AddCatalogEntities`:

| Tabla | Nombre del Índice | Columnas | Propósito |
| :--- | :--- | :--- | :--- |
| `categories` | `ix_categories_slug` | `slug` (UNIQUE) | Búsqueda O(1) de categoría por URL amigable |
| `categories` | `ix_categories_parent_id` | `parent_id` | Construcción rápida de árboles jerárquicos |
| `brands` | `ix_brands_slug` | `slug` (UNIQUE) | Resolución inmediata de marca por slug |
| `products` | `ix_products_slug` | `slug` (UNIQUE) | Resolución directa de la PDP en `/products/[slug]` |
| `products` | `ix_products_category_status_price` | `(category_id, status, base_price)` | Filtro compuesto de catálogo público y ordenación por precio |
| `products` | `ix_products_brand_id` | `brand_id` | Filtro por fabricante / marca |
| `product_variants`| `ix_product_variants_sku` | `sku` (UNIQUE) | Prevención de duplicados de SKU e inventario |
| `product_variants`| `ix_product_variants_product_id` | `product_id` | Join optimizado con productos |
| `product_images` | `ix_product_images_product_id` | `product_id` | Carga de galerías por producto |

---

## 3. Arquitectura Frontend: Next.js 16 App Router & SEO

### 3.1 Streaming con Partial Prerendering (PPR)
- Se aprovecha **Next.js 16 con Turbopack y Cache Components**.
- Las rutas `/products` y `/products/[slug]` pre-renderizan el cascarón estático (navbar, footer, layout, breadcrumbs) y transmiten el contenido dinámico mediante límites de `<Suspense>`.
- Se evita el bloqueo de SSR en tiempo de compilación con fallbacks elegantes.

### 3.2 Microdatos Estructurados (JSON-LD)
En la página de detalle de producto (`/products/[slug]`) se inyecta dinámicamente un esquema Schema.org `Product` con `AggregateOffer`:
- Nombre del producto, marca y galería de imágenes.
- Rango de precios dinámico (`lowPrice` y `highPrice` calculados a partir de las variantes activas).
- Estado de disponibilidad (`InStock` vs `OutOfStock`) para indexación en Google Shopping y Rich Snippets.

### 3.3 Metadatos OpenGraph y Accesibilidad (WCAG 2.1 AA)
- `generateMetadata` dinámico para tarjetas de previsualización en redes sociales (Twitter Cards / OpenGraph).
- Galería con atributos `alt` contextuales y navegación por teclado en selectores de variantes.
- Contraste de texto certificado sobre fondos blancos y oscuros (Tailwind Dark Mode).

---

## 4. Almacenamiento Seguro de Medios (`LocalStorageService`)

- **Aislamiento**: Los archivos subidos se almacenan en `wwwroot/uploads` servidos mediante `app.UseStaticFiles()`.
- **Validación de Extensiones**: Lista blanca estricta (`.jpg`, `.jpeg`, `.png`, `.webp`, `.svg`, `.avif`). Los ejecutables o scripts son rechazados con `400 Bad Request`.
- **Generación Segura de Nombres**: Los nombres de archivo se sanitizan utilizando UUIDs (`{Guid:N}.extension`), eliminando riesgos de Path Traversal o sobreescritura accidental.
- **Límite de Tamaño**: Configuración máxima de 10 MB por recurso.
