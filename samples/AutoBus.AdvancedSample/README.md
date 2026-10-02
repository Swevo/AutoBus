# AutoBus Advanced Sample

This sample is now a **compile-ready app** (`AutoBus.AdvancedSample.csproj`) that wires middleware,
deduplication, dead-lettering, partitioning, retries, and deferred scheduling together.

## Run

```bash
dotnet run --project samples/AutoBus.AdvancedSample/AutoBus.AdvancedSample.csproj
```

Key implementation files:

- `Program.cs` — full DI setup and example publishes
- `AutoBus.AdvancedSample.csproj` — references `AutoBus` directly

## What this demonstrates

- **Retries with jitter** on consumer failure
- **Dead-letter sink invocation** when retries are exhausted
- **Deduplication boundary** before consumer invocation
- **Middleware execution** around every consumer
- **Deferred scheduling** for later publication
- **Partitioned processing** for per-order sequential handling
- **A runnable baseline** you can copy into a real service and replace in-memory dedupe/dead-letter components
