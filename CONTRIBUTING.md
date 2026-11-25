# Contributing to DeskDot

Thanks for taking the time to contribute. DeskDot is a small, dependency-light Windows app:
please keep pull requests focused and avoid adding NuGet packages without discussing it first.

## Getting set up

```powershell
git clone https://github.com/Maheshyadav1318/DeskDot.git
cd DeskDot
dotnet build DeskDot.sln -c Debug -p:Platform=x64
dotnet run --project src/DeskDot/DeskDot.csproj
```

Requirements: Windows 10 1809+, .NET 8 SDK. Visual Studio 2022 (17.9+) is optional but
convenient for XAML.

## Running the tests

```powershell
dotnet test tests/DeskDot.Tests/DeskDot.Tests.csproj
```

The test project targets plain `net8.0` and only covers the platform-neutral logic in
`src/DeskDot/Core` plus the two file-based services, so it runs on Windows, Linux and macOS.
**New logic belongs in `Core` with tests.** Anything touching WPF or Win32 lives in
`Services`/`ViewModels` and is verified by hand using [docs/TESTING.md](docs/TESTING.md).

## Code layout

| Path | Purpose | Tests |
| --- | --- | --- |
| `src/DeskDot/Core` | Pure logic (agenda planning, hotkey parsing, focus clock, formatting) | Required |
| `src/DeskDot/Models` | Records and settings | Covered where pure |
| `src/DeskDot/Services` | Win32/WPF services (stats, hotkey, tray, power, media) | Manual |
| `src/DeskDot/ViewModels` | Bindable island state | Manual |
| `src/DeskDot/Views`, `Themes` | XAML surfaces | Manual |

## Coding conventions

- Nullable reference types are **on**; implicit usings are **off** (WinForms globals would
  clash with WPF types such as `Application`, `KeyEventArgs`, `Color`). Add explicit usings.
- Every service must degrade gracefully: a failure (no tray, no hotkey, unreadable settings)
  must never stop the overlay from starting. Never let an exception escape a Win32 call site.
- Dispose Win32/GDI handles you create; do not dispose shared system handles.
- XAML: keep styles and brushes in `Themes/`; avoid hard-coded colours in views.
- No new NuGet packages without prior discussion in an issue.

## Before opening a pull request

1. `dotnet test tests/DeskDot.Tests/DeskDot.Tests.csproj` passes.
2. `dotnet build DeskDot.sln -c Release -p:Platform=x64 -warnaserror` is clean
   (CI enforces warnings-as-errors).
3. You have walked the relevant parts of `docs/TESTING.md` on a real Windows machine.
4. The PR description says what changed, why, and how you verified it.

## Commit messages

Short imperative subject line (`Fix: settings could never be saved`), then a body explaining
the cause and the fix. Reference the behaviour, not the file list.

## Reporting bugs

Use the bug template. Please include your Windows build (`winver`), DeskDot version, display
setup (scale, number of monitors) and anything relevant from `%APPDATA%\DeskDot\crash.log`.

## Security

Please do not open a public issue for a security problem — see [SECURITY.md](SECURITY.md).

## License

By contributing you agree that your contributions are licensed under the MIT License.
