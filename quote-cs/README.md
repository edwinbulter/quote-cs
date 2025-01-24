# C-Sharp backend for the Quote app
This application can serve as the API backend for the React frontend which is available at:
https://github.com/edwinbulter/quote-web

## Implemented features:
- When a random quote is requested and the database is empty, A set of quotes will be requested at ZenQuotes and written in the default SQL Server database.
- Only unique quotes are written to the database:
  - if the quoteText/author combination doesn't appear in the database, it is added
- When requesting a random quote, 'quote ids to exclude' can be sent in the body of the POST request to avoid sending the same quote again when requesting a random quote
- If the list with 'quote ids to exclude' exceeds the number of quotes in the database:
  - a set of quotes is requested at ZenQuotes, added to the database and a random new quote is returned
- Liking of quotes
  - Liked quotes will get their likes field incremented
- A list with liked quotes sorted by the number of likes can be requested.

## Testing the endpoints
When the application is started from Visual Studio, the endpoints can be tested from the swagger page that will show up in the browser at
[http://localhost:5023/swagger/index.html](http://localhost:5023/swagger/index.html)
