<!-- Thanks for contributing to DeskDot. Keep PRs focused on one change. -->

## What does this change?

<!-- One or two sentences. What behaviour is different after this PR? -->

## Why?

<!-- The problem. Link the issue: Fixes #123 -->

## How was it verified?

- [ ] `dotnet test tests/DeskDot.Tests/DeskDot.Tests.csproj` passes
- [ ] `dotnet build DeskDot.sln -c Release -p:Platform=x64 -warnaserror` is clean
- [ ] Walked the relevant checks in `docs/TESTING.md` on Windows

<!-- Paste anything useful: new test names, screenshots of the island, crash-log excerpts. -->

## Type

- [ ] Bug fix
- [ ] New feature
- [ ] Refactor / cleanup (no behaviour change)
- [ ] Docs / packaging / CI

## Checklist

- [ ] New logic lives in `src/DeskDot/Core` and has unit tests
- [ ] No new NuGet packages (or this was discussed in an issue first)
- [ ] No exceptions can escape Win32 call sites; failures degrade gracefully
- [ ] Win32/GDI handles created by this change are disposed
- [ ] XAML styles/brushes stay in `Themes/` (no hard-coded colours in views)
