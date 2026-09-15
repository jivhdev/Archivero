# Caso-5 — Botón de eliminar/descartar duplicado (2026-09-15)

*Resuelto en el vault del Método JA. Pegar este archivo en `preguntas/Caso-5.md` del repositorio de código. Independiente de Caso-3 y Caso-4 — se puede hacer en cualquier momento.*

## Qué agregar

En la pantalla de resolución de duplicados (`Vistas/ResolverDuplicadoWindow`, Caso-1 punto 4 — Revisar / Reemplazar / Dejar pendiente / Guardar como excepción), agregar una quinta opción: **"Eliminar duplicado"** (nombre tentativo — puede ajustarse).

**Comportamiento exacto (para no invertirlo):**
- **Reemplazar** (ya existente) = se queda el archivo **NUEVO** que llegó; se borra el archivo viejo que ya estaba guardado.
- **Eliminar duplicado** (nuevo) = se queda el archivo **VIEJO** que ya estaba guardado, tal cual estaba; se descarta/borra el archivo nuevo que acaba de llegar.

## Regla de activación

El botón "Eliminar duplicado" **siempre está visible** en la pantalla, pero solo se puede hacer click en él **después** de que el usuario haya usado "Revisar" (ver los dos documentos lado a lado) y haya confirmado ahí que son el mismo documento. Antes de eso, el botón está visible pero deshabilitado (no clickeable) — el usuario no puede eliminar un duplicado sin haberlo comparado visualmente primero.

## Fuera de alcance de este Caso (anotado para el futuro, no implementar ahora)

- **Carga masiva de documentos** para verificar si a una carpeta le faltan documentos: idea de Javier para una versión futura, solo si se confirma que tiene utilidad real en el uso diario. No implementar todavía.
- **Convertir imágenes u otros archivos que no sean PDF a PDF automáticamente**: técnicamente posible, pero es una ampliación de alcance de `SPEC.md` (hoy dice explícitamente que Archivero no procesa nada que no sea PDF). Javier decidió no priorizarlo por ahora — convertir una imagen a mano toma segundos. Queda anotado, no implementar.
