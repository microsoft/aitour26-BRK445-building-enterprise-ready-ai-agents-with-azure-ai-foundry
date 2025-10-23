Video: [brk445-slide 14-demo.mp4](https://aka.ms/AAxrpqj) — 00:05:42

# Aspire + Semantic Kernel — Single-Agent Demo Guide

This guide documents the single-agent demo shown in the video. The demo showcases an Aspire orchestration built on .NET and Semantic Kernel, a storefront (Sava Labs), semantic search, an image-analysis workflow, and end-to-end observability with Aspire traces and Application Insights. Video length: 00:05:42.

---

## Overview

Demo highlights:
- The solution runs on .NET using the Aspire orchestration and Semantic Kernel framework. [00:00:05 – 00:00:10]
- Aspire provides a single entry point that connects components and business service endpoints to agents hosted in Azure AI Foundry. [00:00:18 – 00:00:25]
- The demo focuses on the single-agent flow (analysis, reasoning, inventory matching) and how traces reveal internal processing steps. [00:00:27 – 00:00:41]
- The front-end store (Sava Labs) demonstrates semantic search, embedding generation and vector search for queries such as "paint my room." [00:01:25 – 00:01:52]
- A photo analysis scenario shows an agent analyzing an image and producing a description; the thread logs expose the step-by-step processing and final recommendations. [00:02:10 – 00:03:03]
- Tracing and observability: Aspire traces demonstrate timings (e.g., 12s for image analysis, 4s to build thread) and are accessible in Application Insights for local and cloud deployments. [00:04:24 – 00:05:06]

---

## Step-by-step instructions

### 1) Start — Application & architecture context
Time: 00:00:05 – 00:00:25

Goal: Understand the runtime architecture: Aspire orchestration with a single entry point and agent providers backed by Semantic Kernel.

Steps:
1. Verify the application is running (local Docker / SQL Server) or deployed to cloud.
2. Open the Aspire orchestration UI if available and note the single entry point connecting services.
3. Identify business service endpoints that the single agent will use (e.g., customer info, product inventory, search).

Tip: Running a local SQL Server in Docker emulates a database for the demo environment.

---

### 2) Frontend store and semantic search
Time: 00:01:25 – 00:01:52

Goal: Explore the Sava Labs storefront and perform a semantic search.

Steps:
1. Open the storefront (Sava Labs) in the demo environment.
2. Browse products (paint, drill, circular saw) and note the search field.
3. Run a semantic query such as "paint my room" and observe embedding generation and vector search results.

Tip: Semantic search uses embeddings to return semantically relevant products and content — useful for natural-language queries.

---

### 3) Photo analysis scenario (agent run)
Time: 00:02:08 – 00:02:56

Goal: Demonstrate an end-to-end agent run that analyzes a photo and recommends tools.

Steps:
1. From the store, choose to analyze a photo (select an AI-generated image provided in the demo).
2. Trigger the analyze-photo action; this initiates background traces in Aspire.
3. Open the agent thread or agent playground to view analysis steps and intermediate results.
4. Observe the description output (e.g., "small living room with wooden flooring, white walls, ceiling").

Tip: Use thread logs to trace how the agent called external services like Azure AI Search and how the responses were combined into the final output.

---

### 4) Agent processing steps & reasoning
Time: 00:03:11 – 00:04:13

Goal: Inspect the agent’s stepwise behavior — analyze, fetch customer info, reason, and match inventory.

Steps:
1. In the thread logs, note the sequence of steps: Step 1 — analyze demands; Step 2 — retrieve customer information; Step 3 — reason about required tools; Step 4 — match tools to inventory and recommend purchases.
2. Review the intermediate outputs for each step to validate correctness (e.g., measuring tape already owned, recommended new tools).

Tip: The single-agent implementation is code-driven; you can see each step’s input and output in the logs.

---

### 5) Tracing, timing, and observability
Time: 00:04:24 – 00:05:06

Goal: Collect and review traces for performance analysis and debugging.

Steps:
1. Open Aspire traces or Application Insights to locate traces for the demo timeframe.
2. Identify key spans and timings (for example, image analysis duration ~12s, thread build ~4s).
3. Use trace IDs or timestamps to cross-reference logs and analytics across systems.

Tip: Application Insights allows you to inspect recent traces (last six hours or days) and correlate requests across services and the cloud.

---

## Suggested follow-up & verification template

Use this template to file a validation or bug ticket after reproducing the demo scenario:

- Title: Single-agent photo analysis — verify tool recommendations & inventory matching
- Environment: staging / (demo deployment)
- Timestamp: [e.g., 00:02:15 – 00:02:45]
- Steps to reproduce:
  1. Launch the storefront and select the photo analyze action for a sample image.
  2. Observe the agent thread and capture trace IDs and the sequence of steps in thread logs.
  3. Confirm the agent returns a description of the image and a recommended set of tools (including whether the user already has any tools).
- Expected result: Agent returns clear image description, lists required tools, and correctly matches items to inventory and suggested purchases.
- Actual result: [Describe outcomes; attach logs/traces/screenshots if errors or mismatches occur.]
- Attachments: thread logs, trace IDs, screenshots of UI and JSON output.
- Priority / Assignee: follow team process.

---

## Quick reference — UI elements and artifacts

- Aspire orchestration / Single entry point — connects services and agents.
- Sava Labs storefront — product list and search UI.
- Agent playground / thread logs — view agent runs and intermediate outputs.
- Aspire traces & Application Insights — collect trace IDs, timings, and cross-service telemetry.

---

