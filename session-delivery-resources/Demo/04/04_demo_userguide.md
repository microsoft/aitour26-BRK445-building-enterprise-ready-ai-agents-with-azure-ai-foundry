Video: [brk445-slide 24-demo.mp4](https://aka.ms/AAxrpqk) — 00:01:30

# Azure Monitor and AI Foundry — Correlated Diagnostics (Demo Guide)

This guide documents the demo walkthrough shown in the video. The demo highlights how to use Azure Monitor (Application Map), Azure AI Monitor, and AI Foundry tracing to locate and investigate application issues. Video length: 00:01:30.

---

## Overview

Demo highlights:
- Use Azure Monitor Application Map to identify the likely problem node and then drill down for more details (deep dive).
- Use Azure AI Monitor to perform request-level analysis and identify single-agent call interruptions or failures.
- Correlate Azure Monitor data with AI Foundry tracing (mix/barged calls) to identify model or service outages (for example, a suspected outage of GPT‑5 / GPT5 mini chat).


---

## Step-by-step instructions

### 1) Start — Open Azure Monitor
Time: 00:00:02 – 00:00:10

Goal: Access Azure Monitor and prepare to inspect the Application Map and relevant metrics.

Steps:
1. Sign in to the Azure portal and open Azure Monitor.
2. Select the target application's Application Map.
3. Confirm recent errors or latency alerts are visible.

Tip: If no data is visible, verify that monitoring agents and instrumentation are deployed and that telemetry is being collected.

---

### 2) Inspect the Application Map and locate the affected node
Time: 00:00:12 – 00:00:16

Goal: Identify the highlighted node and determine which layer is affected (frontend, backend, third-party model, etc.).

Steps:
1. Locate the red/highlighted node in the Application Map.
2. Click the node to expand dependency and call summaries.
3. Record the error types, impact scope, and recent time windows showing anomalies.

Tip: Record the node name and timestamps — this information is critical for cross-system tracing and for creating verification tickets.

---

### 3) Deep dive with Azure AI Monitor
Time: 00:00:31 – 00:00:46

Goal: Inspect request-level details and dependencies to find when a single-agent call experienced interruption or abnormal behavior.

Steps:
1. In Azure AI Monitor (or the relevant monitoring dashboard), open the detailed view for the selected node or call.
2. Review the request timeline, error rates, latency distribution, and associated logs.
3. Confirm whether there is a clear interruption (e.g., a window of repeated failures).

Tip: Mark suspect time ranges and flag them for tracing correlation.

---

### 4) Identify model or third-party service outages (example: GPT‑5)
Time: 00:00:46 – 00:01:04

Goal: Determine whether an external model or service is the source of the problem, as suggested in the demo where a GPT‑5 mini chat model is potentially down.

Steps:
1. From the deep dive, find dependencies that point to external model calls.
2. Check the dependency's error and latency statistics and verify if the timestamps align with the observed failure.
3. If available, consult model/service health or diagnostic pages for additional context.

Tip: Access to model runtime metrics or agent-level telemetry can help confirm a model-layer outage.

---

### 5) Correlate Azure Monitor and AI Foundry tracing
Time: 00:00:51 – 00:01:06

Goal: Mix and match monitoring and tracing data to locate the exact failing requests and trace IDs; identify root cause.

Steps:
1. From Azure Monitor, capture the timestamp range for suspected failures.
2. In AI Foundry or the tracing system, search by timestamp or transaction/request ID for corresponding traces (trace ID / span ID).
3. Compare failure sources, error messages, and stack/trace details to identify which service or model returned the abnormal response.

Tip: If you observe a short outage window, look for correlated timeouts or repeated identical errors to help pinpoint the issue.

---

## Suggested follow-up actions and verification template

When a model or service-level outage is suspected, create a remediation/verification task using this template:

- Title: GPT‑5 mini chat model outage during single-agent demo
- Environment: staging / (deployment name)
- Timestamp: e.g., 00:00:46 – 00:00:50
- Steps to reproduce:
  1. Open Azure Monitor → Application Map → locate the highlighted node.
  2. In Azure AI Monitor, find the requests related to the model call and note the timestamp and request ID.
  3. In AI Foundry tracing, search by timestamp/trace ID to fetch the trace, spans, and logs for that request.
- Expected result: Model service responds normally and error/latency metrics remain within acceptable thresholds.
- Actual result: Model service shows repeated failures or timeouts during the specified window (attach logs, trace IDs, and screenshots).
- Attachments: screenshots, trace IDs, relevant log snippets.
- Priority / Assignee: set according to team workflow.

---

## Quick reference — UI elements and artifacts

- Application Map — locate services showing errors/latency.
- Azure AI Monitor — request-level deep analysis dashboard.
- Tracing / AI Foundry — use trace ID / span ID to correlate single request traces and logs.
- Logs — check for error messages and stack traces.

---
