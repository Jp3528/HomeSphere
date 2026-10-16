# Arquitectura de Carrito, Cupones y Validación de Checkout — HomeSphere v1.0

## 1. Visión General y Principio de Autoridad del Servidor

En HomeSphere, toda la lógica de cálculo financiero, disponibilidad de stock, descuentos por cupones y asignación de tarifas de transporte se ejecuta con **100% de autoridad en el backend** (`apps/api`).
El cliente frontend (`apps/web` en Next.js) actúa exclusivamente como un visor reactivo y recolector de intención de compra, consumiendo el contrato determinista `POST /api/checkout/preview`. Bajo ninguna circunstancia el frontend envía montos monetarios calculados localmente.

```
+-------------------------------------------------------------------------------+
|                             Next.js Frontend                                  |
|  - Selector de variantes / Hotspot Click -> addItem(variantId, quantity)       |
|  - CartDrawer / Página /cart -> updateQuantity(itemId, qty), applyCoupon(code)|
|  - Checkout -> Selecciona Dirección + Método Envío                            |
+---------------------------------------+---------------------------------------+
                                        |
                          JSON Request  |  (Solo IDs, códigos y cantidades)
                                        v
+-------------------------------------------------------------------------------+
|                       ASP.NET Core Backend (Clean Architecture)              |
|  - Precios leídos en vivo de ProductVariants (nunca del payload)             |
|  - Verificación en tiempo real de StockQuantity y Estado Activo               |
|  - Validación Criptográfica & Reglas de Cupones (Vigencia, Topes, Mínimos)   |
|  - Tarifación de Transporte (Standard $15 / Gratis >= $1000 / Express $45)   |
|  - Prevención Anti-IDOR: Dirección de entrega debe pertenecer a UserId        |
+---------------------------------------+---------------------------------------+
                                        |
                       Breakdown Seguro |  (Subtotal, Descuentos, Envío, Total)
                                        v
                               Respuesta HTTP 200
```

---

## 2. Gestión Dual de Carrito: Invitados vs. Usuarios Autenticados

Para maximizar la conversión y reducir la fricción de compra, HomeSphere implementa persistencia completa tanto para sesiones anónimas como registradas.

### Ciclo de Vida del Token de Invitado (`X-Cart-Token`)
- Cuando un usuario no autenticado interactúa con el carrito, el backend detecta la ausencia de sesión y asigna un identificador opaco criptográficamente aleatorio (`GuestToken` UUIDv4).
- El token es emitido tanto en el encabezado HTTP de respuesta `X-Cart-Token` como en la cookie `homesphere_cart_token` (Lax, 30 días de caducidad).
- El cliente Next.js (`api-client.ts`) propaga automáticamente el encabezado `X-Cart-Token` en todas las solicitudes posteriores.

### Algoritmo de Fusión Determinista (Merge al Iniciar Sesión)
Al completar el login (`/api/auth/login`) o registro (`/api/auth/register`), el cliente despacha automáticamente `POST /api/cart/merge` enviando su `guestToken`:

1. **Caso 1: El usuario no tenía carrito previo:**
   - Se reasigna directamente el carrito de invitado: `guestCart.UserId = userId; guestCart.GuestToken = null;`.
2. **Caso 2: El usuario ya contaba con un carrito con artículos:**
   - Por cada artículo en el carrito de invitado:
     - Si la variante ya existía en el carrito del usuario, se suman las cantidades: `userItem.Quantity = Math.Min(99, userItem.Quantity + guestItem.Quantity)`.
     - Si la variante no existía, se crea un nuevo `CartItem` asociado al carrito del usuario.
   - El carrito de invitado huérfano es eliminado de la base de datos de manera transaccional (`_context.Carts.Remove(guestCart)`).

---

## 3. Motor de Cupones y Promociones

Los cupones se modelan en la entidad de dominio `Coupon` y admiten dos tipos de descuento:
1. **Porcentual (`Percentage`)**: Descuenta un porcentaje del subtotal, con soporte opcional de tope máximo en dólares (`MaxDiscountAmount`), evitando pérdidas en pedidos de gran volumen.
2. **Monto Fijo (`FixedAmount`)**: Descuenta una suma fija en dólares (por ejemplo, $50 USD), limitada al subtotal de la compra para evitar totales negativos.

### Reglas de Negocio Validadas en Cada Evaluación
- **Estado Activo**: El flag `IsActive` debe ser `true`.
- **Ventana de Vigencia Temporal**: `ValidFromUtc <= UtcNow <= ValidUntilUtc`.
- **Límite de Canjes Globales**: Si `UsageLimit` está definido, `TimesUsed < UsageLimit`.
- **Monto Mínimo de Pedido**: Si `MinOrderAmount` está definido, `Subtotal >= MinOrderAmount`.

---

## 4. Auditoría de Seguridad: Prevención Anti-IDOR

Durante el endpoint `POST /api/checkout/preview`:
- Si la solicitud incluye un `AddressId` y el usuario está autenticado, el servicio consulta la tabla `addresses` y verifica que `address.UserId == currentUserId`.
- Si un atacante intenta previsualizar un pedido inyectando el identificador UUID de la dirección de otro cliente, el sistema aborta de inmediato y responde con **HTTP 403 Forbidden** (`ForbiddenAccessException`), blindando la privacidad de datos personales y domiciliarios.

---

## 5. Resumen de Pruebas Unitarias de Validación (36 Pruebas Superadas)

| Escenario Probado | Clase de Prueba | Resultado |
|---|---|---|
| Adición de variante válida y cálculo de subtotal | `CartServiceTests` | ✅ **PASS** |
| Rechazo ante variante sin stock disponible (`StockQuantity = 0`) | `CartServiceTests` | ✅ **PASS** |
| Acumulación de cantidades al agregar la misma variante | `CartServiceTests` | ✅ **PASS** |
| Eliminación automática de ítem cuando cantidad = 0 | `CartServiceTests` | ✅ **PASS** |
| Merge determinista de carrito invitado a usuario existente | `CartServiceTests` | ✅ **PASS** |
| Reasignación limpia de carrito cuando usuario no tenía ítems | `CartServiceTests` | ✅ **PASS** |
| Cálculo de subtotal y envío estándar ($15) en checkout | `CheckoutServiceTests` | ✅ **PASS** |
| Bonificación de Envío Estándar GRATIS para subtotales >= $1000 | `CheckoutServiceTests` | ✅ **PASS** |
| Cupón porcentual con tope máximo de descuento | `CheckoutServiceTests` | ✅ **PASS** |
| Rechazo y advertencia al intentar usar cupón vencido | `CheckoutServiceTests` | ✅ **PASS** |
| Prevención Anti-IDOR ante dirección perteneciente a otro usuario | `CheckoutServiceTests` | ✅ **PASS** |
