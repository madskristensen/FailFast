[marketplace]: <https://marketplace.visualstudio.com/items?itemName=MadsKristensen.FailFast>
[repo]: <https://github.com/madskristensen/FailFast>
[build]: <https://github.com/madskristensen/FailFast/actions/workflows/build.yaml>
[ci-build]: <https://www.vsixgallery.com/extension/FailFast.897947cf-5417-419c-9d8e-450b9480b07d>
[inspiration]: <https://marketplace.visualstudio.com/items?itemName=EinarEgilsson.StopOnFirstBuildError>

# Fail Fast for Visual Studio

[![Build](https://github.com/madskristensen/FailFast/actions/workflows/build.yaml/badge.svg)](https://github.com/madskristensen/FailFast/actions/workflows/build.yaml)
![GitHub Sponsors](https://img.shields.io/github/sponsors/madskristensen)

Download this extension from the [Visual Studio Marketplace][marketplace] or grab the latest CI build from [VSIX Gallery][ci-build].

---

**Fail Fast stops a solution build as soon as the first project fails.** Instead of waiting for the rest of the projects to continue compiling, the extension cancels the build immediately and writes a short explanation to the Build output window.

This is especially useful in larger solutions where a single failure usually makes the rest of the build irrelevant.

> This extension was inspired by Einar Egilsson's [Stop on first build error][inspiration].

## Why use it?

- **Shorter feedback loops** - stop spending time on projects that no longer matter after the first failure
- **Less output noise** - focus on the first real error instead of scrolling through follow-up failures
- **Built into Visual Studio** - toggle the behavior from the **Build** menu when you need it

![Output Window](art/output-window.png)

## Getting started

1. Install the extension from the [Visual Studio Marketplace][marketplace].
2. Open a solution with multiple projects.
3. Use **Build > Stop Build on First Error** to enable or disable the feature.
4. Start a build as usual.
5. When a project fails, Fail Fast cancels the remaining build immediately.

<!-- TODO: Add screenshot of the Build menu toggle here. -->

## What it does

- Watches solution builds in Visual Studio
- Cancels the build after the first failed project
- Writes a `FailFast:` message to the Build output pane when cancellation happens
- Remembers whether the feature is enabled

## Notes

- The command is only shown when a solution with multiple projects is open.
- This extension targets Visual Studio 2022 on both amd64 and arm64.
- CI builds are available on [VSIX Gallery][ci-build], and publishing is handled by the [Build workflow][build].

## How can I help?

If you enjoy using the extension, please give it a rating on the [Visual Studio Marketplace][marketplace].

If you run into a bug or have an idea for an improvement, open an issue in the [GitHub repo][repo].

Pull requests are welcome.
