# Estado del proyecto

> Se actualiza al final de cada sesión. Es lo tercero que hay que leer (después de AGENTS.md y SPEC.md) para saber dónde quedamos.

## Última sesión
- Fecha: 2026-09-14
- Qué se hizo: `preguntas/Caso-1.md` (5 hallazgos reales del piloto, resueltos en el vault — ninguno contradice `SPEC.md`, la amplían o corrigen bugs reales) implementado completo, un punto a la vez, cada uno en su rama/PR, todos mergeados a `main` sin conflictos. Ver el detalle de cada uno en la sección "Caso-1" más abajo. 75 tests automáticos en total, todos verdes, en el `main` combinado.

## Siguiente paso
- **Todo lo pedido en `Caso-1.md` está implementado y en `main`, pero todavía sin la verificación real a mano de Javier** (AGENTS.md pide correr la app real y probar el flujo, no solo confiar en los tests automáticos — el agente no tiene forma de hacer eso por su cuenta en una app de escritorio). Antes de dar Caso-1 por cerrado del todo, probar a mano cada punto — los pasos sugeridos están en la descripción de cada PR (#13 a #17) y resumidos abajo.
- El `.exe` de `Distribucion/` **todavía no se regeneró** con los cambios de hoy: había una instancia de Archivero corriendo (PID activo) que bloqueaba el archivo, y no se cerró sola por las dudas de que fuera algo que Javier tenía abierto a propósito. Regenerar con el comando de `GIT.md` (o pedírselo al agente) después de cerrar esa instancia.
- Fuera de esto, no queda nada pendiente conocido del alcance de `SPEC.md` + `Caso-1.md`. Sigue abierto, como antes, cerrar los "Open issues" que quedan (nombres finales de interfaz).

## Caso-1 — 5 correcciones/ampliaciones del piloto real (2026-09-14)
Ver `preguntas/Caso-1.md` para el texto completo de cada punto tal como lo trajo Javier del vault.

### Punto 2 — detección de formato de carpeta por evidencia real, nunca por adivinanza
El bug más grave reportado: Javier eligió la carpeta EXACTA de destino y Archivero igual creó una subcarpeta de año/mes adentro sin que nadie la pidiera. `FormatoCarpetaService.Detectar` se reescribió para comparar los nombres de subcarpetas ya existentes entre sí (evidencia real) en vez de contra una lista cerrada de formatos completos: separa la parte literal fija de la parte que varía, y prueba si esa parte variable es consistente con un token de fecha conocido — generaliza a convenciones reales con texto alrededor (ej. `"Año 2024"`), pero sigue exigiendo evidencia real y nunca inventa una subdivisión sin ella. Salvaguarda agregada: un texto candidato a "literal fijo" nunca se acepta si por sí solo parece un valor de fecha válido (evita confundir, ej., el año que se repite en las subcarpetas de mes de una única carpeta de año con texto literal).

Además, se cerró el último párrafo del punto: cuando un documento nuevo coincide con una configuración ya confirmada pero la carpeta del período actual todavía no existe, Archivero ya no la crea sola en silencio — pasa a pendientes (motivo `PeriodoNuevo`) con una pantalla dedicada (`Vistas/CrearPeriodoWindow`) que ofrece crear la carpeta y guardar ahí, crear también por adelantado la del próximo período, o elegir una carpeta manualmente. El caso común (la carpeta del período ya existe) sigue siendo 100% automático y silencioso, sin cambios.

PR: [#13](https://github.com/jivhdev/Archivero/pull/13) (detección) y [#14](https://github.com/jivhdev/Archivero/pull/14) (período nuevo + punto 3, comparten pantalla).

### Punto 3 — preview obligatorio de carpeta anterior/actual/futura
En el asistente de identificación, desde el paso de Formato hasta Confirmar, un recuadro fijo muestra: la carpeta **anterior** (real, ya en disco, vía `FormatoCarpetaService.BuscarCarpetaAnteriorReal`), la carpeta **actual** (ruta y nombre exactos de cómo quedaría el documento de hoy, reusando `ClasificadorService.CalcularRutaDestino`), y la carpeta **futura** (la que se crearía en el próximo período, si aplica). Se actualiza solo al cambiar formato/patrón o al marcar la fecha, y tiene un botón para refrescarlo a mano. PR: [#14](https://github.com/jivhdev/Archivero/pull/14).

### Punto 1 — PDFs sin texto extraíble quedan visibles y configurables a mano
Antes desaparecían sin dejar rastro. Ahora aparecen en pendientes (motivo `SinTextoExtraible`) con una pantalla dedicada (`Vistas/IdentificarSinTextoWindow`): Emisor y Tipo se escriben a mano (no hay texto para marcar por coordenadas), la carpeta se elige como cualquier configuración normal, y la configuración resultante queda siempre en "Directo" y sin renombrar. Los próximos documentos del mismo Emisor+Tipo se enrutan solos vía un mecanismo de coincidencia aparte (`CoincidenciaAutomaticaService.BuscarConfiguracionSinTextoQueCoincide`, busca una configuración con un "patrón sin ninguna marca"); si hay más de una así, es ambiguo y queda pendiente en vez de adivinar. De paso se corrigió un bug real en `ConfiguracionDocumentoRepository.ObtenerPatrones`: usaba un INNER JOIN desde `Marcas`, así que un patrón sin ninguna marca desaparecía al leerlo de vuelta de la base (nunca se había notado porque hasta ahora todo patrón tenía al menos Emisor y Tipo). PR: [#15](https://github.com/jivhdev/Archivero/pull/15).

### Punto 4 — vista simple de un guardado, botón Atrás, y borrar configuración
"Guardados automáticamente" es interactivo: doble click abre una vista simple (`Vistas/VerGuardadoWindow`) con "Abrir ubicación" (explorador de Windows) y "Editar configuración" — esta última toma ese mismo archivo directamente (nunca pide buscar uno parecido), lo re-reconoce en su ubicación actual y lleva al asistente derecho al paso de elegir carpeta, sin repetir Emisor/Tipo. Se agregó un botón "Atrás" al asistente (se puede retroceder un paso, nunca saltar adelante). Se agregó borrar una configuración de documento completa desde "Administrar clasificaciones" (no existía) — nunca toca archivos ya guardados en disco. PR: [#16](https://github.com/jivhdev/Archivero/pull/16).

### Punto 5 — opción "abrir después de guardar"
Reemplaza el efecto de la apertura automática que hacía PDFCreator antes de que Archivero moviera el archivo casi al instante. Nueva columna `Configuraciones.AbrirDespuesDeGuardar` (con migración); checkbox en el último paso del asistente (nunca en el medio), arranca siempre destildado. Si está activo, al guardar automáticamente un documento de ese tipo se abre en el visor de PDF por defecto del sistema. Si no se puede abrir, no afecta el resultado del guardado. PR: [#17](https://github.com/jivhdev/Archivero/pull/17).

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
