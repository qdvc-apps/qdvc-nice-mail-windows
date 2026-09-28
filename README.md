# QDVC Nice Mail for Windows

A native Windows (WinForms) port of [QDVC Nice Mail](https://github.com/qdvc-apps/qdvc-nice-mail):
a searchable **emoji** picker with favourites and skin tones, a library of reusable
**phrases**, a plaintext **signature** assembler, and a **Note to Self** `.eml` writer.

The workspace folder format is the same as the GTK app's, so one folder can be
shared between the Linux and Windows versions.

## Requirements

Only the [.NET 10 SDK](https://dotnet.microsoft.com/download) is needed to build.
No Visual Studio. Any editor works; VS Code with the C# extension is a good fit.

## Build and run

```
dotnet run --project src/NiceMail                       # run
dotnet run --project src/NiceMail -- sample-workspace   # open a workspace on launch
dotnet test                                             # model tests
publish.cmd                                             # single-file exe in .\publish
```

To target .NET 8 instead, change `net10.0-windows` to `net8.0-windows` in both
`.csproj` files.

## Layout

```
src/NiceMail/
    Program.cs              entry point, command-line workspace argument
    Model/                  UI-free logic (CSV, workspace, signature, .eml, emoji, prefs)
    UI/                     main window, one TabView per tab, dialogs, helpers
    Resources/emoji.tsv     generated emoji catalogue (embedded in the exe)
tests/NiceMail.Tests/       xUnit tests for the model layer
branding/                   optional app.ico (not tracked by git)
tools/generate_emoji_catalogue.py
sample-workspace/
```

## Keyboard

Alt+F / Alt+E / Alt+V / Alt+H open the menus. Ctrl+O opens a workspace,
Ctrl+C copies (the selected text in a text box if there is a selection,
otherwise the active tab's emoji, phrase, or signature), Ctrl+F jumps to
search, Ctrl+R refreshes from disk, F5 makes a new message ref, and Ctrl+,
opens Preferences. Ctrl+Tab / Ctrl+Shift+Tab (or Ctrl+PgDn / Ctrl+PgUp) switch
tabs, and Alt+M, Alt+P, Alt+G and Alt+N jump to Emoji, Phrases, Signature and
Note to Self. In the Phrases tab, Enter edits
and Delete deletes. Esc clears a search box.

## Emoji catalogue

.NET has no equivalent of Python's `unicodedata.name()`, so the list of emoji
and their names is generated ahead of time and embedded as a resource:

```
python3 tools/generate_emoji_catalogue.py                 # built-in Unicode 15 table
python3 tools/generate_emoji_catalogue.py emoji-data.txt  # from the official data file
```

Ids use the same snake_case-of-Unicode-name scheme as the GTK app
(e.g. `waving_hand_sign`), and custom glyphs use `custom_<codepoints>`.

## Preferences

Stored in `%APPDATA%\QDVC\NiceMail\preferences.json`: theme, toolbar style,
skin tone, signature font, reopen-last-workspace, plus remembered session state
(disclaimer / Ref Only toggles, profile, Note to Self address, window size).

## Dark mode

Edit → Preferences → Theme offers *Follow Windows* (the default), *Light*, and
*Dark*. It uses WinForms' built-in dark mode (`Application.SetColorMode`), which
is applied at startup, so a change takes effect the next time the app starts.
Dark mode requires Windows 11; on Windows 10 the app stays light. A few
system-drawn elements, such as message boxes, remain light.

## Colour emoji

The standard Windows list control draws text with GDI, which shows emoji in
monochrome. The emoji column (and the skin-tone dropdown in Preferences) is
therefore owner-drawn with DirectWrite through a Direct2D DC render target,
which renders the colour layers of Segoe UI Emoji. It's implemented in
`UI/ColorEmoji.cs` with direct COM calls, so there's no extra dependency. If
Direct2D can't start (for example on some remote or virtualised sessions), the
list falls back to monochrome emoji.

Emoji typed into plain text boxes (phrases, Note to Self) still appear in
monochrome; that's how the standard Windows edit control draws them.

## Custom icon

Put a multi-size `.ico` at `branding/app.ico` (ignored by git) and rebuild; it
becomes the exe's icon and the window icon. See `branding/README.md`.
