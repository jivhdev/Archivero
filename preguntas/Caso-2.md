# Caso-2 — Carpeta observada no configurable + vigilancia que no reacciona (2026-09-15)

Javier detectó estos dos problemas al usar Archivero en el pc del trabajo. Un requerimiento a la vez, como siempre, y actualizá `ESTADO.md` al cerrar.

## 1. No hay forma de cambiar la carpeta observada una vez configurada (afecta REQ-001)

**Problema:** Javier abrió Archivero en otro equipo y quiso empezar de cero (borró las clasificaciones guardadas), pero no encontró ninguna opción para cambiar la carpeta observada — ni su ubicación ni su nombre. Hoy esa carpeta solo se define una vez, en el onboarding inicial (`OnboardingWindow`), y no hay pantalla ni botón para volver a tocarla después.

**Corrección:** agregar una opción de configuración (ej. desde `MainWindow` o "Administrar clasificaciones") donde el usuario pueda ver y cambiar la carpeta observada en cualquier momento — ubicación y nombre, sin perder lo demás. Si la carpeta anterior tenía pendientes o guardados indexados, definir qué pasa con esas referencias (dejarlas como están, no migrar archivos solos).

**Verificar de paso:** confirmar que el onboarding de primera vez (sin ninguna carpeta configurada aún) sigue pidiendo crear/elegir la carpeta como corresponde — no pareciera ser parte del bug, pero conviene confirmarlo en `ESTADO.md`.

## 2. La vigilancia no reacciona a archivos nuevos dejados en la carpeta observada (afecta REQ-002/REQ-005)

**Bug real y grave:** Javier dejó archivos directamente en la carpeta que se supone Archivero está observando, y no pasó nada — no aparecieron en pendientes, no se archivaron, ningún aviso. La vigilancia en tiempo real (`VigilanciaCarpetaService` / `FileSystemWatcher`) no está reaccionando como debería.

**Corrección:** investigar por qué el evento `Created` no dispara el flujo esperado en este escenario (¿instancia no corriendo en ese momento y sin reconciliación al arrancar? ¿ruta distinta a la configurada? ¿watcher caído en silencio?) y corregirlo. Cubrir también el caso de reconciliación al iniciar Archivero (si había archivos dejados mientras estaba cerrado, deben aparecer igual, como ya existe para el caso inverso — ver `ReconciliarPendientesConDisco` en `ESTADO.md`).

## Cierre del caso

Una vez implementados y verificados a mano los dos puntos, regenerar el `.exe` con `dotnet publish` (self-contained, ver comando en `GIT.md`) para dejar la distribución actualizada.
