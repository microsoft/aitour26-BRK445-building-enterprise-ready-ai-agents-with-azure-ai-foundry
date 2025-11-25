Video: [brk445-slide 08-demo.mp4](https://aka.ms/AAxri1g) — 00:05:29

# AI Foundry Agents — Demo Guide

This guide documents the demo walkthrough shown in the video. The demo demonstrates AI Foundry project features: agent/playground management, agent creation and configuration, external data connectors, running agents in the playground, and related management, security and tracing capabilities. Video length: 00:05:29.

---

## Overview

Demo highlights:
- Accessing an AI Foundry project and its playgrounds (chat, video, agent playgrounds). [00:00:02 – 00:00:09]
- Reviewing the agents list (multi-agent orchestrator, navigation agent, product/inventory agents) and existing templates. [00:00:15 – 00:00:29]
- Creating an agent: name, model selection (OpenAI GPT, Meta, etc.), and system prompts. [00:00:30 – 00:00:39]
- Grounding agents with external data sources (Azure AI Search, SharePoint, Bing) and third-party integrations (OpenAPI-compatible services). [00:00:52 – 00:01:12]
- Enabling agent actions (run code / code interpreter, call Azure Functions or Logic Apps). [00:01:16 – 00:01:58]
- Running an example inventory agent in the playground and returning structured JSON output after calling Azure Search and other services. [00:02:34 – 00:03:48]
- Management, security and observability: management center, user access, connected resources, tracing, and trust & safety features (guardrails, content filters). [00:03:59 – 00:05:27]

---

## Step-by-step instructions

### 1) Start — Open the AI Foundry project and playgrounds
Time: 00:00:02 – 00:00:09

Goal: Access the project and identify available playgrounds (chat, video, agent).

Steps:
1. Open the AI Foundry project in the portal.
2. Check the playground options: chat playground, video playgrounds, and agent playgrounds.
3. Confirm you can list and open existing agents.

Tip: Playground environments let you quickly test agent behaviors without deploying them.

---

### 2) Agents list and templates
Time: 00:00:15 – 00:00:29

Goal: Review available agents and understand the templates and orchestrators present.

Steps:
1. Navigate to the agents list and review entries (e.g., multi-agent orchestrator, navigation, inventory agents).
2. Note which templates or agents exist and their intended purpose.

Tip: Use existing templates as starting points to create specialized agents.

---

### 3) Create an agent: name, model, and system prompt
Time: 00:00:30 – 00:00:39

Goal: Create a basic agent by defining its name, selecting a model, and adding a system prompt/instructions.

Steps:
1. Click "Create agent" and enter a descriptive name.
2. Choose a model to use (e.g., GPT from OpenAI, or other supported models).
3. Provide a system prompt or instructions to define agent behavior.

Tip: System prompts should be concise and specific to control agent behavior.

---

### 4) Grounding and data connectors (Search, SharePoint, Bing)
Time: 00:00:52 – 00:01:12

Goal: Connect the agent to external data sources so it can ground responses on your data.

Steps:
1. Configure connectors to Azure AI Search, SharePoint, or other data sources.
2. Optionally enable Bing grounding to augment responses with web results.
3. Test the connector with a sample query to confirm successful integration.

Tip: Grounding improves accuracy by allowing agents to retrieve authoritative data from connected sources.

---

### 5) Integrations and actions (3rd-party APIs, code execution)
Time: 00:01:12 – 00:01:58

Goal: Enable agent actions such as calling OpenAPI-compatible third-party services, running code, or invoking Azure Functions/Logic Apps.

Steps:
1. Add third-party integrations that support OpenAPI 3 if needed (e.g., travel/review APIs).
2. Enable a code interpreter or action runner to allow the agent to execute Python or other code for computation tasks.
3. Configure Azure Functions or Logic Apps connections for server-side actions.

Tip: Actions let agents perform operations (data manipulation, calculations, remote calls) and incorporate the results into responses.

---

### 6) Run the inventory agent in the playground (example)
Time: 00:02:34 – 00:03:48

Goal: Execute a sample prompt against the inventory agent to retrieve customer and product data and return structured JSON.

Steps:
1. Open the inventory agent in the agent playground.
2. Use the provided sample prompt (e.g., search for customer "John Smith").
3. Observe how the agent analyzes the request, calls Azure AI Search, and returns a JSON output containing relevant fields (e.g., customer info, product lists).
4. Inspect the thread logs to see the message flow: initial message → analysis → external call → final JSON response.

Tip: Request JSON output for easier automated validation and parsing.

---

### 7) Management, security, and tracing
Time: 00:03:59 – 00:05:27

Goal: Use the project management center to manage users, connected resources, and to enable tracing and observability for agents and models.

Steps:
1. Open the project management center to review and assign user permissions.
2. Check connected resources (Logic Apps, Azure AI Search, etc.) and verify their configurations.
3. Configure Microsoft Entra ID (Azure AD) for authentication if required.
4. Access tracing capabilities to collect traces for applications, models, and agents for later analysis.

Tip: Enable tracing during testing to capture detailed interactions, HTTP requests, and database calls for troubleshooting.

---

## Suggested follow-up & verification template

Use this template when creating verification tickets after testing an agent or integration:

- Title: Inventory agent — grounding and output validation (example: "John Smith")
- Environment: staging / (deployment)
- Timestamp: [e.g., 00:02:56 – 00:03:24]
- Steps to reproduce:
  1. Open AI Foundry → Agents → Inventory agent → Playground.
  2. Run the sample prompt provided in the demo (search for "John Smith").
  3. Confirm the agent calls Azure AI Search and returns structured JSON.
  4. Inspect thread logs and traces for the request (trace ID / span ID) and collect relevant logs.
- Expected result: Agent returns well-formed JSON with expected fields and correct data from the connected sources.
- Actual result: [Describe observed output: missing fields, incorrect data, errors or timeouts. Attach logs/traces/screenshots.]
- Attachments: screenshot(s), trace IDs, sample JSON output, logs.
- Priority / Assignee: set according to team workflow.

---

## Quick reference — UI elements and artifacts

- Project Playgrounds — chat, video, and agent playgrounds (for testing agents).
- Agents list — review existing agents and templates.
- Agent Playground — run sample prompts and inspect outputs.
- Management Center — user access, connected resources, and permissions.
- Tracing & Logs — collect trace data (trace IDs / span IDs) and logs for diagnostics.

---

