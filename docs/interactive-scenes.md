# HomeSphere — Interactive Room Scenes & Hotspot Canvas Architecture (Session 3R)

Este documento detalla la arquitectura técnica, modelo matemático de coordenadas normalizadas, sincronización bidireccional y cumplimiento de accesibilidad (WCAG 2.1 AA) implementados en el módulo de **Ambientes Interactivos** de HomeSphere durante la **Semana 3**.

---

## 1. El Diferencial del Proyecto: Interiorismo Interactivo

El módulo de **Ambientes Interactivos** permite al usuario explorar habitaciones reales de diseño (salones nórdicos, comedores contemporáneos, dormitorios minimalistas) e interactuar con pines posicionales vinculados al catálogo y a variantes específicas.

```
       ┌────────────────────────────────────────────────────────┐
       │             Lienzo Visual (SceneCanvas)                │
       │                                                        │
       │     [1] Pin Sofá (38%, 60%) ───► Popover flotante      │
       │                                   • Foto               │
       │                                   • Título & Precio    │
       │                                   • Link PDP           │
       │     [2] Pin Mesa (52%, 75%)                            │
       │     [3] Pin Alfombra (56%, 85%)                        │
       └───────────────────────────┬────────────────────────────┘
                                   │ Sincronización
                                   ▼ Bidireccional
       ┌────────────────────────────────────────────────────────┐
       │        Lista Curada Lateral (InteractiveSceneView)     │
       │  [1] Sofá Modular Nordhavn 3P  ──── $1,450.00  [Ver]   │
       │  [2] Mesa Centro Travertino    ──── $890.00    [Ver]   │
       │  [3] Alfombra Atlas Lana       ──── $410.00    [Ver]   │
       │                                                        │
       │  Total Estilismo: $2,750.00                            │
       └────────────────────────────────────────────────────────┘
```

---

## 2. Sistema de Coordenadas Normalizadas (0.00% a 100.00%)

### 2.1 Razón de Diseño
El almacenamiento de píxeles absolutos (`px`) falla en la web moderna donde la pantalla varía desde un smartphone de 375px hasta monitores 4K. Al normalizar las coordenadas como porcentajes decimales:
$$\text{X}_{\text{norm}} = \frac{x_{\text{pixel}}}{w_{\text{imagen}}} \times 100, \quad \text{Y}_{\text{norm}} = \frac{y_{\text{pixel}}}{h_{\text{imagen}}} \times 100$$
el pin se renderiza con exactitud matemática sub-pixel en cualquier relación de aspecto:
```tsx
style={{
  left: `${hotspot.xCoordinate}%`,
  top: `${hotspot.yCoordinate}%`,
}}
```

### 2.2 Validación en el Servidor
En la capa de aplicación y entidad:
- Validación estricta con anotaciones `[Range(0.0, 100.0)]`.
- Método de dominio `ValidateCoordinates` que rechaza coordenadas fuera del rango con `BusinessRuleValidationException` (RFC 7807 400 Bad Request).
- Columna PostgreSQL configurada como `numeric(5,2)` asegurando precisión de centésimas de porcentaje sin redondeos flotantes.

---

## 3. Inteligencia de Posicionamiento de Tooltips

Para evitar que los popovers de los pines se recorten en los bordes de la pantalla (overflow boundary clipping):
- **Eje Y**: Si el pin está en la mitad inferior de la imagen (`yCoordinate > 55%`), el popover se abre hacia arriba (`bottom-full mb-3`). En caso contrario, se abre hacia abajo (`top-full mt-3`).
- **Eje X**: Si el pin está cerca del borde derecho (`xCoordinate > 65%`), el popover se alinea hacia la izquierda (`right-0`).

---

## 4. Accesibilidad y Navegación por Teclado (WCAG 2.1 AA)

| Criterio WCAG | Implementación Técnica |
| :--- | :--- |
| **Teclado / Foco** | Todos los pines son elementos `<button>` nativos accesibles mediante `Tab` y accionables con `Enter` o `Espacio`. |
| **Semántica ARIA** | Atributos `aria-expanded` dinámicos reflejando el estado abierto/cerrado, y `aria-label` descriptivo indicando el producto. |
| **Escape Key** | Event listener global que escucha `Escape` para cerrar inmediatamente cualquier tooltip abierto y devolver el foco. |
| **Contraste Visual** | Pines con anillo de contraste blanco/ámbar y números de alto contraste legibles sobre cualquier fotografía de fondo. |
| **Sincronización Bidireccional** | Al seleccionar un producto en la lista lateral, el pin correspondiente se enfoca y abre automáticamente su popover. |

---

## 5. Rendimiento y Streaming

- **Next.js 16 Partial Prerendering (PPR)**: La página `/scenes` y los ambientes individuales `/scenes/[slug]` generan el esqueleto visual en el servidor y transmiten el contenido interactivo mediante `<Suspense>`.
- **Eager Loading Optimizado en EF Core**:
  ```csharp
  Include(s => s.Hotspots)
      .ThenInclude(h => h.Product)
          .ThenInclude(p => p.Images)
  ```
  Evita problemas N+1 y entrega toda la escena y sus productos en una sola consulta SQL optimizada.
