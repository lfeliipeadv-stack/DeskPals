# How to contribute

Thanks for wanting to help DeskPals! There are three main ways. (Issues and pull requests are welcome in English or Portuguese.)

## A new pet for the gallery

1. Open the **Pet Editor** (Settings › App › Pet Editor, or `DeskPals-Editor.exe`).
2. Create the pet with the **New pet wizard** or change an existing one (use **Save as** to keep the original untouched).
3. Test it with **Test on desktop** (F5): it opens a second DesktopPet with only your pet, without touching yours.
4. Check the **Validator**: the gallery only accepts pets with no errors and with a name and author in the Header.
5. Click **Publish to gallery**: the editor creates the .zip (in `Documents\DeskPals\galeria`) and opens the
   [pet form](https://github.com/lfeliipeadv-stack/DeskPals/issues/new?template=new_pet.md) already filled in.
   Drag the .zip onto the page and submit.

Use your own drawings or ones whose license allows redistribution, and say where the art and sounds came from.
The full guide is in [Make your pet](https://lfeliipeadv-stack.github.io/DeskPals/wiki/Crie-o-seu-pet.html) (in Portuguese).

## A translation

The app's languages live in `src/lang` (the built-in ones) and in `Languages` (the ones downloaded from the
settings). Each file is a key → text JSON. To translate a new language or fix an existing one,
follow [Languages/README.md](Languages/README.md) and open a pull request.

## A bug or an idea

Open an [issue](https://github.com/lfeliipeadv-stack/DeskPals/issues/new/choose) with the bug or idea form.
For a bug, include the version (Settings › About), your Windows version and how to reproduce it.

## Code

The app is C# (Windows Forms) on .NET 10. Before sending a pull request:

```bash
dotnet build src/DesktopPet.Net.csproj -c Release
src\bin\Release\net10.0-windows\win-x64\DeskPals.exe --selftest=result.txt --only=<what you changed>
```

The self-test runs against the real code; the exit code is the number of failures. Details are in
[For developers](https://lfeliipeadv-stack.github.io/DeskPals/wiki/Para-desenvolvedores.html) (in Portuguese).

By contributing, you agree that your contribution is licensed under the project's [MIT license](LICENSE.md).
