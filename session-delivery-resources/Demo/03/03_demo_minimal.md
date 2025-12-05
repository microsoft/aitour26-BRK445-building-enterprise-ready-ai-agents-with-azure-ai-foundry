Video: [brk445-slide 19-demo.mp4](https://aka.ms/AAxrab6) — 00:06:46

1. Scope: Multi-agent demo showing different orchestration patterns (default, sequential, concurrent, handoff, group chat) implemented with Aspire and Semantic Kernel. [00:00:01 – 00:00:10]
2. Orchestration types: Default (coordinated), Sequential, Concurrent (parallel tasks), Handoff (router agent), Group chat (worker/reviewer) — choose based on desired coordination. [00:00:25 – 00:01:37]
3. Sample scenario: User query in store (e.g., "I can't find paint sprayer turbo price 750") launches inventory, matchmaking, location and navigation agents; the orchestration returns an orchestration ID and debug info. [00:00:46 – 00:02:42]
4. Concurrent vs Handoff: Concurrent mode runs agents in parallel but outputs may be incoherent; handoff uses a router agent to sequence/compose outputs more reliably. [00:01:22 – 00:04:00]
5. Implementation notes: Agents produce tasks and outputs are aggregated when finished; some services (e.g., navigation) may require JSON output improvements. [00:02:35 – 00:03:04]
6. Observability: Full traceability and logging via Aspire; use trace IDs and runtime logs to debug multi-agent coordination. [00:04:20 – 00:06:45]


Related guides:

[Full user manual](./03_demo_userguide.md)