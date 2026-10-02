# AutoBus Distribution Playbook

## GitHub Discussions post (copy/paste)

Title: `AutoBus update: reliability features + analyzers + RabbitMQ 1.0.2`

Body:

- `Swevo.AutoBus 1.2.0` adds middleware, dead-lettering hooks, dedupe hooks, partitioned concurrency, cooldowns, scheduling, and telemetry.
- `Swevo.AutoBus.RabbitMQ 1.0.2` removes the NU1902 transitive warning by upgrading SourceLink.
- `Swevo.AutoBus.Analyzers 1.2.0` adds five diagnostics (ABUS001-ABUS005) with code fixes for ABUS001-ABUS003:
  - `ABUS001` missing await on bus/scheduler async calls
  - `ABUS002` broad `catch (Exception)` in consumers/handlers
  - `ABUS003` excessive retry count in `UseRetry(...)`
- New runnable sample: `samples/AutoBus.AdvancedSample`.

## Social post (short)

AutoBus update shipped ✅  
• `Swevo.AutoBus 1.2.0` reliability/observability improvements  
• `Swevo.AutoBus.RabbitMQ 1.0.2` NU1902 warning removed  
• `Swevo.AutoBus.Analyzers 1.0.0` (ABUS001–ABUS003)  
• Runnable advanced sample included

NuGet:
- https://www.nuget.org/packages/Swevo.AutoBus
- https://www.nuget.org/packages/Swevo.AutoBus.RabbitMQ
- https://www.nuget.org/packages/Swevo.AutoBus.Analyzers
