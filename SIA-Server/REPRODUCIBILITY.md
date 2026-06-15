# Public Release Notes

## 1) Scope
This public server release exposes module responsibilities and interface contracts while omitting internal implementation details.

Goal:
- Help external teams reproduce the workflow design and evaluation protocol.
- Provide enough observable contracts to validate behavior against the paper.

Out of scope:
- Internal private prompts, proprietary heuristics, and private model orchestration internals.

## 2) Pipeline Snapshot
Input audio/text + embodiment context
-> speechPatternAnalyzer
-> spatialContextResolver
-> projector
-> feedforwardGenerator
-> dataComposer
-> validator
-> main_server response

## 3) Environment Baseline
- OS: Windows 10/11 (reference)
- Python: 3.9+ (recommend pinning exact patch in your local setup)
- Server deps: see requirements.txt
- API dependency: OpenAI-compatible chat/transcription endpoint

Checklist:
- [ ] Python environment created
- [ ] requirements installed
- [ ] auth file configured
- [ ] dataset files available
- [ ] moduleSpec.json and chartSpec.json initialized

## 4) Contract-First Module I/O
This section summarizes public module I/O contracts for understanding the server workflow. 

### speechPatternAnalyzer
Input:
- transcribed text
- optional context from prior step

Output:
- task hypothesis
- uncertainty and speech-act summaries

### spatialContextResolver
Input:
- embodiment signals (gaze/head)
- current chart context

Output:
- spatialContextResolver object
- spatialContextTypes list

### projector
Input:
- analyzer output
- spatial context
- module/chart specs

Output:
- context_summary
- data_summary
- decision payload for downstream modules

### feedforwardGenerator
Input:
- projector output
- task-specific prompt profile (public summary)

Output:
- feedforward candidates
- selected final feedforward
- moduleSpec update payload

### dataComposer
Input:
- projector output
- chart spec context

Output:
- generated chart-spec payload
- reset flag / validation-ready object

### validator
Input:
- generated payload
- schema/rule constraints

Output:
- valid/invalid result
- sanitized or fallback-ready payload

## 4) Known Limits (Public Build)
- Internal prompts are summarized, not fully disclosed.
- Private selection heuristics are abstracted to contract-level rules.
- Output text may vary by model provider even with same high-level settings.

