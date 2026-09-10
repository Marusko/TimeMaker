# lib

Two assemblies that are not published on nuget.org, so the built files are committed here
and referenced from `TimeMaker.csproj` by `HintPath`.

## ClickWrap.UpdateClient.dll

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

## RaceResultClient.dll

`RaceResultClient.dll` — RaceResultClient.NET, the typed race|result Web API client
built from the `RaceResultClient` repo beside this one. It is used only by
[`Services/RaceResultSetupService.cs`](../Services/RaceResultSetupService.cs), which logs in,
lists the account's upcoming events and creates the event's Simple API entries when the user
picks `Prihlásiť sa do RaceResult` in the RaceResult settings window. The running app itself
talks to RaceResult through the Simple API links, which need no login and no client.

The same assembly is used by the API Creator and by Trakster; the copy here is theirs.

To refresh it after a change to the client:

```powershell
dotnet build ..\RaceResultClient\RaceResultClient.csproj -c Release
copy ..\RaceResultClient\bin\Release\net10.0\RaceResultClient.dll lib\
```

The Simple API entries Time Maker asks for live in
[`Models/RaceResultApiCatalog.cs`](../Models/RaceResultApiCatalog.cs) and must stay in step
with what the API Creator writes for Time Maker (`Configuration/ApiCatalog.cs` in that repo)
and with the labels [`Services/RaceResultService.cs`](../Services/RaceResultService.cs)
switches on.
