# ADR-002 Brain Loop Spike

Purpose: prove that Loren can use the official OpenAI .NET Responses client while retaining control of the action boundary.

Expected flow:

```text
user input
  -> OpenAI brain
  -> structured function/action request
  -> Loren ActionGateway validation
  -> fake read-only executor
  -> structured action result
  -> OpenAI brain
  -> final answer
```

This spike is intentionally disposable. It is not production Loren architecture.

## Requirements

- .NET 10 SDK
- `OPENAI_API_KEY`
- optional `LOREN_OPENAI_MODEL` (defaults to `gpt-5-mini`, matching the validated OpenAI .NET 2.12.0 Responses examples)
- optional `LOREN_OPENAI_TIMEOUT_SECONDS` (defaults to 60)

## Run

```bash
dotnet run --project spikes/adr-002/brain-loop/Loren.Spike.Brain.csproj
```

The loop uses the async Responses API and passes one cancellation token through every provider call. `Ctrl+C` cancels the current run; the timeout provides a second hard bound.

## Validation

The repository-level `M0 ADR-002 Spike` workflow compiles this spike on .NET 10. A live API round trip requires `OPENAI_API_KEY` supplied outside the repository.

The live validation must prove both:

1. normal path: model requests `get_project_status` -> Loren gateway -> structured result -> final model response -> `PASS`;
2. cancellation path: cancel an in-flight provider request and observe `CANCELLED` without bypassing the Loren boundary.

## Pass criteria

The normal process must print a final `PASS` line after a model-requested `get_project_status` action crosses Loren-owned code and the structured result is returned to the model.

The spike has a hard six-turn limit plus a wall-clock timeout. No privileged external credential is exposed to the action request or fake executor.
