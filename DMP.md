Pomáhej mi krok za krokem vytvořit malý test projekt: jednoduchý RSS reader v ASP.NET Core MVC + EF Core + SQLite.

Chci jednoduchou server-rendered MVC aplikaci s Razor views, bez SPA frontendu, bez samostatného REST API a bez zbytečného overengineeringu.

Funkce:

- přidat RSS feed (name + url)
- list feedů
- smazat feed
- detail feedu
- list articles ve feedu
- filter articles by date
- reload articles in feed

Architektura:

- Models: Feed, Article
- ViewModels tam, kde dávají smysl
- malá service layer pro RSS parsing / reload
- jednoduché MVC controllery a views
- Bootstrap / minimální UI

Důležité:

- chci rozumět tomu, co dělám
- navrhuj malé kroky
- vždy stručně vysvětli proč
- negeneruj celý projekt naráz
- drž se jednoduchého a čistého řešení vhodného pro test assignment
