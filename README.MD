# Microsoft AI Tour 2026 — BRK445

![AI Tour banner](img/Banner-MS-AI-Tour-26.png)

[![Microsoft Azure AI Foundry Discord](https://dcbadge.limes.pink/api/server/Pwpvf3TWaw)](https://discord.gg/Pwpvf3TWaw)
[![Azure AI Foundry Developer Forum](https://img.shields.io/badge/GitHub-Azure_AI_Foundry_Developer_Forum-blue?style=for-the-badge&logo=github&color=adff2f&logoColor=fff)](https://aka.ms/foundry/forum)

Azure AI Foundry Agent Service: Building enterprise-ready AI Agents with Azure AI Foundry

This repository contains the sample code, services, and materials used in BRK445: "Building enterprise-ready AI Agents with Azure AI Foundry" presented at Microsoft AI Tour 2026. The solution demonstrates patterns for designing, implementing, and deploying multi-service AI agents with governance, reuse, and responsible-AI considerations.

If you are delivering this session, check the `session-delivery-resources/` folder for slides, scripts, and demos.

## Quick links

- Source code: `src/`
- Session delivery resources: `session-delivery-resources/`

## What you'll learn

In this session you will learn practical techniques to:

- Design agent architectures that separate reasoning, tools, and orchestration
- Integrate Azure AI Foundry (Azure AI Studio) services with local microservices
- Add safety, evaluation, and observability to generative AI agents
- Package and run multi-service demos locally and in Azure

### Target audience

Developers, architects, and AI engineers who build production-grade agentic applications with Azure AI services and need guidance on patterns for security, governance, and reliability.

## What's in this repository

- `src/` — Example source code used by the demos (C#, .NET 9).  
- `session-delivery-resources/` — slides, demos, and presenter notes

## Technologies used

- C# / .NET 9 / .NET Aspire for orchestration
- Azure AI Foundry / Azure AI Foundry Agents
- Docker

### Agent Frameworks

This solution demonstrates integration with two agent frameworks:

1. **Semantic Kernel (SK)** - Microsoft.SemanticKernel - Default implementation
2. **Microsoft Agent Framework (AgentFx)** - Microsoft.Agents.AI - Alternative implementation

Both frameworks can connect to the same Azure AI Foundry agents. You can switch between them using the **Settings page** in the Store frontend application:

1. Navigate to **Settings** in the Store app
2. Use the toggle switch to select your preferred framework
3. Your selection is saved automatically and takes effect immediately

See [`src/readme.md`](./src/readme.md) for more details on the framework architecture.

## 🔗 Session Resources

Check the [Session Delivery Resources folder](./session-delivery-resources/) for the whole set of materials.

| Resource | Path |
|---|---:|
| Presenter Guide (full) | [session-delivery-resources/readme.md](session-delivery-resources/readme.md)
| Slides | [EN-US_BRK445_Tech_FY26.pptx](https://aka.ms/AAxri1f) |

## Content Owners

<table>
<tr>
    <td align="center"><a href="http://github.com/mikekinsman">
        <img src="https://github.com/elbruno.png" width="100px;" alt="Bruno Capuano"/>
        <br />
        <sub><b>Bruno Capuano</b></sub></a>
        <br />
        <a href="https://github.com/elbruno" title="talk">📢</a>
    </td>
    <td align="center"><a href="https://github.com/kinfey">
        <img src="https://github.com/kinfey.png" width="100px;" alt="Bruno Capuano"/>
        <br />
        <sub><b>Kinfey Lo</b></sub></a>
        <br />
        <a href="https://github.com/kinfey" title="talk">📢</a>
    </td>

</tr></table>

## Responsible AI

Microsoft is committed to helping our customers use our AI products responsibly, sharing our learnings, and building trust-based partnerships through tools like Transparency Notes and Impact Assessments. Many of these resources can be found at [https://aka.ms/RAI](https://aka.ms/RAI).
Microsoft’s approach to responsible AI is grounded in our AI principles of fairness, reliability and safety, privacy and security, inclusiveness, transparency, and accountability.

Large-scale natural language, image, and speech models - like the ones used in this sample - can potentially behave in ways that are unfair, unreliable, or offensive, in turn causing harms. Please consult the [Azure OpenAI service Transparency note](https://learn.microsoft.com/legal/cognitive-services/openai/transparency-note?tabs=text) to be informed about risks and limitations.

The recommended approach to mitigating these risks is to include a safety system in your architecture that can detect and prevent harmful behavior. [Azure AI Content Safety](https://learn.microsoft.com/azure/ai-services/content-safety/overview) provides an independent layer of protection, able to detect harmful user-generated and AI-generated content in applications and services. Azure AI Content Safety includes text and image APIs that allow you to detect material that is harmful. Within Azure AI Foundry portal, the Content Safety service allows you to view, explore and try out sample code for detecting harmful content across different modalities. The following [quickstart documentation](https://learn.microsoft.com/azure/ai-services/content-safety/quickstart-text?tabs=visual-studio%2Clinux&pivots=programming-language-rest) guides you through making requests to the service.

Another aspect to take into account is the overall application performance. With multi-modal and multi-models applications, we consider performance to mean that the system performs as you and your users expect, including not generating harmful outputs. It's important to assess the performance of your overall application using [Performance and Quality and Risk and Safety evaluators](https://learn.microsoft.com/azure/ai-studio/concepts/evaluation-metrics-built-in). You also have the ability to create and evaluate with [custom evaluators](https://learn.microsoft.com/azure/ai-studio/how-to/develop/evaluate-sdk#custom-evaluators).

You can evaluate your AI application in your development environment using the [Azure AI Evaluation SDK](https://microsoft.github.io/promptflow/index.html). Given either a test dataset or a target, your generative AI application generations are quantitatively measured with built-in evaluators or custom evaluators of your choice. To get started with the azure ai evaluation sdk to evaluate your system, you can follow the [quickstart guide](https://learn.microsoft.com/azure/ai-studio/how-to/develop/flow-evaluate-sdk). Once you execute an evaluation run, you can [visualize the results in Azure AI Foundry portal](https://learn.microsoft.com/azure/ai-studio/how-to/evaluate-flow-results).
