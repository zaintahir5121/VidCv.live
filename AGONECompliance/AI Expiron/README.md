# AI Expiron - Experion Cross-Product Bot POC

This folder contains the architecture and implementation guidance for the AG ONE conversational bot experience ("Experion") across:

- AG ONE Work
- AG ONE Learn
- AG ONE Safe
- AG ONE Hire
- AG ONE Spot
- AG ONE Sentiment

All documents in this folder are intended to be implementation-ready for your engineering team.

## Contents

- `experion_architecture.md` - end-to-end architecture, triggering model, lifecycle, security, scaling.
- `experion_api_contracts.md` - API contracts between frontend SDK and AG AI Hub.
- `experion_rollout_plan.md` - phased delivery plan with POC milestones and risks.
- `experion_architecture.drawio` - draw.io diagram you can open directly in diagrams.net.

## Key Decision

Implement Experion as a **central web SDK** injected into every AG ONE product, backed by **AG AI Hub** as the single orchestration and AI execution plane.

This gives one bot behavior, one security model, one audit trail, and one place to scale.
