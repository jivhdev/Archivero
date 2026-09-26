Estado actual: En piloto desde 2026-09-13 — pendiente de revisión.

Última actualización del .exe: 2026-09-26, con `preguntas/Caso-9.md` (validación de entradas/rutas contra path traversal y nombres inválidos de Windows, más un log de auditoría de texto plano con rotación) — ver `ESTADO.md`. Caso-1 a Caso-6, Caso-8 y Caso-9 están cerrados; `preguntas/Caso-7.md` sigue pendiente, todavía sin empezar.

## Qué contiene esta carpeta
- `Archivero.exe`: ejecutable autocontenido de Windows (no requiere .NET instalado). Se abre con doble click.
- `Archivero.pdb`: símbolos de depuración (no hace falta para ejecutarlo).
- `LICENSE`: licencia de terceros de PDFium (Docnet.Core la incluye automáticamente al publicar).

El `.exe` pesa unos 160MB y nunca se sube al repositorio (ver `.gitignore`) — supera el límite de tamaño de archivo de GitHub. Solo este archivo (`ESTADO-DISTRIBUCION.md`) queda versionado.

## Cómo se regenera
Desde la raíz del repo:

```
dotnet publish src/Archivero/Archivero.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o Distribucion
```

Ver `GIT.md` para más detalle sobre este comando.
