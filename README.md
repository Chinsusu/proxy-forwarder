# Proxy Forwarder (Skeleton)
A minimal multi-project skeleton to build a desktop **proxy forwarder** for transforming upstream proxies (`HOST:PORT:USER:PASS`) into local forwarders on `127.0.0.1:<port>`.

## Build
```bash
dotnet build src/ProxyForwarder.sln
dotnet run --project src/ProxyForwarder.App/ProxyForwarder.App.csproj
```
