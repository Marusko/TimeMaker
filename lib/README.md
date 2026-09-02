# lib

`ClickWrap.UpdateClient.dll` — the update-check half of
[ClickWrap](https://github.com/Marusko/ClickWrap). It is not published on nuget.org, so the
built assembly is committed here and referenced from `TimeMaker.csproj` by `HintPath`.

The `.xml` beside it is the documentation file, kept only so IntelliSense shows the API docs.

To refresh it after a change to ClickWrap:

```powershell
dotnet build ..\ClickWrap\src\ClickWrap.UpdateClient\ClickWrap.UpdateClient.csproj -c Release
copy ..\ClickWrap\src\ClickWrap.UpdateClient\bin\Release\net10.0-windows\ClickWrap.UpdateClient.* lib\
```

The app id (`time-maker`) and server URL used with it live in
[`Services/UpdateService.cs`](../Services/UpdateService.cs) and must stay in step with
`src/ClickWrap.Installer/apps/time-maker.yaml` in that repo.
