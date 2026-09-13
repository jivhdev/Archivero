# Archivero — contexto para agentes de IA

> Generado por el Método JA a partir de `SPEC.md`. La sección "Reglas universales"
> no se edita: aplica siempre. Todo lo demás sale de las Fases 3 y 7 de este
> proyecto — no son placeholders genéricos, ya están completos.

## Reglas universales (aplican siempre, en cualquier proyecto nacido del Método JA)

### Especificación y alcance
1. No decidas, no asumas, no completes por cuenta propia. Si algo no está en `SPEC.md`, detente y decilo — no lo resuelvas con tu propio criterio.
2. No implementes nada que no esté en la sección "Requirements" de `SPEC.md`.
3. Respetá la sección "Boundaries" de `SPEC.md` al pie de la letra: es una lista de lo que NO hay que tocar ni agregar.
4. Todo requerimiento funcional tiene un escenario Given/When/Then en `SPEC.md`. Antes de marcar algo como "hecho", verificá contra ese escenario, no contra tu propio criterio de qué significa "funciona".
5. Todo requerimiento no funcional tiene una medida numérica en `SPEC.md`. Si no se alcanza, no está terminado, aunque el código corra sin errores.
6. Ante cualquier ambigüedad, la pregunta correcta es "¿qué dice SPEC.md?", nunca "¿qué es lo más razonable acá?".
7. Si hace falta una decisión que cambia el alcance o el comportamiento descrito en `SPEC.md`, no la tomes en este repositorio — seguí el protocolo de "Caso-N" de `LEEME.md`.

### Límites
- Nunca leer, mover, sobrescribir ni borrar archivos fuera de la carpeta de este proyecto (`C:\Archivero`), salvo las dos excepciones explícitas de la primera sesión descritas en `LEEME.md` (agregar una línea a `otros-repos.txt` y actualizar `Registro-de-Proyectos.md`, ambos dentro de `C:\MQD`).
- Ningún dato de negocio real (documentos del usuario) se borra físicamente sin que `SPEC.md` lo pida explícitamente.
- Antes de una acción destructiva o difícil de revertir (`git reset --hard`, `push --force`, borrar ramas, `rm -rf`, sobrescribir cambios sin commitear), parar y confirmar con el usuario.

### Control de versiones y GitHub
- Repositorio git desde el primer archivo, con remoto en GitHub.
- Commits frecuentes, formato `tipo: descripción breve` (`feat`, `fix`, `refactor`, `test`, `docs`, `chore`).
- Una rama por tarea; nunca dos tareas distintas en la misma rama.
- Autorización permanente para `git push` normal (sin `--force`) del trabajo ya commiteado al remoto ya configurado, sin pedir permiso cada vez. Crear un remoto nuevo, forzar un push, o reescribir historia sigue necesitando confirmación explícita.
- Ver `GIT.md` para los comandos de uso diario.

### Licencias y dependencias
- Toda librería/tecnología nueva debe ser gratuita de usar y distribuir — ver la restricción de licencia en "Technical spec" de `SPEC.md`. La licencia final de Archivero todavía no está cerrada (ver "Open issues" de `SPEC.md`) — no elegir una y darla por definitiva sin que el usuario lo confirme.
- Si una librería es GPL/AGPL, avisar antes de agregarla.

### Metodología de desarrollo
- Arquitectura por defecto: monolito modular simple, salvo que `SPEC.md` diga otra cosa explícitamente.
- Cada módulo resuelve una función clara. Si un módulo empieza a hacer dos trabajos distintos, se separa en dos.
- El trabajo avanza por requerimiento: uno de `SPEC.md` por vez, entrega acotada — nunca "construí todo".
- Ajustar algo nuevo sobre una parte que ya funciona no debe obligar a modificar esa parte ya verificada. Relacionado no es lo mismo que acoplado.
- Verificación real obligatoria: ninguna tarea se da por terminada solo porque los tests automáticos pasan. Corré la app real y probá el flujo a mano (o dá los pasos exactos para que Javier lo pruebe), y dejá tests automáticos para lógica que se pueda romper en silencio (extracción de campos, detección de formato/patrón, clasificación).
- Para probar durante el desarrollo, corré la app directo (no empaquetar como ejecutable autocontenido en cada ciclo de prueba — demasiado lento para iterar). Empaquetar recién en hitos reales.

### Comunicación del agente
- Español, directo, breve.
- Sin comentarios de código que expliquen el "qué"; solo los que expliquen un "por qué" no obvio.
- Cambios acotados al pedido — sin refactors ni features no solicitados.

## Qué es esto
Archivero automatiza la tarea repetitiva de identificar y guardar documentos PDF en su carpeta correspondiente: se configura cada tipo de documento una sola vez, y de ahí en adelante se reconoce y guarda solo. Pensado para Javier y su equipo de oficina, pero construido para funcionar en cualquier contexto similar. Ver el "Intent" de `SPEC.md`.

## Arquitectura de este proyecto
- **Plataforma:** .NET (C#) con WPF. Solo Windows. Empaquetado como ejecutable autocontenido.
- **Almacenamiento:** SQLite (un archivo local único) para configuraciones, patrones de reconocimiento, y entidades conocidas (autocompletado).
- **PDF:** librería .NET basada en PDFium, embebida en la propia ventana — nunca abre una app externa.
- Detalle completo, con las razones de cada elección: `SPEC.md`, sección "Technical spec".

## Base de datos
SQLite. La base real del usuario nunca se sube al repositorio (ver `.gitignore` y RNF-4 de `SPEC.md`) — el repo se distribuye con una base vacía.

## Convenciones específicas
- C# siguiendo las convenciones estándar de .NET (PascalCase para tipos/métodos públicos, camelCase para variables locales).
- Un módulo/clase por función clara del flujo (ver "Metodología de desarrollo" arriba) — por ejemplo, el reconocimiento de Emisor/Tipo, el manejo de formato/patrón de carpetas, y el guardado seguro del archivo (copiar-verificar-borrar) son responsabilidades separadas, no una sola clase gigante.
- Tests automáticos para: extracción de campos por coordenadas, detección y derivación de formato/patrón de carpetas, y la lógica de coincidencia exacta (Emisor+Tipo). No hace falta testear la interfaz gráfica en sí.

## Dónde estamos
Ver `ESTADO.md` para el detalle de la última sesión y el siguiente paso.

## Dónde vive la especificación (no editar esta sección)
La fuente de verdad de este proyecto es el vault de Obsidian del Método JA, en:
`C:\MQD\01-Proyectos\Archivero\02-Especificacion-Final.md`

`SPEC.md` es una copia de trabajo de la sección "SPEC exportable" de ese archivo. Si algo acá parece faltar, contradecirse, o necesitar una decisión nueva de alcance: no lo resuelvas en este repo. Seguí el protocolo de "Caso-N" de `LEEME.md`.
