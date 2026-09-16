# Caso-6 — Tres ajustes de uso real (2026-09-16)

*Resuelto en el vault del Método JA. Los 3 puntos son independientes entre sí — se pueden hacer en cualquier orden, uno a la vez.*

## 1. La lista de PDFs sin texto extraíble no se actualiza sola

**Problema:** un PDF sin texto extraíble nuevo (la lista de Caso-4, "Pendientes de distribuir" o como haya quedado el nombre) no aparece hasta que el usuario cierra y vuelve a abrir Archivero. La lista de "Pendientes por reconocer" normal sí se actualiza en vivo — esta no.

**Corrección, en este orden de preferencia:**
1. Primero, intentar arreglar la causa real: revisar por qué esta lista en particular no reacciona al mismo evento de archivo nuevo que sí dispara la lista normal de pendientes (probablemente el ViewModel/colección de esta lista no se está notificando/refrescando igual que la otra).
2. Además, sin importar si el punto 1 se resuelve del todo o no: agregar un botón de "Recargar" visible en esa sección, para que el usuario pueda forzar el refresco a mano si hiciera falta.

## 2. "Guardados recientes" debe recordar más historial

**Problema:** la lista de guardados automáticos recientes (Caso-1) hoy tiene muy poca memoria — se olvida de guardados anteriores casi enseguida.

**Corrección:** que recuerde al menos los últimos 10 guardados (no solo el/los más recientes), con un límite razonable para no crecer sin control — 10 es un piso sugerido por Javier, no un techo estricto; usar buen criterio si conviene un número un poco mayor, pero no ilimitado.

## 3. En el paso de elegir formato (Caso-3, cuando es "subcarpetas"), la elección queda bloqueada

**Problema:** una vez que el usuario elige un tipo de organización (ej. "Por año"), si se equivocó y quería otro (ej. "Por año y mes"), no hay forma de cambiarlo ahí mismo — hay que cancelar todo el asistente de identificación/edición y empezar de nuevo desde el principio.

**Corrección:** dentro de ese mismo paso, la elección de tipo de organización tiene que seguir siendo interactiva/reelegible en cualquier momento mientras no se avance al paso siguiente — al hacer click en un tipo distinto, se actualiza la elección (y en cascada, los ejemplos de patrón y la vista previa) sin necesidad de cancelar ni reiniciar el asistente.

## Cierre del caso

Una vez implementados y verificados a mano los 3 puntos, regenerar el `.exe` con `dotnet publish` (self-contained, ver comando en `GIT.md`) para dejar la distribución actualizada.
