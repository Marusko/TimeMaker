# Time Maker

### Aplikácia pre vytváranie manuálnych impulzov z CSV súboru alebo sériového portu s ALGE Timy formátom z viacerých zdrojov v [RaceResult](https://www.raceresult.com/en-us/home/index) naprogramovaná v **[.NET 10](https://dotnet.microsoft.com/en-us/)**

### Aplikácia je využívaná interne spoločnosťou [ČasomieraPT](https://casomierapt.com/)

### V RaceResult je potrebné mať vytvorené tieto API:
* Typ: `Custom` - Detaily: `rawdata/addmanual` - Label: `manual`
* Typ: `Custom` - Detaily: `rawdata/setinvalid` - Label: `invalid` - Voliteľné pre automatickú invalidáciu
* Typ: `Custom` - Detaily: `rawdata/get` - Label: `search` - Voliteľné pre automatickú invalidáciu
* Typ: `Custom` - Detaily: `timingpoints/get` - Label: `points`
* Typ: `Custom` - Detaily: `data/list?&fields=Bib&listformat=JSON` - Label: `bibs` - Voliteľné pre CSV šablónu
* Typ: `Custom` - Detaily: `simpleapi/get` - Label: `api` - Tento link sa kopíruje do Time Maker

Táto aplikácia využíva tieto externé služby:
* [RaceResult](https://www.raceresult.com/en-us/home/index)

Táto aplikácia využíva tieto knižnice:
* [Newtonsoft.Json](https://www.newtonsoft.com/json)
* [System.IO.Ports](https://www.nuget.org/packages/System.IO.Ports)
* [Microsoft.Toolkit.Uwp.Notifications](https://www.nuget.org/packages/Microsoft.Toolkit.Uwp.Notifications)
---
### Kontakt na mňa
Keď nastanú nejaké problémy s aplikáciou alebo sa budete chcieť niečo opýtať, prípadne navrhnúť novú funkciu, neváhajte ma kontaktovať:
* telefón: 0948 776 559
* email: matus@susky.net
* aj cez WhatsApp
