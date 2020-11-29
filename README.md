# ServerTemplate
Template server for versatile use in gaming applications

NOTE FOR SERVER:
The server currently has an experimental world generator on it. The default settings I left it on to build with took about 23 minutes to render a (high detail, imo) bitmap. If you don't want to wait, download the source instead of the bundled server, and either comment out/remove the world gen portion (in Server.InitializeServerData) or reduce the number of generations it processes (Also in Server.InitializeServerData, where the WorldGenerator.SeedLandmass is called)

NOTE FOR CLIENT (PLEASE READ)
There is no exit button (yet), use ALT-F4 or task manager to close it
Use 127.0.0.1 to connect to server on local machine.

Server runs on port 26950.

3 archives available for download

-Client
--Unity Client that can connect to the server supplied in the other archives. Use 127.0.0.1 for ip and whatever username you want

-ServerClientBundle
--Unity client and server executable for quick testing

-ServerSrc
--Server source code
