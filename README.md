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
tools/generate_emoji_catalogue.py
sample-workspace/
```

## Keyboard

Alt+F / Alt+E / Alt+V / Alt+H open the menus. Ctrl+O opens a workspace,
Ctrl+C copies (the selected text in a text box if there is a selection,
otherwise the active tab's emoji, phrase, or signature), Ctrl+F jumps to
search, Ctrl+R refreshes from disk, F5 makes a new message ref, and Ctrl+,
opens Preferences. Ctrl+Tab switches tabs. In the Phrases tab, Enter edits
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

Stored in `%APPDATA%\QDVC\NiceMail\preferences.json`: toolbar style, skin tone,
signature font, reopen-last-workspace, plus remembered session state
(disclaimer / Ref Only toggles, profile, Note to Self address, window size).

## Known limitations

Standard Win32 list controls draw emoji with GDI, which renders Segoe UI Emoji
in monochrome rather than colour. The copied emoji are the real characters and
appear in colour wherever they're pasted.
