# Caso-10 — Evidencia real de Caso-9 (validación + log de auditoría) (2026-09-26)

*Resuelto en el vault del Método JA. Este Caso no pide implementar nada nuevo — pide un reporte de evidencia real sobre lo hecho en Caso-9, para que Javier lo copie a un documento.*

**Regla no negociable de este Caso:** nada de código en la respuesta, solo hallazgos concretos. Si algo no lo verificaste corriéndolo de verdad, decilo explícitamente ("esto no lo verifiqué") en vez de suponerlo o redactarlo como si lo hubieras confirmado. Todo lo que sigue requiere ejecutar cosas de verdad (tests, git log, coverlet) — no se contesta de memoria.

Entregá esto, en este orden:

## 1. Commits de las dos mejoras
Hash corto y mensaje de cada commit asociado a las dos mejoras de Caso-9.

## 2. Por cada mejora, por separado (validación de entradas, y log de auditoría)
- **Problema detectado** (una o dos líneas).
- **Corrección aplicada** (una o dos líneas).
- **Tests nuevos:** nombre de cada uno y qué verifica.
- **Antes y después:** corré los tests nuevos contra el código anterior a la corrección (el commit previo — usá `git stash`, un `git worktree`, o lo que te resulte más práctico para aplicar esos mismos tests sobre el código viejo) y contá cuántos fallan. Después corré los mismos tests contra el código actual y contá cuántos pasan. Reportá los números reales de ambas corridas, no una suposición de que "obviamente" fallarían antes.

## 3. Reglas de validación finales, en tres grupos
- Qué se **rechaza**.
- Qué se **sanea**.
- Qué se **permite**.

Incluir explícitamente dónde quedaron caracteres como `& ( ) + ' %`.

## 4. Log de auditoría
- Ubicación del archivo.
- Formato.
- Rotación.
- Qué eventos registra.
- Cómo se evita que un valor falsifique una entrada (inyección de líneas).
- **5 líneas de ejemplo reales del log** (generadas de verdad, no inventadas) — si contienen rutas personales de esta PC, reemplazalas por rutas genéricas antes de mostrarlas.

## 5. Resultado total de `dotnet test`
Cuántos tests pasan de cuántos, en total (todo el proyecto, no solo lo nuevo de Caso-9).

## 6. Cobertura actualizada con Coverlet
Buscá en `ESTADO.md` el formato de la medición de cobertura anterior (si existe una) y reportá esta nueva medición en el mismo formato exacto, para que sean comparables:
- Total: % de líneas (cubiertas/totales) y % de ramas.
- Por carpeta: `Datos`, `Servicios`, `Vistas`, `App.xaml.cs` y raíz, `obj`.
- Cobertura solo de lógica (`Datos` + `Servicios`): % y líneas cubiertas/totales.

Si no encontrás una medición anterior con la que comparar, decilo explícitamente y reportá igual la medición actual en un formato claro.
