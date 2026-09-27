# Branding

Put your application icon here as **`app.ico`**. It is ignored by git, so each
person or organisation can use their own.

When `branding/app.ico` exists, every build (including `dotnet run` and
`publish.cmd`) uses it as:

- the icon of `NiceMail.exe` (Explorer, desktop shortcuts, Start menu), and
- the main window's icon (title bar, taskbar, Alt+Tab).

If it's absent, the build still works and uses the default icons.

## Making a good icon

Include these sizes in the one `.ico` file so it stays sharp at every display
scaling level: 16, 20, 24, 32, 40, 48, 64 and 256 px. Free tools such as
GIMP (export as `.ico`, one layer per size) or ImageMagick can do this:

```
magick icon-256.png -define icon:auto-resize=256,64,48,40,32,24,20,16 app.ico
```

After adding or changing the icon, rebuild. MSBuild only checks for the file
when the project is evaluated, so if the icon doesn't appear, run
`dotnet build --no-incremental` once.

## If Windows still shows the old icon

Windows caches icons aggressively. Rename the exe, or unpin and re-pin it
from the taskbar, to force a refresh.
