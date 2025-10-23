# Phase 3 Implementation Plan – Microsoft Agent Framework Completion

## Context Recap

- **Phase 1 (Service Controller Refactoring):** All eight microservices now expose paired `/sk` and `/agentfx` endpoints with shared business logic, dual DI registrations, and framework-specific logging, as captured in `PHASE1_COMPLETION_SUMMARY.md`.
- **Phase 2 (Service Consumer Routing):** MultiAgentDemo and SingleAgentDemo service consumers gained runtime framework switches (`SetFramework`) ensuring requests route to the correct endpoints, as detailed in `PHASE2_COMPLETION_SUMMARY.md`.
- **Phase 3 Mission:** Replace the AgentFx fallbacks/TODOs with production-ready Microsoft Agent Framework integrations while keeping Semantic Kernel parity and the dual-framework architecture intact.

## Goals and Scope

1. **Implement real AgentFx calls** in every controller still using placeholder logic, matching Semantic Kernel behavior and response formats.
2. **Harden shared infrastructure** for AgentFx (configuration, provider ergonomics, error handling, logging, and observability).
3. **Validate end-to-end flows** for both frameworks through automated and manual tests, ensuring toggling via the frontend continues to work.
4. **Document operational guidance** covering configuration, deployment, and troubleshooting for the completed dual-framework setup.

Out of scope for Phase 3: Large-scale performance tuning, telemetry dashboards, or major UX changes (may be revisited afterward).

## TODO Inventory in C# Code

| # | Service Area | File & Method | Current Placeholder | Customer Impact |
|---|--------------|---------------|----------------------|-----------------|
| 1 | ToolReasoningService | `ReasoningController.GenerateDetailedReasoningWithAgentFx` | `// TODO: Implement full AgentFx integration` | AgentFx reasoning requests fall back to rule-based text. |
| 2 | AnalyzePhotoService | `PhotoAnalysisController.AnalyzeAgentFxAsync` | `// TODO: Implement actual Agent Framework invocation` | Photo analysis via AgentFx returns heuristic data only. |
| 3 | InventoryService | `InventoryController.SearchInventoryInternalAsync` | `// Use AgentFx agent - TODO: Implement full AgentFx integration` | AgentFx inventory searches ignore real agents, limiting parity with SK. |
| 4 | CustomerInformationService | `CustomerController.GetCustomerAgentFx` | `// TODO: Implement actual Agent Framework invocation` | AgentFx customer lookups never query AI and always use fallback records. |
| 5 | AgentsCatalogService | `AgentCatalogController.TestAgentFxAsync` | `// TODO: Implement actual Agent Framework invocation` | AgentFx agent testing endpoint doesn't exercise Agent Framework (always uses canned copy). |

## Cross-Cutting Prerequisites

- **Configuration audit:** Ensure every microservice has AgentFx agent IDs (parallel to the SK agent IDs noted in Phase 1). Extend `appsettings.*` or service defaults to surface: `inventoryagentfxid`, `customeragentfxid`, `photoanalysisagentfxid`, etc.
- **Provider refactor:** Update `AgentFxAgentProvider` to accept and persist a default agent ID (mirroring `AIFoundryAgentProvider`). Add overloads for per-call overrides and validate Azure CLI credential flow.
- **Error & retry policy:** Define a common policy (e.g., exponential backoff for 429/5xx, fallback to deterministic responses only after logging structured errors).
- **Logging & telemetry:** Adopt consistent log categories (`[AgentFx]`) and enrich logs with agent IDs, latency, token usage placeholders, and correlation IDs to support observability.
- **Security & secrets:** Confirm agent endpoint values come from managed configuration / Key Vault via Aspire service defaults; document rotation steps.

## Workstreams

### WS1 – Implement AgentFx Invocation (Service Controllers)

1. **ReasoningController (ToolReasoningService)**
   - Introduce AgentFx chat session flow: acquire agent via provider, create a thread/session, post `ReasoningRequest` prompt, and stream aggregated response.
   - Align parsing with SK path (string response). Maintain deterministic fallback only on hard failures.
   - Emit structured telemetry (framework, agentId, prompt hashes) and clean up sessions if SDK requires explicit disposal.

2. **PhotoAnalysisController (AnalyzePhotoService)**
   - Use AgentFx to process multimodal request: send textual prompt plus image reference (upload to blob/temporary store or use agent attachments depending on SDK support).
   - Parse strict JSON response to `PhotoAnalysisResult`, mirroring SK logic (brace extraction, JSON parse with validation).
   - Handle large payloads via streaming or chunking; document size limits and fallback triggers.

3. **InventoryController (InventoryService)**
   - Call AgentFx agent to return SKU list; re-use prompt from SK flow for parity.
   - Centralize SKU parsing/validation so both frameworks share the same `ParseSkuList` helper.
   - Replace temporary heuristic `GetFallbackInventorySearch` with true fallback invoked only on exception; log agent output for auditing.

4. **CustomerController (CustomerInformationService)**
   - Query AgentFx agent with JSON-specific instruction, deserialize to `CustomerInformation`, and preserve fallback path for missing records.
   - Add guardrails to detect malformed JSON (logging + fallback) similar to SK branch.
   - Ensure both GET and POST routes reuse helper methods for prompts and parsing.

5. **AgentCatalogController (AgentsCatalogService)**
   - Execute AgentFx test call using supplied `agentId`, streaming responses back to caller.
   - Support partial responses and propagate SDK errors to the client with actionable messages.
   - Update fallback logic so it only executes when AgentFx call fails.

### WS2 – Shared Infrastructure Hardening

- **`AgentFxAgentProvider` enhancements:**
  - Accept default agent ID in constructor, validate parameters, and add caching/pooling if SDK benefits from it.
  - Provide helper to construct chat sessions/threads, minimizing duplication in controllers.
- **Utility abstractions:** Consider extracting shared AgentFx response parsing (e.g., JSON extraction) into reusable helpers or base classes to reduce code duplication highlighted at the end of Phase 2.
- **Resilience policies:** Integrate Polly or SDK-native retry handlers; ensure HTTP clients have sensible timeouts for AgentFx operations.
- **Observability:** Hook Application Insights (per Phase 1 resource list) to capture custom metrics for AgentFx success/failure counts and latency.

### WS3 – Verification & Quality Assurance

- **Automated tests:**
  - Add integration-style tests (can be flagged as `[Category("AgentFx")]`) using test doubles or recorded responses to validate JSON parsing, fallback logic, and routing.
  - Extend `Products.Tests` or create service-specific test projects to cover prompt builders and parser helpers.
- **Manual validation:**
  - Repeat Phase 2 manual scenarios toggling frameworks; confirm logs show real AgentFx hits.
  - Validate error paths (invalid agent ID, missing configuration) surface cleanly to the UI while logging detailed diagnostics.
- **Performance checks:**
  - Measure latency deltas between SK and AgentFx; document expectations and thresholds.

### WS4 – Documentation, DevOps, and Rollout

- **Docs updates:**
  - Append Phase 3 work to `PHASE3_IMPLEMENTATION_PLAN.md` (this document), `SERVICE_REFACTORING_GUIDE.md`, and deployment guides with new configuration keys and troubleshooting tips.
  - Highlight Microsoft Agent Framework sample usage; reference any external snippets provided by stakeholders.
- **Deployment readiness:**
  - Update `DEPLOYMENT.md` and infrastructure templates if additional environment variables or secrets are required.
  - Ensure `deploy.ps1`/`deploy.sh` upload new configuration values (agent IDs, connection strings) safely.
- **Knowledge transfer:**
  - Prepare runbooks for operations teams covering how to rotate agent IDs, enable/disable frameworks, and inspect logs.

## Deliverables & Milestones

| Milestone | Description | Owners | Target |
|-----------|-------------|--------|--------|
| M1 | Configuration & provider hardening complete; SDK access validated | Infra/Core | Week 1 |
| M2 | AgentFx implementations merged for Reasoning & PhotoAnalysis | Service Teams | Week 2 |
| M3 | Remaining controllers (Inventory, Customer, Agent Catalog) integrated with AgentFx | Service Teams | Week 3 |
| M4 | Test suite + manual validation completed for dual-framework flows | QA/Service Teams | Week 4 |
| M5 | Documentation & deployment assets updated; go-live sign-off | DevRel/Infra | Week 4 |

## Risks & Mitigations

- **SDK parity gaps:** Microsoft Agent Framework features may lack streaming or multimodal parity. *Mitigation:* Engage with product team, leverage provided samples, and maintain graceful degradation with clear communication.
- **Configuration drift across services:** Multiple agent IDs increase risk of misconfiguration. *Mitigation:* Centralize configuration via Aspire defaults/Key Vault and add startup validation checks that log/abort on missing IDs.
- **Latency or rate limits:** AgentFx calls might be slower than SK. *Mitigation:* Instrument metrics, add retries with jitter, and document SLA expectations.
- **Fallback overuse hiding bugs:** Existing heuristics may mask integration issues. *Mitigation:* Add alerting when fallback paths trigger more than a defined threshold and expose telemetry counters.

## Dependencies & Open Questions

- Confirm availability of production-ready AgentFx sample code or guidance (stakeholder offered to provide examples if needed).
- Validate whether image payload handling requires additional storage or signed URLs.
- Determine if shared base classes (suggested in Phase 2 future enhancements) should be part of Phase 3 or deferred.

## Acceptance Criteria

- All TODO markers removed with functional AgentFx integrations across the five identified controllers.
- Automated tests cover happy-path and failure-path invocation for both frameworks with consistent response contracts.
- Documentation reflects configuration, operational procedures, and troubleshooting for AgentFx usage.
- Manual validation confirms the UI toggle seamlessly switches between SK and AgentFx flows across MultiAgentDemo and SingleAgentDemo.
