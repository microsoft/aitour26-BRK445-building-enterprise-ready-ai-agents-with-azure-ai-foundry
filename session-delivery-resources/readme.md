## How to deliver this session

🥇 Thanks for delivering this session!

Prior to delivering the workshop please:

1. Read this document and all included resources included in their entirety.
2. Watch the video presentation
3. Ask questions of the content leads! We're here to help!

## 📁 File Summary

| Resource | Link | Type | Description |
|---|---|---:|---|
| Session Delivery Deck | [Deck](https://aka.ms/AAxri1f) | External | Main slide deck for the session |
| Full Session Recording | [Recording](https://youtu.be/AwKSBaA2HXQ) | External | Full train-the-trainer recorded session |
| Demo source code | [`/src` demo source](../src) | Internal | Demo source code used in the live demos |
| Prerequisites | [`Prerequisites`](./docs/Prerequisites.md) | Internal | Tooling and access required to run the demos |
| Cloud Resources | [`Needed Cloud Resources`](./docs/02.NeededCloudResources.md) | Internal | Cloud resources required to run the demos |
| How to run demo locally | [`HowToRunDemoLocally`](./docs/HowToRunDemoLocally.md) | Internal | Step-by-step instructions to build and run the demo locally |

## 🚀Get Started

The workshop mixes short live demos with recorded segments for reference.

### 🕐Timing

> Note: Times are approximate. Use the `Mode` guidance to decide whether to run a demo live or play the recorded fallback.

| Time | Segment | Mode | Notes |
|---:|---|---|---|
| 07 mins | Introduction & content | Live | Presenter: lead — slides 1–3 |
| 06 mins | Demo — AI Foundry Agents | Live (recorded fallback) | [Recorded demo](https://aka.ms/AAxri1g) |
| 04 mins | Content | Live | Key point: agent overview |
| 06 mins | Demo — Aspire + Single Agent | Recorded (recommended) | [Recorded demo](https://aka.ms/AAxrpqj) |
| 03 mins | Content | Live | Transition & Q&A |
| 07 mins | Demo — Multi-Agent Orchestration | Live (recorded fallback) | [Recorded demo](https://aka.ms/AAxrab6) |
| 04 mins | Content | Live | Observability & tracing |
| 02 mins | Demo — Azure Monitor & Diagnostics | Recorded | [Recorded demo](https://aka.ms/AAxrpqk) |
| 04 mins | Content / Q&A | Live | Wrap-up & next steps |

### 🏋️Preparation (presenter quick-check)

Purpose: a short, actionable checklist to get a presenter ready for a live session. The repo contains more detailed setup steps in `./docs/01.Installation.md` — use the checklist below as the final pre-session verification.

Pre-session checklist (30–60 minutes before)

- [ ] Clone the repository (or pull latest if already cloned)
- [ ] Install prerequisites — follow `session-delivery-resources/docs/Prerequisites.md`
- [ ] Deploy required cloud resources (see `session-delivery-resources/docs/02.NeededCloudResources.md`) or confirm they already exist
- [ ] Follow the instructions in `session-delivery-resources/docs/HowToRunDemoLocally.md` to run the demo locally and verify health endpoints

Fallback & recording guidance

- If a live demo fails (service doesn't start, index not ready, or external resource is inaccessible) — play the recorded demo clip for that segment and mark the runbook with the issue.
- For fragile demos (search/indexing, external APIs), prefer the recorded fallback during high-risk sessions.

### 🖥️Demos

All demos reference the userguide files in the `session-delivery-resources/Demo/` folder. For each demo, use the referenced minimal userguide for talking points. If a demo is fragile or long-running, prefer a recorded segment.

| Demo | Link | Type | Mode | Short description |
|---:|---|---:|---|---|
| 01 | [AI Foundry Agents](./Demo/01/01_demo_minimal.md) | Internal | Live (recorded fallback) | Agent/playground management; create/configure agents; demonstrate inventory agent returning structured JSON. |
| 02 | [Aspire + Semantic Kernel](./Demo/02/02_demo_minimal.md) | Internal | Recorded (recommended) | Single-agent flow using Aspire + Semantic Kernel: semantic search and image analysis workflow. |
| 03 | [Multi-Agent Orchestration](./Demo/03/03_demo_minimal.md) | Internal | Live (recorded fallback) | Orchestration patterns (sequential, concurrent, handoff); triggers across inventory, matchmaking, location, navigation services. |
| 04 | [Azure Monitor & AI Foundry Diagnostics](./Demo/04/04_demo_minimal.md) | Internal | Recorded | Use Azure Monitor and AI Foundry diagnostics to locate and investigate model/service issues. |
