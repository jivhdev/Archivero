# Caso-9 — Validación de entradas/rutas + log de auditoría (2026-09-25)

*Resuelto en el vault del Método JA. Preparación de seguridad y trazabilidad antes de repartir Archivero a otros usuarios. Las decisiones de diseño ya están tomadas — no hay que volver a decidirlas, hay que implementarlas tal como están.*

**Forma de trabajo, obligatoria para las dos mejoras, en este orden exacto:**
1. Escribir tests que demuestren el problema actual (deben fallar con el código de hoy, antes de tocar nada más).
2. Implementar la corrección hasta que esos tests pasen, sin romper ninguno de los tests existentes (166 al momento de escribir esto).
3. Un commit por mejora, con mensaje que describa el problema encontrado y la corrección aplicada.
4. Una nota breve de cambios por mejora en `ESTADO.md`: qué se corrigió, por qué, cómo se verificó.

Javier quiere poder mostrar, con evidencia real, el estado antes y después de cada corrección — por eso el orden test-primero no es negociable.

## Mejora 1 — Validación de entradas y rutas

Aplica a todo texto que termine formando parte de un nombre de archivo o una ruta: texto extraído del PDF por coordenadas, texto tipeado por el usuario (Emisor, Tipo, patrón personalizado de Caso-3, nombre editado a mano en Caso-4), y los componentes de carpeta derivados de un patrón. Centralizar esto en un servicio único (ej. `Servicios/ValidadorRutaService.cs`) que se llama desde el guardado automático (REQ-002), el asistente de identificación (REQ-003), y el flujo sin texto (Caso-4) — no duplicar la lógica en cada lugar.

### a) Caracteres de control y nulos — SE RECHAZA, no se sanea
Si cualquiera de estos textos contiene un carácter de control ASCII (0-31) o el carácter 127 (DEL), incluido el byte nulo: **rechazar el documento entero**, nunca intentar limpiarlo. Es indicio de un dato corrupto o construido a propósito para causar problemas, y no hay forma segura de "arreglarlo" sin arriesgar el significado real. Va a pendientes con motivo "Texto con caracteres inválidos".

### b) Caracteres inválidos de Windows en un nombre — se sanea (reemplazo), salvo nombres reservados
Los caracteres `< > : " / \ | ? *` no son válidos dentro de un segmento de nombre de archivo o carpeta (aunque `\` y `/` sí son válidos como separadores en una ruta completa — la regla acá es por segmento, no por la ruta entera). Si alguno de estos aparece en un texto que va a formar un solo segmento de nombre: **reemplazar cada uno por `_`** — es común que documentos reales tengan estos caracteres en un título, y rechazar el documento completo por esto sería demasiado agresivo.

**Excepción — nombres reservados de Windows:** si el nombre resultante (sin distinguir mayúsculas, con o sin extensión) coincide exactamente con `CON`, `PRN`, `AUX`, `NUL`, `COM1`-`COM9`, o `LPT1`-`LPT9`: **rechazar el documento** (no sanear con un reemplazo automático, podría generar colisiones o comportamiento raro de Windows al tratar de crear el archivo). Va a pendientes con motivo "Nombre de archivo reservado por Windows".

### c) Caracteres válidos — no tocar, no rechazar
`& ( ) + ' % , ; = @ # $ ! ^ ~ [ ] { } - _ .` y en general cualquier carácter imprimible que Windows sí permite en nombres de archivo (ej. "Factura (1).pdf" es válido tal cual, no se toca). No inventar restricciones más allá de lo que Windows realmente prohíbe.

### d) Espacios y puntos sobrantes
Windows tiene problemas con espacios/puntos al final de un nombre, y espacios al principio: sanear con un trim (quitar espacios al principio, y espacios/puntos al final) antes de usar el nombre.

### e) Path traversal — verificación obligatoria de contención, siempre
Después de calcular la ruta final completa (carpeta destino + subcarpetas del patrón + nombre de archivo), antes de guardar:
1. Resolver esa ruta con `Path.GetFullPath` (para colapsar cualquier `..\`, ruta relativa, o similar).
2. Verificar que el resultado quede **efectivamente dentro** de la carpeta de destino configurada — comparación por segmentos completos de ruta, nunca por substring (ej. que `"C:\Docs\FooBar"` NO cuente como "dentro de" `"C:\Docs\Foo"` solo porque el texto empieza igual).
3. Si la ruta final calculada no queda contenida en la carpeta configurada, por cualquier motivo: **rechazar**, nunca guardar ahí. Va a pendientes con motivo "La ubicación calculada no corresponde a la carpeta configurada".

Esto cubre tanto texto extraído malicioso/corrupto como un eventual bug futuro de cálculo de patrón — es la última línea de defensa, así que se aplica siempre, sin excepción, incluso si las validaciones anteriores ya pasaron.

### f) Largos máximos
- Nombre de archivo (sin extensión): máximo 200 caracteres.
- Ruta completa final: máximo 240 caracteres (margen de seguridad bajo el límite clásico de Windows de 260, para no depender de que el usuario tenga habilitado el soporte de rutas largas).

Si se supera cualquiera de los dos: rechazar, va a pendientes con motivo "Nombre o ruta demasiado larga".

### g) Regla general — nunca guardado silencioso
Cualquier falla de validación de este Caso siempre termina igual: el documento no se toca, no se guarda en ningún lado, y aparece en "Pendientes por reconocer" con un motivo específico y legible para el usuario (los motivos exactos de arriba, o similares). Nunca una excepción sin manejar que rompa la app, y nunca un guardado silencioso en una ubicación inesperada.

## Mejora 2 — Log de auditoría

### Formato y ubicación
Texto plano (no CSV — evita el problema de escapar comas dentro de nombres de archivo reales sin ganar nada a cambio). Un archivo, una línea por evento: `[fecha-hora ISO-8601] TIPO_DE_EVENTO — detalle`. Ubicación: `%LocalAppData%\Archivero\auditoria.log` (junto a la base SQLite, mismo criterio de ADR-002).

### Rotación
Cuando `auditoria.log` supere 10 MB: rotar. El archivo actual pasa a `auditoria.log.1` (si ya existe `.1`, ese pasa a `.2`, y así hasta `.3` — no conservar más de 3 archivos rotados), y se empieza un `auditoria.log` nuevo y vacío. Esto acota el crecimiento sin perder historial reciente.

### Qué se registra (cada línea con fecha-hora + tipo de evento + detalle relevante)
- Documento detectado (llegó un archivo nuevo a la carpeta observada).
- Documento clasificado / guardado automático (Emisor+Tipo, ruta final, nombre resultante).
- Documento enviado a pendientes, **con el motivo** (incluye los motivos de la Mejora 1).
- Guardado manual (sin texto extraíble, Caso-4).
- Clasificación creada / editada / borrada (qué configuración, qué cambió a grandes rasgos — no hace falta el detalle completo de cada campo).
- Cambio de carpeta observada.
- Rechazo por validación (Mejora 1), con el motivo exacto.

### Qué NO se registra
No volcar el contenido completo del PDF ni el texto extraído completo — solo los campos clave ya mencionados arriba (Emisor, Tipo, ruta, motivo). No hace falta más detalle que ese para trazabilidad.

### Evitar que un valor problemático falsifique el log (inyección de líneas)
Antes de escribir cualquier dato variable (nombre de archivo, texto extraído, ruta) dentro de una línea del log: reemplazar cualquier salto de línea (`\n`, `\r`) y carácter de control por un espacio o una secuencia visible tipo `\n` literal, de forma que un valor nunca pueda partir una línea de log en dos ni fingir ser una entrada distinta. Esto aplica especialmente a los valores que llegaron a rechazarse por la Mejora 1 — son justo los que más probablemente traigan algo raro.

### Falla al escribir el log nunca detiene la app
Toda escritura al log de auditoría va envuelta en su propio manejo de errores — si falla escribir (disco lleno, permisos, lo que sea), la aplicación sigue funcionando con normalidad; como mucho, ese fallo puntual de logging se pierde, pero nunca debe interrumpir el guardado real de un documento ni ninguna otra funcionalidad.

## Cierre del caso

Regenerar el `.exe` con `dotnet publish` (self-contained, ver comando en `GIT.md`) una vez terminadas y verificadas ambas mejoras.
