# Leeme primero

Instrucción para pegar al empezar cualquier sesión con un agente de IA (Claude Code, OpenCode Go, Cursor, Aider, o el que sea):

> Lee LEEME.md y seguí sus instrucciones antes de programar.

El usuario que pegó esta carpeta en `C:\Archivero` **no va a tocar una terminal**. Todo el setup técnico (git, GitHub, registrar este repo en el vault) lo hace el agente, no el usuario. Los únicos dos momentos donde el usuario interviene son: cuando el agente le pregunta algo puntual durante el trabajo normal, y cuando hace falta volver al vault del Método JA a decidir algo de alcance (ver "Casos" más abajo).

## Primera sesión únicamente (si no existe todavía una carpeta `.git/` acá)
1. `git init`, `git add .`, primer commit (ver `GIT.md`).
2. Crear el repositorio remoto en GitHub y subir (con `gh` si está disponible; si no, pedirle al usuario la URL de un repo vacío ya creado — ver `GIT.md`).
3. Agregar `C:\Archivero` como una línea nueva en `C:\MQD\Scripts\otros-repos.txt` (el archivo ya existe con el formato explicado en un comentario — solo agregar la línea).
4. Actualizar la fila de Archivero en `C:\MQD\01-Proyectos\Registro-de-Proyectos.md` con la URL de GitHub una vez creada.
5. Crear la carpeta `preguntas/` vacía (para el protocolo de Casos, ver abajo).

## Cada sesión (incluida la primera, después de lo anterior)
1. Leé `AGENTS.md` completo — son las reglas fijas, aplican siempre.
2. Leé `SPEC.md` completo — es la especificación funcional y técnica de Archivero entero.
3. Leé `ESTADO.md` — qué se hizo la última sesión y cuál es el siguiente paso.
4. Revisá `preguntas/` por algún `Caso-N.md` que ya tenga una sección "## Respuesta" — son decisiones que Javier ya resolvió en el vault de MQD y trajo de vuelta. Tratalas como parte del pedido de esta sesión.
5. Antes de tocar código, resumí en pocas líneas qué vas a hacer y en qué orden, referenciando el requerimiento exacto de `SPEC.md` (ej. "REQ-002"). Un requerimiento por vez — nunca "construí todo Archivero".

## Al cerrar una sesión (o un bloque grande de trabajo)
1. Actualizá `ESTADO.md`: qué quedó hecho y cuál es el siguiente paso concreto.
2. Comiteá con un mensaje claro y subí a GitHub (ver `GIT.md`) — no hace falta esperar a la sincronización automática del vault (que también cubre este repo una vez agregado a `otros-repos.txt`).

## Si hace falta una decisión que no está en SPEC.md (protocolo de "Caso-N")
No la tomes por tu cuenta. Creá un archivo nuevo `preguntas/Caso-N.md` (siguiente número disponible, secuencial — Caso-1, Caso-2, etc.) con la pregunta exacta y el contexto mínimo necesario para entenderla sin vos. Avisale a Javier: *"Necesito resolver el Caso-N en el vault de MQD antes de seguir con esto — podés seguir trabajando en otra parte mientras tanto si hay algo independiente."* Javier lo lleva al Método JA, lo resuelve ahí, y te trae de vuelta un `Caso-N.md` con la respuesta ya escrita — nunca respondas una pregunta de alcance solo porque "suena razonable", ni siquiera si parece obvia.
