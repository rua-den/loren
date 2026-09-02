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

## Run

```bash
dotnet run --project spikes/adr-002/brain-loop/Loren.Spike.Brain.csproj
```

## Validation

The repository-level `M0 ADR-002 Spike` workflow compiles this spike on .NET 10 for the spike PR. The live API round trip remains an explicit validation step because CI does not receive an OpenAI API key by default.

## Pass criteria

The process must print a final `PASS` line after a model-requested `get_project_status` action crosses Loren-owned code and the structured result is returned to the model.

The spike has a hard six-turn limit. No privileged external credential is exposed to the action request or fake executor.
