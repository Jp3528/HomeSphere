# HomeSphere — Identity & Security Architecture Reference (Session 1R)

Este documento detalla el modelo de amenazas, políticas de seguridad y controles criptográficos implementados en HomeSphere para la gestión de identidad, autenticación, autorización y protección de datos.

---

## 1. Modelo de Amenazas (STRIDE) y Mitigaciones

| Amenaza (STRIDE) | Vector Potencial | Control Implementado en HomeSphere |
| :--- | :--- | :--- |
| **Spoofing (Suplantación)** | Credenciales robadas, adivinación de contraseñas, reutilización de tokens expirados. | • PBKDF2 (SHA-256, 100,000 iteraciones, sal criptográfica de 128 bits).<br>• Access Tokens JWT de corta duración (15 minutos).<br>• Validación criptográfica de firma HMAC-SHA256 en cada petición. |
| **Tampering (Manipulación)** | Modificación de claims JWT en tránsito, inyección de roles no asignados. | • Verificación estricta de firma del JWT (`ValidateIssuerSigningKey = true`).<br>• `ValidateLifetime = true` y validación de Issuer/Audience.<br>• Roles evaluados estrictamente en el servidor. |
| **Repudiation (Repudio)** | Negación de cambios en direcciones, perfiles o pedidos. | • Registro de auditoría con `CorrelationIdMiddleware` (`X-Correlation-Id`).<br>• Entidades base con `CreatedAt` y `UpdatedAt`. |
| **Information Disclosure (Fuga)** | Exposición de credenciales en logs, tokens en URLs, stack traces en respuestas HTTP. | • RFC 7807 `ProblemDetails` estandarizado; los errores en producción no exponen trazas internas.<br>• Serilog configurado para enmascarar/ignorar contraseñas y secretos.<br>• Tokens enviados exclusivamente en encabezados `Authorization: Bearer` o cuerpos POST protegidos. |
| **Denial of Service (Denegación)** | Ataques de fuerza bruta contra login, saturación de tokens de refresco. | • Rotación de tokens de refresco con invalidación automática de cadenas de tokens comprometidas.<br>• Búsquedas indexadas en base de datos (`IX_Users_Email`, `IX_RefreshTokens_Token`). |
| **Elevation of Privilege (IDOR / Escalada)** | Usuarios cliente intentando acceder a direcciones ajenas o invocar endpoints de administración. | • **Anti-IDOR**: Verificación forzosa a nivel de capa de aplicación (`entity.UserId == currentUserId`). Respuestas 403 Forbidden cuando el recurso no pertenece al usuario autenticado.<br>• **RBAC**: Endpoints administrativos protegidos con `[Authorize(Roles = "ADMIN")]`. |

---

## 2. Controles Criptográficos y de Identidad

### 2.1 Hashing de Contraseñas
- **Algoritmo**: PBKDF2 (`KeyDerivation.Pbkdf2`) con función pseudoaleatoria `HMACSHA256`.
- **Iteraciones**: `100,000` iteraciones (conforme a las directrices OWASP y NIST SP 800-63B).
- **Sal**: `128 bits` (16 bytes) generados mediante generador de números pseudoaleatorios criptográficamente seguro (`RandomNumberGenerator.GetBytes`).
- **Subclave (Hash)**: `256 bits` (32 bytes).
- **Formato de Almacenamiento**: `Salt:Hash` codificados en Base64.
- **Protección contra Temporización**: Comparación en tiempo constante (`CryptographicOperations.FixedTimeEquals`) para prevenir ataques de temporización (timing attacks).

### 2.2 Tokens de Acceso (JWT)
- **Algoritmo de Firma**: `HMAC-SHA256` (`SecurityAlgorithms.HmacSha256Signature`).
- **Tiempo de Expiración**: `15 minutos`.
- **Claims Estándar**:
  - `sub`: Identificador único de usuario (`Guid`).
  - `email`: Correo electrónico normalizado.
  - `jti`: Identificador único de token para trazabilidad.
  - `role`: Roles del usuario (`CUSTOMER`, `ADMIN`, `WAREHOUSE`, `MANAGER`).

### 2.3 Tokens de Refresco y Detección de Reutilización
- **Generación**: Cadena Base64 de `64 bytes` generada por `RandomNumberGenerator`.
- **Tiempo de Expiración**: `7 días`.
- **Rotación Estricta**: Cada vez que se usa un refresh token para obtener un nuevo par de tokens:
  1. El token usado se marca como revocado con la fecha actual (`RevokedAt = DateTime.UtcNow`).
  2. Se asocia al nuevo token generado (`ReplacedByToken = nuevoToken`).
  3. Si se intenta presentar un token ya revocado o reemplazado, la petición es rechazada de inmediato (`401 Unauthorized`), mitigando el reuso de tokens interceptados.
- **Revocación Explícita**: Endpoint `/api/auth/revoke` permite al usuario revocar inmediatamente su sesión activa.

---

## 3. Matriz de Autorización y Control de Acceso (RBAC)

| Recurso / Operación | Método | Ruta | Rol Mínimo Requerido | Validación de Propiedad |
| :--- | :--- | :--- | :--- | :--- |
| Registro de usuario | `POST` | `/api/auth/register` | Anónimo | N/A (asigna rol `CUSTOMER`) |
| Inicio de sesión | `POST` | `/api/auth/login` | Anónimo | N/A |
| Refresco de sesión | `POST` | `/api/auth/refresh` | Anónimo | Validación de token criptográfico |
| Cierre de sesión | `POST` | `/api/auth/revoke` | Autenticado | Revoca token de sesión activa |
| Consultar perfil propio | `GET` | `/api/me` | Autenticado | Lee claims del token |
| Actualizar perfil propio | `PUT` | `/api/me` | Autenticado | Lee claims del token |
| Listar mis direcciones | `GET` | `/api/addresses` | Autenticado | Filtro forzoso por `UserId` del token |
| Crear dirección | `POST` | `/api/addresses` | Autenticado | Asigna forzosamente `UserId` del token |
| Actualizar dirección | `PUT` | `/api/addresses/{id}` | Autenticado | **Anti-IDOR**: `address.UserId == currentUserId` |
| Eliminar dirección | `DELETE` | `/api/addresses/{id}` | Autenticado | **Anti-IDOR**: `address.UserId == currentUserId` |
| Establecer predeterminada | `PATCH` | `/api/addresses/{id}/default` | Autenticado | **Anti-IDOR**: `address.UserId == currentUserId` |
| Listar todos los usuarios | `GET` | `/api/admin/users` | `ADMIN` | Restricción por rol de claim |
| Cambiar estado de usuario | `PATCH` | `/api/admin/users/{id}/status` | `ADMIN` | Restricción por rol de claim |

---

## 4. Prácticas de Código Limpio y Protección Perimetral

1. **Sin secretos en el control de versiones**: Las claves JWT y cadenas de conexión reales se mantienen en variables de entorno o archivos `appsettings.Development.json` excluidos en `.gitignore`.
2. **CORS restrictivo**: Solo los orígenes configurados en `AllowedOrigins` (por defecto `http://localhost:3000`) pueden interactuar con la API con credenciales.
3. **Validación de Usuarios Inactivos**: Si un usuario tiene `IsActive == false`, tanto el inicio de sesión como la rotación de tokens son denegados inmediatamente, asegurando el bloqueo efectivo de cuentas suspendidas.
