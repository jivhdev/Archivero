# Estado del proyecto

> Se actualiza al final de cada sesión. Es lo tercero que hay que leer (después de AGENTS.md y SPEC.md) para saber dónde quedamos.

## Última sesión
- Fecha: 2026-09-13
- Qué se hizo:
  - Se re-chequeó `SPEC.md` local contra `C:\MQD\01-Proyectos\Archivero\semilla-archivero\SPEC.md` a pedido de Javier — **sin diferencias** esta vez, la copia local ya estaba al día.
  - Se mergearon a `main` los 3 PRs que habían quedado pendientes de la sesión anterior (PR #6 REQ-002 duplicados, PR #7 empaquetado, PR #8 LICENSE+ícono), sin conflictos. Se verificó que el resultado combinado compila y pasa los 63 tests automáticos.
  - Se corrigió la sección "Decisiones abiertas" de este archivo (PR #10), que todavía daba como pendientes la licencia y la personalización visual, ya resueltas.
  - Se creó `Distribucion/` en la raíz del repo (PR #11) con el `.exe` autocontenido regenerado y `Distribucion/ESTADO-DISTRIBUCION.md` (versionado) documentando el piloto — el binario mismo queda excluido del repo vía `.gitignore`, con la misma regla que ya protege a `publish/`.

## Siguiente paso
- **Los 5 requerimientos funcionales de SPEC.md (REQ-001 a REQ-005) están completos, mergeados a `main`, y probados por Javier**, incluida la resolución de duplicados de REQ-002. Varios bugs reales encontrados y arreglados en el camino (ver secciones de cada uno más abajo).
- Empaquetado, licencia, ícono, y la carpeta `Distribucion/` para el piloto también están en `main`.
- No queda nada pendiente conocido del alcance de `SPEC.md`. El siguiente paso es la ronda de uso real en piloto (`Distribucion/ESTADO-DISTRIBUCION.md` dice "En piloto desde 2026-09-13 — pendiente de revisión") y, más adelante, cerrar los "Open issues" que quedan (nombres finales de interfaz).

### REQ-001 — qué se construyó
- Proyecto WPF (.NET 8) creado en `src/Archivero`, solución `Archivero.sln`.
- SQLite (`Microsoft.Data.Sqlite`) para la tabla `Configuracion` (clave/valor), guardada en `%LocalAppData%\Archivero\archivero.db`.
- `Servicios/CarpetaObservadaService.cs`: sugiere nombre/ubicación por defecto, valida colisión, crea la carpeta y la marca como observada.
- `Vistas/OnboardingWindow.xaml(.cs)`: asistente de primera configuración.
- Se instaló el SDK de .NET 8 en esta máquina (no estaba presente).

### REQ-003 — qué se construyó (escenario principal)
- **PDF embebido**: `Servicios/Pdf/LectorPdf.cs`, envoltorio de Docnet.Core (PDFium, MIT) — renderiza páginas y extrae texto dentro de un rectángulo (fracción 0-1 de la página, independiente de resolución).
- **Marcado sobre el PDF**: `Vistas/VisorPdfConMarcado` (UserControl reutilizable) — click-and-drag sobre la página para marcar un rectángulo; se reutiliza para Emisor, Tipo, Fecha y campo de nombre de archivo (la misma herramienta para cualquier dato, como pide SPEC.md).
- **Modelo de datos nuevo**: `EntidadesConocidas` (Emisor/Tipo con autocompletado), `Configuraciones`, `PatronesReconocimiento`, `Marcas`, `Pendientes` (ver `Datos/BaseDeDatos.cs`).
- **Detección de formato/patrón de carpeta**: `Servicios/FormatoCarpetaService.cs` — mira las subcarpetas ya existentes en la carpeta de destino elegida y detecta Directo / Por año / Por año y mes, y cómo está escrito cada componente (`yyyy` vs `yy`, `MM` vs `MMMM`).
- **Guardado seguro**: `Servicios/ClasificadorService.cs` — copia, verifica (hash SHA-256), y recién ahí borra el original.
- **Vigilancia de la carpeta**: `Servicios/VigilanciaCarpetaService.cs` — `FileSystemWatcher` en tiempo real + escaneo al arrancar. Por ahora, **todo PDF nuevo pasa a pendientes** (la coincidencia automática contra configuraciones existentes es REQ-002).
- **Asistente**: `Vistas/IdentificarDocumentoWindow` — 5 pasos (Emisor/Tipo → Carpeta → Formato → Nombre de archivo → Confirmar), con Posponer/Cancelar disponibles en cualquier paso.
- `MainWindow` ahora muestra la lista real de "Pendientes por reconocer"; doble clic abre el asistente.
- Tests automáticos nuevos en `src/Archivero.Tests` (19, todos verdes): extracción por coordenadas (con PDFs reales generados en el propio test), detección/derivación de formato de carpeta, coincidencia exacta Emisor+Tipo.

### REQ-003 — escenario "vincular a configuración existente"
Implementado: si el Emisor+Tipo que se está identificando ya tiene una configuración guardada, el asistente ofrece vincular el documento actual a ella como patrón de reconocimiento adicional (en vez de intentar crear una segunda configuración, que rompía con un error de SQLite). La carpeta/formato/nombre quedan fijados por la configuración existente; igual hay que marcar Fecha y/o el campo de nombre si esa configuración los usa, porque las coordenadas son específicas del diseño de cada documento.

### REQ-003 — escenario "posponer con progreso guardado"
Implementado: tabla `Borradores` (`Datos/BorradorRepository.cs`), clave = ruta del archivo pendiente, valor = JSON con lo tipeado/marcado hasta el momento. Se guarda al tocar "Posponer" o al cerrar la ventana sin terminar (la X, tratado igual que posponer per SPEC.md); se restaura automáticamente la próxima vez que se abre ese mismo documento, con un aviso. "Cancelar" (con confirmación) descarta el borrador; guardar con éxito también lo limpia. **Falta que Javier lo pruebe a mano.**

### REQ-003 — qué queda fuera a propósito (no es un defecto)
- Colisión de nombre de archivo al clasificar el documento actual: por ahora solo muestra un error simple, no las 4 opciones completas de REQ-002 (Revisar/Reemplazar/Pendiente/Excepción) — ese flujo es explícitamente de REQ-002.

### REQ-002 — qué se construyó
- **Coincidencia automática**: `Servicios/CoincidenciaAutomaticaService.cs` — cuando llega un PDF, prueba los patrones de reconocimiento de todas las configuraciones guardadas: extrae texto en la coordenada de Emisor y de Tipo de cada patrón y compara exacto (sin distinguir mayúsculas/espacios) contra el Emisor+Tipo de su propia configuración.
- **Guardado automático**: `Servicios/GuardadoAutomaticoService.cs` — si un patrón coincide, extrae Fecha y/o Nombre de archivo según corresponda, valida que tengan forma válida, y clasifica el archivo. Devuelve un resultado tipado (Guardado / ValorInvalido / Duplicado / CarpetaNoDisponible) para que quien lo llama decida qué hacer.
- **`VigilanciaCarpetaService` actualizado**: ahora intenta la coincidencia automática antes de mandar algo a pendientes; maneja explícitamente PDF sin texto extraíble (no se toca) y archivo corrupto/no legible (se ignora ese archivo puntual, sigue observando los demás — RNF-2).
- **Cambio de modelo**: `ConfiguracionDocumento.Marcas` (lista plana) pasó a ser `Patrones` (`List<PatronReconocimiento>`), porque la coincidencia automática necesita probar cada patrón (cada diseño de documento) por separado, no mezclar las marcas de todos.
- `MainWindow` ahora también muestra un historial "Guardados automáticamente".
- Tests nuevos: `CoincidenciaAutomaticaServiceTests` y `GuardadoAutomaticoServiceTests` (30 tests en total, todos verdes).

### REQ-002 — qué queda fuera a propósito (no es un defecto)
- Colisión de nombre de archivo: hoy solo evita sobreescribir (manda a pendientes). Faltan las 4 opciones completas de SPEC.md (Revisar lado a lado / Reemplazar / Dejar pendiente / Guardar como excepción) — es una pieza de UI aparte, no bloquea el resto de REQ-002.

### REQ-002 — bug real encontrado por Javier y arreglado: coincidencia contra el texto marcado, no contra el nombre
Caso real: el nombre del Emisor no siempre es texto extraíble (a veces es un logo/imagen). La única forma de identificar el documento con certeza es marcar otro campo confiable (ej. el RUT) mientras se sigue escribiendo el nombre real a mano para mostrar/organizar. La coincidencia automática original comparaba el texto extraído contra el NOMBRE tipeado, no contra lo que esa coordenada realmente contiene — nunca iba a reconocer nada si el campo marcado no era literalmente el nombre.

Fix: se agregó `Marca.TextoReferencia` (texto extraído en el momento de crear la marca); la coincidencia automática compara contra este valor, no contra el nombre de la entidad (con fallback al comportamiento viejo si una marca no tiene el dato, para no romper configuraciones creadas antes del fix — igual hay que re-vincular esas para que el fix las alcance). Incluye migración mínima de esquema (`ALTER TABLE ... ADD COLUMN` si falta, sin perder datos). Ver `Servicios/CoincidenciaAutomaticaService.cs` y memoria `feedback-coincidencia-contra-texto-marcado`.

### REQ-004 — qué se construyó
- **"Administrar clasificaciones"** (botón en `MainWindow`): lista todas las configuraciones (Emisor, Tipo, carpeta destino, disponibilidad), buscador con autocompletado (`Vistas/AdministrarClasificacionesWindow`).
- **"Revisar disponibilidad"**: chequea `Directory.Exists` de cada carpeta destino, solo al abrir o al tocar el botón (sin polling en background, como pide SPEC.md).
- **Editar (reutiliza el asistente paso a paso)**: feedback de Javier — prefiere revisar/corregir desde el mismo asistente de identificación, no un formulario aparte. "Editar" abre `IdentificarDocumentoWindow` en modo edición: si la configuración tiene más de un patrón (ej. guía vieja/nueva de un proveedor), primero se elige cuál con `Vistas/ElegirPatronWindow` (mostrando el texto marcado como Emisor de cada uno); como el documento original ya se movió, se pide un PDF de ejemplo del mismo diseño para revisar/corregir las marcas sobre él, sin tocarlo ni moverlo. Emisor/Tipo quedan de solo lectura. "Guardar cambios" usa `ActualizarPatron` + `ActualizarDestino` — nunca reclasifica archivos ya guardados.
- **Exportar/respaldar**: vuelca todas las configuraciones+patrones a un JSON, en un botón secundario de la ventana (no en la pantalla principal).
- Tests nuevos: `ActualizarDestino`, `ActualizarPatron`, `ObtenerTodas`.

### REQ-002/REQ-003 — bug real: parser de fechas y variante de carpeta año+yyyyMM
Javier encontró que `"30-ABR-2026"` (día-mes en letras abreviado-año) no lo reconocía el parseo de fecha por defecto. Se agregó `Servicios/FechaExtraidaService.cs`: prueba varios formatos numéricos explícitos, fechas con nombre de mes en español (tabla propia de meses/abreviaturas, no depende de la configuración regional del sistema) en dos formas ("30 de abril de 2026" y "30-ABR-2026"), y como último recurso el parseo general de .NET. Se usa tanto en el asistente como en el guardado automático.

También se agregó la variante de patrón de carpeta `yyyy\yyyyMM` (año en una subcarpeta, mes concatenado con el año dentro — ej. `2026\202601`), pedida por Javier por ser una convención muy usada; se detecta sola y aparece como opción al elegir "por año y mes". 54 tests en total.

### REQ-005 — qué se construyó
- **Cerrar no apaga**: la X de `MainWindow` la minimiza a la bandeja en vez de cerrar Archivero (`OnClosing` cancela y hace `Hide()`).
- **Ícono de bandeja** (`Servicios/TrayIconService.cs`, dibujado en código con `System.Drawing`, sin archivo `.ico`): parpadea azul↔naranja mientras haya algo en pendientes, sin popups ni sonidos; deja de parpadear cuando la lista queda vacía.
- **Instancia única**: `Mutex` con nombre fijo en `App.xaml.cs`; si ya hay una instancia corriendo, la nueva apertura no crea nada — usa `FindWindow`/`SetForegroundWindow` (Win32, vía P/Invoke) para enfocar la ventana existente (aunque esté oculta en la bandeja) y se cierra sola. Verificado desde la sesión: abrir el ejecutable dos veces deja una sola instancia corriendo.
- **Aviso si la carpeta observada desaparece**: se engancha al evento `Error` del `FileSystemWatcher` (se dispara si el directorio se borra/mueve) y avisa en vez de fallar en silencio.
- ✅ **Probado por Javier**: lo visual (parpadeo, minimizar/restaurar, "Salir") funciona.

### REQ-005 — bug real: pendientes que no se limpiaban al sacar el archivo de la carpeta
Javier sacó un archivo de la carpeta observada a mano y siguió apareciendo en "Pendientes por reconocer" — Archivero solo escuchaba cuando aparecía un archivo nuevo (`FileSystemWatcher.Created`), nunca cuando desaparecía uno. Se agregó:
- En vivo: manejo de `Deleted`/`Renamed` del `FileSystemWatcher` para sacar el archivo de Pendientes (y cualquier Borrador huérfano) apenas desaparece.
- Al arrancar: `ReconciliarPendientesConDisco` revisa cada pendiente guardado contra el disco, por si el archivo se sacó mientras Archivero no estaba corriendo (el watcher en vivo no se hubiera enterado).

Probado a mano en los dos casos (borrar con la app corriendo, y con la app cerrada) — en ambos el pendiente fantasma desaparece solo. 54 tests en total (sin tests nuevos para esto — es comportamiento de integración con el sistema de archivos real, verificado a mano).

### REQ-002 — completado: las 4 opciones de nombre duplicado
`Vistas/ResolverDuplicadoWindow`: Revisar lado a lado (renderiza la primera página de ambos documentos), Reemplazar, Dejar pendiente, Guardar en otra ubicación como excepción — en el orden que pide SPEC.md. Se ofrece tanto al reabrir un pendiente marcado como "Duplicado" (nuevo campo `Pendientes.Motivo`, para saber qué pantalla abrir sin tener que re-identificar) como desde el propio asistente si el guardado choca con un duplicado ahí mismo. `ClasificadorService.CalcularRutaDestino` separa el cálculo de la ruta final (sin tocar el disco) para poder recalcularla al reabrir un pendiente. 63 tests en total.

### Empaquetado — primer ejecutable autocontenido real
`dotnet publish` con `--self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true` (el último flag hace falta para que la librería nativa de PDFium quede embebida en el único .exe). Comando completo documentado en `GIT.md`. Resultado: un `.exe` de ~160MB que corre sin tener .NET instalado — probado a mano desde la sesión. No se tocó `Archivero.csproj` con `RuntimeIdentifier`/`SelfContained` fijos, para no ensuciar el ciclo normal de `dotnet build`/`dotnet run` durante el desarrollo. El `.exe` nunca se sube al repo (supera el límite de tamaño de archivo de GitHub) — `publish/` está en `.gitignore`.

### LICENSE + ícono único y fijo
LICENSE (MIT, Javier Valdebenito, 2026). Ícono (`src/Archivero/Recursos/icono.ico`, varias resoluciones) con el mismo diseño que ya usaba el ícono de bandeja en su estado normal (círculo azul, "A" blanca — `Servicios/TrayIconService.cs`), aplicado como ícono del `.exe` (`<ApplicationIcon>`) y de todas las ventanas (`Style` de `Window` en `App.xaml`). Verificado extrayendo el ícono del `.exe` compilado y viéndolo.

### Nota sobre la sincronización automática del vault
Hay un proceso en background (mencionado en `AGENTS.md`) que hace commits automáticos ("Sync automatico: ...") a este repo con la identidad de Javier, incluso a mitad de una sesión de trabajo. La mayoría de las veces es inofensivo (solo hace que aparezca un commit intermedio con código a medio terminar en el historial), pero **una vez alcanzó a comitear localmente el ejecutable de 161MB de `publish/`** en una rama que todavía no tenía la regla de `.gitignore` que lo excluye (se creó antes de mergear la rama de empaquetado). Se detectó y se deshizo (`git reset`) antes de pushear — nunca llegó a GitHub, pero pudo haber roto el push (GitHub rechaza archivos de más de 100MB). Ver memoria `project-vault-auto-sync`: antes de pushear una rama nueva, conviene revisar si hay un commit "Sync automatico" con algo grande adentro.

## Decisiones abiertas / dudas para el usuario
- Nombres finales de interfaz pendientes (no bloquean el desarrollo) — ver "Open issues" de `SPEC.md`: la sección "pendientes por reconocer", la sección "configuraciones/identificaciones/identidades", el botón de revisar/actualizar disponibilidad, la opción de "vincular a configuración existente".

Ya resueltas (quedan solo como referencia histórica):
- Licencia de código: MIT, confirmada una vez verificado que Docnet.Core/PDFium, SQLite y .NET son todas permisivas y compatibles entre sí. Ver `LICENSE`.
- "Personalización visual liviana": decidida en contra el 2026-09-13 — Archivero no tiene ninguna opción de personalización. Ícono único y fijo (el mismo diseño ya usado en la bandeja del sistema). Ver Boundaries de `SPEC.md`.

## Riesgos o cosas frágiles a tener en cuenta
- El reconocimiento automático de Emisor/Tipo depende de patrones de coordenadas marcados a mano — un cambio de diseño en un documento de un proveedor puede romper un patrón existente (ya contemplado en REQ-002/REQ-003, pero es el punto más frágil del sistema).
