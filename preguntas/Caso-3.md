# Caso-3 — Rediseño del paso de formato/patrón de carpetas (2026-09-15)

*Resuelto en el vault del Método JA. Pegar este archivo en `preguntas/Caso-3.md` del repositorio de código. Ejecutar ANTES que Caso-4 (Caso-4 depende de este).*

**Contexto para quien implemente esto:** la detección automática de formato de carpeta (Caso-1, punto 2) resultó más difícil de acertar siempre bien de lo esperado. La decisión de Javier es priorizar claridad guiada para el usuario por sobre "magia" de detección: menos adivinar, más pasos explícitos y una vista previa que sirva como verificación. Sos un ejecutor, no un diseñador — segui los pasos tal como están descritos acá, sin improvisar variantes. Si algo no está cubierto en este documento, detenete y avisale a Javier en vez de decidir por tu cuenta.

Esto reemplaza el paso de "Formato" del asistente de identificación/edición de documentos (`Vistas/IdentificarDocumentoWindow`), pasos 2 y 3 según la numeración de `SPEC.md` (REQ-003). El resto del asistente (Emisor/Tipo, nombre de archivo, confirmar) no cambia.

## Paso 2 (revisado) — Carpeta madre y Directo/Subcarpetas

1. El usuario elige la carpeta de destino **"madre"** (la carpeta raíz donde va a vivir todo lo de este tipo de documento).
2. En la misma pantalla, el usuario marca explícitamente una de estas dos opciones (nunca se detecta sola, es una decisión del usuario en este paso):
   - **"Guardar directo en esta carpeta"** → el documento se guarda dentro de la carpeta madre elegida, sin subcarpetas. Saltar directo al Paso 4 (nombre de archivo) — no se muestra nada del Paso 3.
   - **"Guardar en subcarpetas dentro de esta carpeta"** → continuar al Paso 3.

## Paso 3 (nuevo) — Elegir tipo de organización, patrón, y fecha de referencia

### 3a. Accesos rápidos + "Ver todas las opciones"

Se muestran botones/opciones de acceso rápido con los tipos de organización que el usuario ya tiene configurados como favoritos (por defecto, la primera vez, no hay ninguno configurado — ver 3b), más una opción fija que siempre está: **"Ver todas las opciones"**.

- El número de accesos rápidos es configurable por el usuario (puede tener 1, 2, 3, o más — no hay un mínimo obligatorio). Javier, para su propio uso, va a configurar 2: "Por año" y "Por año y mes".
- "Ver todas las opciones" siempre está presente, sin importar cuántos accesos rápidos haya configurados, y no ocupa un slot de acceso rápido (es aparte).

### 3b. Lista completa ("Ver todas las opciones")

Al elegir "Ver todas las opciones", mostrar esta lista completa de tipos, en este orden exacto (ordenados por granularidad creciente — de menos subdivisión a más):

1. Directo en la carpeta
2. Por año
3. Por año y semestre
4. Por año y trimestre
5. Por año y mes
6. Por año y quincena
7. Por año y semana
8. Por año, mes y día
9. Por mes (sin año)
10. Por semana del mes

Al hacer click en un tipo de esta lista, preguntar: **"¿Con cuál de tus accesos rápidos actuales querés reemplazar esta opción?"** (mostrar los accesos rápidos actuales para elegir cuál se reemplaza; si hay espacio libre — el usuario configuró menos accesos rápidos que el máximo que se decida soportar — ofrecer también "Agregar como acceso rápido nuevo" en vez de forzar un reemplazo). Después de elegir, ese tipo queda "enlazado" en el acceso rápido correspondiente para las próximas veces, y se continúa con el flujo normal usando el tipo recién elegido (no hace falta volver a buscarlo en la lista completa esta vez).

*(Nota del que arma este Caso, no es un pedido explícito de Javier — confirmar con él si no está de acuerdo: agregar al final de esta lista una opción "Ninguna de estas — patrón personalizado" que permita al usuario escribir su propio patrón a mano, para no dejar sin salida a un caso que ninguna de las 10 opciones cubra.)*

### 3c. Elegir el patrón exacto (una vez elegido el tipo)

No mostrar códigos de formato abstractos (nada de `yyyy/MM`). En cambio, generar y mostrar **ejemplos concretos** usando una fecha de referencia real (ver 3d para cuál fecha usar), y que el usuario elija cuál se parece a lo que ya usa. Tabla de patrones de ejemplo a ofrecer por tipo (usando como fecha de referencia ilustrativa el 15 de marzo de 2026 solo para esta tabla — en la app real se usa la fecha de referencia real de 3d):

| Tipo | Ejemplos de patrón a mostrar |
|---|---|
| Por año | `2026` |
| Por año y semestre | `2026/S1` · `2026-S1` · `2026/Semestre 1` |
| Por año y trimestre | `2026/T1` · `2026-T1` · `2026/Q1` |
| Por año y mes | `2026/03` · `202603` · `2026-03` · `03-2026` · `2026/Marzo` |
| Por año y quincena | `2026/03/Q1` · `2026-03-Q1` · `2026/Marzo - 1ra quincena` |
| Por año y semana | `2026/Semana 11` · `2026-W11` |
| Por año, mes y día | `2026/03/15` · `20260315` · `2026-03-15` |
| Por mes (sin año) | `03` · `Marzo` · `Mes 03` |
| Por semana del mes | `Semana 1` (dentro de la carpeta del mes correspondiente) |

("Directo en la carpeta" no tiene patrón — ya se resolvió en el Paso 2.)

### 3d. Fecha de referencia: hoy, o la del documento si está marcada

- Mientras el usuario todavía no marcó la fecha del documento, los ejemplos del punto 3c se generan usando **la fecha de hoy** como referencia. Avisarle esto al usuario con un texto breve (ej. "Estos ejemplos usan la fecha de hoy — marcá la fecha del documento abajo para ver el ejemplo real").
- En este mismo paso, el usuario marca la fecha del documento sobre el PDF (mismo mecanismo de marcado por coordenadas que ya existe para otros campos — ver `SPEC.md`).
- Apenas se marca esa fecha, los ejemplos del punto 3c y la vista previa (ver 3e) se recalculan usando **la fecha detectada en el documento**, no más la de hoy.
- Si el tipo elegido es "Por mes (sin año)" o "Por semana del mes", igual conviene marcar la fecha si el documento la tiene, para que la vista previa sea exacta — pero no bloquea avanzar si el usuario decide no marcarla (usar la fecha de hoy como referencia en ese caso).

### 3e. Vista previa — sin cambios en el mecanismo, sigue siendo obligatoria

La vista previa de tres tiempos (anterior/actual/futuro) y su botón de actualizar, tal como quedaron en Caso-1 punto 3, se mantienen exactamente igual — ahora reflejan el tipo/patrón elegido acá y la fecha de referencia de 3d. Sigue siendo obligatoria antes de confirmar cualquier configuración nueva o editada.
