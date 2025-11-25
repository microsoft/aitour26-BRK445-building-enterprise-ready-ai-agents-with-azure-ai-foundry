Video: [brk445-slide 19-demo.mp4](https://aka.ms/AAxrab6) — 00:06:46

# Multi-Agent Orchestration — Demo Guide

This guide documents the second scenario (multi-agent) in the demo. It explains orchestration patterns (default, sequential, concurrent, handoff, group chat), shows a sample store query flow that invokes multiple agents, and covers observability and verification steps. Video length: 00:06:45.

---

## Overview

Demo highlights:
- Demonstrates a multi-agent architecture using Aspire and Semantic Kernel; multiple agents are connected to different services to provide distinct capabilities/tools. [00:00:03 – 00:00:19]
- Orchestration modes: default (coordinated), sequential, concurrent, handoff (router agent), and group chat (worker/reviewer). Choose the orchestration type based on the required coordination and output coherence. [00:01:02 – 00:01:37]
- Example query: user asks "I can't find the product paint sprayer turbo price 750" — triggers inventory, matchmaking, location, and navigation agents; the orchestrator returns an orchestration ID and debug info for troubleshooting. [00:00:46 – 00:02:42]
- Concurrent mode runs tasks in parallel but may produce incoherent outputs; handoff mode with a router agent sequences steps and composes coherent results. [00:01:22 – 00:04:00]
- All agent activities are traceable; Aspire provides full traceability and logging for debugging multi-agent scenarios. [00:04:20 – 00:06:45]

---

## Step-by-step instructions

### 1) Start — Understand the multi-agent runtime
Time: 00:00:01 – 00:00:25

Goal: Familiarize yourself with the multi-agent architecture and the orchestration endpoints.

Steps:
1. Confirm the Aspire orchestration controller is running (single entry point with API endpoints).
2. Review available agents (inventory, matchmaking, location, navigation, etc.) and note which external services they call.
3. Identify the orchestration implementation(s) available (default, sequential, concurrent, handoff, group chat).

Tip: The orchestration controller exposes different endpoints to trigger each pattern — use them to reproduce scenario variants.

---

### 2) Run the sample multi-agent scenario (store query)
Time: 00:00:46 – 00:02:42

Goal: Reproduce the sample user query and collect orchestration outputs and trace info.

Steps:
1. In the storefront demo, submit the sample query (e.g., "I can't find the product paint sprayer turbo price 750").
2. Select the orchestration type to test (start with "default" to reproduce the demo example).
3. Watch the orchestrator trigger sub-steps (inventory → matchmaking → location → navigation) and capture the orchestration ID shown in the UI.
4. Inspect debug output or logs returned in the UI for each step, and copy any trace IDs for deeper analysis.

Tip: The demo UI shows debugging information and the orchestration ID — use these to locate corresponding traces in Aspire/Application Insights.

---

### 3) Understand orchestration patterns and their trade-offs
Time: 00:01:22 – 00:04:24

Goal: Learn when to use each orchestration pattern.

Patterns:
- Default: coordinated single process that sequences steps — good for deterministic flows.
- Sequential: explicit step-by-step execution where outputs feed the next step.
- Concurrent: launches multiple agent tasks in parallel; faster but may produce inconsistent or duplicated outputs because each agent runs independently.
- Handoff (router): use a router agent to manage step order, handoffs between agents, and mini-logic for composition — more coherent aggregated output.
- Group chat: worker and reviewer roles collaborate; use for scenarios requiring peer review or consensus.

Tip: When concurrent outputs are incoherent, prefer a handoff/router approach to sequence steps and aggregate results reliably.

---

### 4) Troubleshooting agent outputs (practical tips)
Time: 00:02:35 – 00:03:15

Goal: When outputs are missing or malformed (e.g., navigation not returning JSON), identify the responsible agent and fix the payload/formatting.

Steps:
1. Use the orchestration ID to find the trace in Aspire/application logs.
2. Inspect each agent’s output (inventory, matchmaking, location, navigation) to see whether JSON payloads are well-formed.
3. If a particular service (e.g., navigation) doesn’t return JSON, update its implementation to return structured output or add a translation step in the router agent.

Tip: Add validation checks to agent responses to ensure downstream agents receive the expected data shapes.

---

### 5) Handoff & router agent configuration
Time: 00:04:28 – 00:05:08

Goal: Configure a router agent to control handoffs between agents and implement a mini logic for ordering steps.

Steps:
1. Implement or enable the router agent that knows each worker agent’s role and capabilities.
2. Define handoff rules (e.g., max handoffs, preferred order: inventory → matchmaking → navigation).
3. Test with sample queries and observe how the router composes and sequences the worker agent outputs.

Tip: The router can also perform data normalization and ensure the final result is coherent and well-structured for presentation.

---

## Suggested follow-up & verification template

Use the template below for filing issues or validation tickets related to multi-agent orchestration:

- Title: Multi-agent orchestration — verify product search & aggregated output
- Environment: staging / (demo)
- Timestamp: [e.g., 00:00:46 – 00:02:00]
- Steps to reproduce:
  1. Open the store demo and submit a product search query (e.g., "paint sprayer turbo 750").
  2. Select orchestration type (default / concurrent / handoff) and run.
  3. Capture orchestration ID and collect per-agent outputs and traces.
  4. Validate aggregated output matches expected structure (JSON), and verify navigation outputs are parsable.
- Expected result: Orchestrator returns a coherent aggregated response listing product matches and navigation instructions (structured output).
- Actual result: [Describe observed behavior: incoherent responses, malformed JSON, missing fields, etc. Attach traces/logs.]
- Attachments: orchestration ID, trace IDs, screenshots, agent outputs.
- Priority / Assignee: team to triage.

---

## Quick reference — UI elements and artifacts

- Orchestration endpoints — choose default / sequential / concurrent / handoff / group chat.
- Orchestration ID — key for locating traces in Aspire/Application Insights.
- Agent outputs — inspect per-agent payloads (inventory, matchmaking, location, navigation).
- Router agent — implement sequencing and data normalization when using handoff pattern.
- Traces & logs — use Aspire traces to follow the entire multi-agent flow.

---

