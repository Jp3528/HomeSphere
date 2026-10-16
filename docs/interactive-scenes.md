# HomeSphere — Ambientes Interactivos & Editor Visual (Semana 6)

## 1. Visión General
El módulo de **Ambientes Interactivos** es el diferenciador clave de la propuesta de valor de HomeSphere. Transforma fotografías editoriales de alta resolución de salones, comedores, dormitorios y terrazas en catálogos inmersivos interactivos donde los usuarios pueden explorar cada pieza, inspeccionar sus acabados y añadir el ambiente completo a su carrito con un solo clic.

---

## 2. Arquitectura de Dominio y Datos

### 2.1 Modelo `RoomScene` y `SceneHotspot`
- **`RoomScene`**:
  - `Title`, `Slug`, `Description`, `ImageUrl`
  - `RoomType`: Categoría espacial (`"Living"`, `"Comedor"`, `"Dormitorio"`, `"Terraza"`, `"Estudio"`).
  - `DisplayOrder`, `IsActive`, `CreatedAtUtc`, `UpdatedAtUtc`
  - Colección de `SceneHotspot`.
- **`SceneHotspot`**:
  - `RoomSceneId`, `ProductId`, `ProductVariantId?`
  - Coordenadas porcentuales normalizadas: `XCoordinate` (0.00 a 100.00%), `YCoordinate` (0.00 a 100.00%).
  - `Title`: Sobrescritura de título opcional para la composición (ej. "Sofá Modular en Lino Crudo").
  - `DisplayOrder`: Orden secuencial del pin (1, 2, 3...) reflejado en la lista lateral.
  - `IsActive`: Visibilidad independiente del punto.

### 2.2 Degradación Segura y Disponibilidad Dinámica
Para evitar que un ambiente falle si un producto es archivado o se queda sin stock:
1. El backend computa `IsAvailable` y `UnavailableReason` en tiempo de consulta sin romper el ambiente.
2. Si `Product.Status != Active`: `IsAvailable = false`, `UnavailableReason = "Producto no disponible"`.
3. Si la variante tiene `StockQuantity <= 0`: `IsAvailable = false`, `UnavailableReason = "Agotado"`.
4. El frontend degrada elegantemente: el pin se colorea en escala de grises atenuada, el botón de compra individual se deshabilita con el motivo, y la acción de "Comprar Ambiente Completo" añade solo las piezas disponibles.

---

## 3. Experiencia de Usuario y Accesibilidad (WCAG 2.2)

- **Objetivos Táctiles (Touch Targets)**:
  - Pines con área de pulsación de mínimo **44x44 px** (`h-11 w-11`) para cumplimiento AAA en dispositivos móviles.
- **Navegación por Teclado y Foco**:
  - `focus-visible:ring-4 focus-visible:ring-amber-500/70` de alto contraste en cada pin y botón de acción.
  - Tecla `Escape` cierra de inmediato cualquier tooltip interactivo abierto.
- **Semántica Accessible**:
  - Botones con `aria-haspopup="dialog"`, `aria-expanded` dinámico y etiquetas descriptivas completas (`aria-label="Ver producto: Sofá Modular Nordhavn"`).
  - Tooltips marcados con `role="dialog"` y botón de cierre etiquetado.

---

## 4. "Comprar Ambiente Completo"
- Calcula el subtotal consolidado de todas las piezas con stock disponible en el ambiente.
- Al hacer clic, itera secuencialmente añadiendo cada variante al carrito (`useCartStore.addItem`), muestra un spinner de progreso, un mensaje de confirmación y abre automáticamente el drawer del carrito.

---

## 5. Editor Visual WYSIWYG en Panel Admin (`/admin/scenes/[id]/editor`)

### 5.1 Captura de Coordenadas al Clic
```typescript
const rect = canvasRef.current.getBoundingClientRect();
const clickX = parseFloat((((e.clientX - rect.left) / rect.width) * 100).toFixed(2));
const clickY = parseFloat((((e.clientY - rect.top) / rect.height) * 100).toFixed(2));
```
Al hacer clic en cualquier lugar de la imagen:
1. Se calcula la posición relativa exacta en porcentaje (0.00% a 100.00%).
2. Se muestra un indicador pulsante temporal en verde en la ubicación elegida.
3. Se despliega el Drawer lateral con las coordenadas pre-cargadas y selector de ajuste fino (+/- 0.1%).

### 5.2 Selector de Producto y Variante
- Buscador con autocompletado y debounce conectado a `GET /api/products?search=...`.
- Selección de producto carga automáticamente variantes, precio y stock disponible.

### 5.3 Alternancia "Modo Edición" vs "Vista Previa en Vivo"
- El administrador puede alternar en un clic entre la interfaz de posicionamiento de pines y la experiencia exacta que tendrá el cliente final, validando la interacción y compra antes de publicar.

---

## 6. Endpoints de la API

| Método | Endpoint | Roles | Descripción |
|---|---|---|---|
| `GET` | `/api/scenes` | Público | Lista de ambientes activos con conteo de hotspots |
| `GET` | `/api/scenes/{slug}` | Público | Detalle de ambiente con hotspots y disponibilidad calculada |
| `GET` | `/api/admin/scenes` | Admin, Manager | Lista de todos los ambientes (activos e inactivos) |
| `GET` | `/api/admin/scenes/{id}` | Admin, Manager | Detalle completo de ambiente por ID |
| `POST` | `/api/admin/scenes` | Admin, Manager | Crear nuevo ambiente |
| `PUT` | `/api/admin/scenes/{id}` | Admin, Manager | Actualizar metadatos de ambiente |
| `DELETE` | `/api/admin/scenes/{id}` | Admin, Manager | Eliminar ambiente y sus hotspots asociados |
| `POST` | `/api/admin/scenes/{id}/hotspots` | Admin, Manager | Añadir punto interactivo con validación de coordenadas (0-100%) |
| `PUT` | `/api/admin/scenes/{id}/hotspots/{hotspotId}` | Admin, Manager | Actualizar coordenadas o producto de un hotspot |
| `DELETE` | `/api/admin/scenes/{id}/hotspots/{hotspotId}` | Admin, Manager | Eliminar hotspot de un ambiente |
