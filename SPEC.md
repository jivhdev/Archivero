# SPEC — Archivero

## Intent
Automatizar la tarea repetitiva de identificar y guardar documentos PDF en su carpeta correspondiente: se configura cada tipo de documento una sola vez, y de ahí en adelante Archivero los reconoce y guarda solo — pensado para Javier y su equipo de oficina, pero construido para funcionar en cualquier contexto similar, no solo el suyo.

## Requirements

### REQ-001 — Primera configuración (elegir/crear la carpeta observada)
La primera vez que se abre Archivero, antes de cualquier otra función, el programa guía al usuario para crear la carpeta que va a observar: sugiere el nombre "Archivero" (el usuario puede cambiarlo) y una ubicación por defecto (ej. Escritorio), que el usuario puede aceptar o cambiar. Si ya existe una carpeta con ese nombre en esa ubicación, Archivero pide otro nombre u otra ubicación en vez de reutilizarla a ciegas. Una vez creada, esa carpeta queda marcada como la carpeta observada para todo lo demás.

**Acceptance criteria:**
- Escenario: Usuario configura Archivero por primera vez
  Given: Es la primera vez que se abre Archivero, sin carpeta observada configurada
  When: El usuario elige (o acepta la sugerida) una ubicación y un nombre para la carpeta
  Then: Archivero crea esa carpeta y la marca como la carpeta observada, lista para el resto de los requerimientos

### REQ-002 — Guardar automáticamente un documento ya identificado
Cuando aparece un archivo PDF nuevo en la carpeta observada, Archivero extrae el Emisor Documento y el Tipo Documento (usando los patrones de reconocimiento ya guardados) y los compara contra las configuraciones existentes. Si hay una coincidencia exacta con una configuración, Archivero renombra el archivo (solo si esa configuración indica renombrar, extrayendo el campo correspondiente marcado por coordenadas) y lo guarda en la carpeta de destino de esa configuración, derivando la subcarpeta de fecha según el formato y patrón guardados. Muestra un mensaje confirmando dónde quedó guardado. Los archivos se procesan de a uno, en el orden en que llegan, nunca en paralelo. Al guardar, Archivero copia primero al destino, verifica que la copia es idéntica, y recién ahí borra el original — nunca deja un archivo a medio mover.

No hay coincidencia exacta ⇒ el documento pasa al flujo de REQ-003 (Identificar documento nuevo).

El archivo no es un PDF con texto plano extraíble (ej. una imagen escaneada) ⇒ Archivero no lo procesa; el usuario debe guardarlo a mano, pero el buscador con autocompletado de Emisor/Tipo (REQ-004) debe estar disponible para ayudarlo a elegir la carpeta.

El dato extraído de una coordenada (Emisor, Tipo, o el campo del nombre de archivo) no tiene una forma válida (vacío, o no parece un dato real — ej. el proveedor cambió su plantilla) ⇒ Archivero no lo guarda con un valor dudoso; lo manda a "pendientes por reconocer" para revisión manual.

El nombre de archivo resultante ya existe en la carpeta de destino (posible duplicado) ⇒ Archivero no sobreescribe automáticamente. Ofrece, en este orden: (1) Revisar — muestra el documento existente y el nuevo lado a lado para comparar; (2) Reemplazar; (3) Dejar pendiente; (4) Guardar en otra ubicación como excepción, manteniendo el mismo nombre/formato de identificación.

La carpeta de destino no está disponible (ej. servidor desconectado) ⇒ Archivero no falla en silencio: deja el archivo pendiente y avisa. No reintenta automáticamente en segundo plano (para no sobrecargar un recurso compartido) — se resuelve cuando el usuario abre Archivero (chequeo al inicio) o usa el botón de revisar disponibilidad (REQ-004). Para carpetas locales o de Google Drive esto no aplica, siempre están disponibles localmente.

**Acceptance criteria:**
- Escenario: Documento con tipo ya identificado se guarda automáticamente
  Given: Existe una configuración guardada para el Emisor y Tipo de un documento
  When: El usuario descarga un archivo PDF de ese mismo Emisor y Tipo en la carpeta observada
  Then: Archivero lo guarda en su ubicación definitiva (renombrado si corresponde) y muestra un mensaje confirmando dónde quedó guardado
- Escenario: Documento con valor extraído inválido no se guarda solo
  Given: Existe una configuración guardada para un Emisor+Tipo, con un patrón de reconocimiento de coordenadas
  When: Llega un documento de ese Emisor+Tipo pero el valor extraído de una coordenada no tiene forma válida
  Then: Archivero no lo guarda automáticamente; lo envía a "pendientes por reconocer"
- Escenario: Nombre de archivo duplicado en destino
  Given: Un archivo con el mismo nombre resultante ya existe en la carpeta de destino
  When: Archivero está por guardar el nuevo archivo ahí
  Then: Archivero ofrece revisar, reemplazar, dejar pendiente, o guardar como excepción — nunca sobreescribe solo
- Escenario: Carpeta de destino no disponible
  Given: La carpeta de destino configurada es un servidor que está desconectado
  When: Archivero intenta guardar un archivo ahí
  Then: Archivero deja el archivo pendiente y avisa, sin reintentar automáticamente en segundo plano

### REQ-003 — Identificar documento nuevo (primera vez para un tipo de documento)
Cuando un documento no coincide con ninguna configuración existente, aparece en "pendientes por reconocer". Al seleccionarlo, Archivero abre el PDF embebido en su propia ventana (nunca en una app externa) para que el usuario lo identifique: marca sobre el PDF dónde aparece el Emisor Documento y el Tipo Documento, y además los tipea (con autocompletado de entidades ya conocidas) — pudiendo normalizar/vincular el valor a un Emisor o Tipo ya existente en vez de crear uno nuevo. En vez de crear una configuración nueva, el usuario también puede elegir vincular este documento a una configuración ya existente como un patrón de reconocimiento adicional (útil cuando un proveedor cambia el diseño de su documento). El usuario elige la carpeta de destino manualmente; Archivero intenta detectar el formato (por año, por año y mes, o directo en carpeta) y el patrón de esa carpeta automáticamente — el caso esperado, dado que Archivero automatiza un proceso que ya existe. El usuario confirma o corrige el formato/patrón detectado, marcando sobre el PDF dónde extraer la fecha (salvo que sea "directo en carpeta"). El usuario define el nombre con que se guardará el archivo (extrayendo un campo marcado por coordenadas, o manteniendo el nombre original). El usuario completa todos los campos necesarios para identificar el documento con certeza — no hay guardado posible sin ellos. Archivero guarda la configuración de forma permanente y clasifica el archivo actual según ella, mostrando un mensaje de confirmación.

Archivero no detecta formato/patrón (carpeta nueva o entorno que arranca de cero) ⇒ el usuario lo define todo manualmente, incluyendo cómo se escribe cada componente (ej. el año en números o de otra forma), pudiendo elegir entre patrones ya usados en otras configuraciones o crear uno nuevo.

Faltan campos necesarios para identificar el documento con certeza ⇒ Archivero no obliga a terminar ahora. Ofrece posponer (guarda lo ingresado en estado incompleto, se retoma después) o cancelar (con confirmación previa, para no perder trabajo sin querer).

El usuario cierra el programa a la mitad del asistente sin elegir posponer ni cancelar ⇒ se trata igual que "posponer": no se pierde nada, reaparece en "pendientes por reconocer".

La herramienta de marcado sobre el PDF es la misma para cualquier dato marcado (fecha, nombre de archivo, Emisor, Tipo) — no hace falta una ventana distinta, porque el asistente es estrictamente paso a paso.

**Acceptance criteria:**
- Escenario: Usuario identifica un tipo de documento por primera vez
  Given: Llegó un documento cuyo Emisor+Tipo no coincide con ninguna configuración existente
  When: El usuario completa Emisor, Tipo, carpeta de destino, formato/patrón (detectado o manual), y nombre de archivo, con todos los campos necesarios
  Then: Archivero guarda esa configuración de forma permanente, clasifica y guarda el archivo actual según ella, y los futuros documentos del mismo Emisor+Tipo se guardan solos
- Escenario: Usuario vincula un documento a una configuración existente en vez de crear una nueva
  Given: El usuario está identificando un documento que en realidad corresponde a una configuración ya existente (ej. el proveedor actualizó el diseño)
  When: El usuario elige vincularlo a esa configuración existente en vez de crear una nueva
  Then: El patrón de reconocimiento del documento actual se suma a esa configuración, y de ahí en adelante Archivero reconoce documentos que coincidan con cualquiera de los patrones asociados
- Escenario: Usuario pospone una identificación incompleta
  Given: El usuario está identificando un documento nuevo pero le faltan campos necesarios
  When: El usuario elige posponer (o cierra el programa sin elegir nada)
  Then: Lo ya ingresado no se pierde, y el documento reaparece en "pendientes por reconocer"

### REQ-004 — Administrar clasificaciones existentes
El usuario puede ver y administrar todas las clasificaciones ya hechas, organizadas por Emisor Documento y Tipo Documento, con un buscador con autocompletado para filtrarlas. Puede seleccionar una para editarla, siguiendo los mismos pasos de REQ-003. Un cambio a una configuración existente aplica solo de ahí en adelante — nunca reclasifica archivos ya guardados antes. Junto a cada clasificación, se muestra si su carpeta de destino está disponible ahora mismo (chequeado al abrir Archivero, o con un botón de revisar/actualizar disponibilidad) — sin chequeo automático en segundo plano. Archivero permite exportar/respaldar todas las configuraciones a un archivo, en un lugar secundario de la interfaz (no en la pantalla principal).

No hay ninguna clasificación guardada todavía ⇒ Archivero muestra la sección vacía.

Alguna carpeta de destino no está disponible ⇒ se marca visualmente en el panel.

**Acceptance criteria:**
- Escenario: Usuario edita una clasificación existente
  Given: Existe una configuración guardada para un Emisor+Tipo
  When: El usuario la abre desde la sección de administración y modifica alguno de sus campos
  Then: Archivero guarda los cambios y los aplica solo a los próximos documentos de ese mismo Emisor+Tipo, nunca a los ya guardados

### REQ-005 — Carcasa e interfaz general
Archivero es un programa normal: doble click para abrirlo, con una ventana principal que muestra sus secciones visibles (ej. una barra lateral simple con "Pendientes por reconocer" y "Administrar clasificaciones"). Cerrar la ventana no apaga Archivero — lo minimiza a la bandeja del sistema, donde sigue observando la carpeta en silencio. El ícono de la bandeja parpadea o cambia visualmente cuando hay algo pendiente por reconocer, sin ventanas emergentes ni sonidos. Para volver a la ventana principal: click en el ícono. Para apagar Archivero de verdad: una opción explícita de "Salir". Si el usuario abre Archivero mientras ya hay una instancia corriendo, la segunda apertura solo enfoca la ventana de la primera. Al abrir Archivero (no arranca solo con Windows), además de observar la carpeta hacia adelante, revisa una vez qué archivos ya estaban ahí sin procesar.

Si la carpeta observada deja de existir (se borra o se mueve) mientras Archivero está corriendo, avisa al usuario en vez de fallar en silencio.

**Acceptance criteria:**
- Escenario: Cerrar la ventana no apaga el programa
  Given: Archivero está corriendo con su ventana principal abierta
  When: El usuario cierra la ventana (la X)
  Then: Archivero se minimiza a la bandeja del sistema y sigue observando la carpeta, sin apagarse
- Escenario: Aviso sutil de pendientes
  Given: Archivero está corriendo minimizado en la bandeja
  When: Aparece un documento que no se pudo identificar automáticamente
  Then: El ícono de la bandeja parpadea o cambia visualmente, sin ventana emergente ni sonido

## Technical spec

- **Plataforma:** .NET (C#) con WPF, empaquetado como ejecutable de Windows autocontenido. Solo Windows (confirmado — no requiere Mac/Linux).
- **Almacenamiento:** SQLite, un archivo local único, para las configuraciones, los patrones de reconocimiento, y las entidades conocidas (para el autocompletado).
- **Lectura/renderizado de PDF:** librería .NET basada en PDFium, embebida en la propia ventana de Archivero — nunca abre Chrome ni ninguna app externa.
- **Licencia del código:** MIT — decidida una vez confirmado que todas las dependencias reales (Docnet.Core/PDFium, SQLite, .NET) son permisivas y no imponen restricciones entre sí. Permite uso, modificación y redistribución libres, incluso comercial por terceros — el uso por parte de cualquier usuario final siempre es gratis.
- **Publicación del código:** open source, pensado para publicarse en GitHub. La base de datos real de configuraciones nunca se incluye en el repositorio público (excluida vía `.gitignore`); la app se distribuye con una base de datos vacía.
- **Vigilancia de carpetas:** tiempo real para carpetas locales o de Google Drive (siempre disponibles localmente); sin polling automático en segundo plano para carpetas de red/servidor — solo chequeo al abrir Archivero o a pedido explícito del usuario, para no sobrecargar un recurso compartido.

## Non-functional requirements

```
RNF-1 — Rendimiento
Atributo de calidad: Rendimiento
Fuente del estímulo: Sistema externo (llega un archivo nuevo a la carpeta observada)
Estímulo: Un archivo PDF con un tipo ya identificado se descarga en la carpeta observada
Artefacto afectado: Módulo de detección y clasificación automática
Entorno: Carga normal (uso diario en una PC de gama baja)
Respuesta esperada: Archivero reconoce, clasifica y guarda el archivo sin intervención del usuario
Medida de la respuesta: En 1-2 segundos desde que el archivo termina de descargarse
```

```
RNF-2 — Disponibilidad
Atributo de calidad: Disponibilidad
Fuente del estímulo: Componente interno (un archivo dañado o ilegible)
Estímulo: Llega un PDF corrupto o que Archivero no puede leer
Artefacto afectado: Módulo de lectura de PDF
Entorno: Falla (archivo malformado)
Respuesta esperada: Archivero deja de intentar leer ese archivo puntual, sin tocarlo ni moverlo, y sigue observando la carpeta con normalidad para los demás
Medida de la respuesta: Máximo 5 segundos por archivo antes de descartar el intento
```

```
RNF-3 — Seguridad (integridad, nunca perder un archivo)
Atributo de calidad: Seguridad
Fuente del estímulo: Cualquiera (usuario, error interno, o archivo problemático)
Estímulo: Cualquier archivo que Archivero procese, reconocido, no reconocido, o con error de lectura
Artefacto afectado: Todo el flujo de clasificación y guardado
Entorno: Carga normal y de falla
Respuesta esperada: El archivo original nunca se borra ni se pierde — si no se puede clasificar con certeza, queda intacto y visible
Medida de la respuesta: 0 archivos perdidos o borrados por el programa, siempre
```

```
RNF-4 — Seguridad (no filtrar datos reales al repo público)
Atributo de calidad: Seguridad
Fuente del estímulo: Usuario (al publicar/actualizar el código en GitHub)
Estímulo: Se sube una nueva versión del código fuente al repositorio público
Artefacto afectado: Repositorio de código en GitHub
Entorno: Publicación/actualización del repositorio
Respuesta esperada: El repositorio público nunca incluye la base de datos real de configuraciones (carpetas, proveedores, clientes reales)
Medida de la respuesta: 0 archivos de configuración real incluidos en el repositorio público
```

## Boundaries (qué NO construir)
- Archivero solo identifica el tipo de documento que llega y lo guarda donde corresponde — no hace nada más que eso.
- No sincroniza nada a la nube por su cuenta — eso lo hace Google Drive (u otro servicio) por su lado; Archivero solo escribe localmente.
- No revisa actualizaciones por internet.
- No reclasifica ni mueve archivos ya guardados cuando se edita una configuración — el cambio aplica solo hacia adelante.
- No vigila carpetas remotas en tiempo real ni con polling automático — solo al abrir el programa o a pedido del usuario.
- No procesa nada que no sea un PDF con texto plano extraíble — eso se guarda a mano.
- No modifica el contenido de los documentos — solo los renombra y/o los mueve.
- No es un lector de PDF de propósito general, más allá de lo necesario para identificar y marcar campos.
- No tiene ninguna opción de personalización visual — es un programa sin personalización *(decisión revisada el 2026-09-13: la Fase 1 había registrado un deseo de "personalización liviana"; Javier decidió que no, una vez viendo el programa funcionando, que es mejor no tenerla)*. Ícono/logo único y fijo: el mismo diseño ya usado en el ícono de la bandeja del sistema en su estado normal (una "A" blanca sobre fondo azul, ver `Servicios/TrayIconService.cs`), aplicado también como ícono de la aplicación/ejecutable.

## Open issues
- Nombres finales pendientes de definir (no bloquean la construcción, son solo texto de interfaz): la sección "pendientes por reconocer", la sección "configuraciones/identificaciones/identidades", el botón de revisar/actualizar disponibilidad, la opción de "vincular a configuración existente".
