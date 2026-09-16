Estado actual: En piloto desde 2026-09-13 — pendiente de revisión.

Última actualización del .exe: 2026-09-16, con `preguntas/Caso-6.md` (tres ajustes de uso real: "Pendientes de distribuir" ahora actualiza en vivo y tiene botón "Recargar"; "Guardados recientes" persiste hasta los últimos 20; el tipo de organización se puede volver a elegir sin cancelar el asistente) — ver `ESTADO.md`. Caso-1 a Caso-6 están todos cerrados.

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
