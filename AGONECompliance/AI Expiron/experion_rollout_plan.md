# Experion Rollout Plan (POC -> Production)

## 1) POC Objective

Prove that a user can complete a real journey with minimal manual UI effort by using Experion across one AG ONE product first, while architecture stays reusable for all six products.

## 2) Recommended POC Product & Journey

Choose one high-value journey with clear API steps.

Example:
- Product: AG ONE Hire
- Journey: "Create hiring workflow and publish job"

Why:
- Multi-step enough to validate orchestration.
- Safe to gate with confirmation before write operations.
- Easy to measure completion time reduction.

## 3) Scope by Phase

### Phase 0 - Foundations (1-2 weeks)
- Create central frontend SDK package (JS/TS).
- Add overlay launcher + circle/selection capture.
- Integrate SDK into one product shell.
- Create AG AI Hub endpoints:
  - `POST /api/experion/sessions`
  - `POST /api/experion/messages`
  - `POST /api/experion/actions/confirm`
  - `GET /api/experion/sessions/{id}/events`
- Add JWT validation and product allowlist.
- Add structured audit logs.

Exit criteria:
- Bot can open from user action and send/receive messages.
- Session, intent, and logs are persisted.

### Phase 1 - Context + Guided Assistance (2-3 weeks)
- Add auto-trigger rules engine (route + DOM anchor + role).
- Add context extraction from:
  - route
  - selected/circled area
  - current form values (authorized only)
- Implement read-only guidance actions.
- Add confidence gating and fallback messaging.

Exit criteria:
- Bot auto-appears on configured pages.
- Bot references page context correctly.
- User can dismiss and continue manually.

### Phase 2 - Controlled Action Execution (2-4 weeks)
- Add action registry + policy engine.
- Enable low-risk actions with explicit confirmation.
- Add rollback hooks for reversible operations.
- Add progress timeline in UI.

Exit criteria:
- User can complete one end-to-end journey via Experion.
- Critical actions always require confirmation.
- All actions are auditable.

### Phase 3 - Multi-Product Expansion (3-6 weeks)
- Integrate SDK into remaining AG ONE products.
- Add product-specific context adapters.
- Standardize telemetry dashboards.
- Optimize latency and caching for shared context.

Exit criteria:
- Unified bot behavior across all products.
- Shared governance and monitoring in AG AI Hub.

## 4) Non-Functional Requirements

- Availability: AG AI Hub target >= 99.9%.
- P95 response latency:
  - Initial response <= 2.5s (non-action).
  - Action submit acknowledgement <= 1.5s.
- Security:
  - Signed JWT + tenant/workspace claims.
  - Product allowlist.
  - Data scope enforcement.
- Observability:
  - Distributed trace id across SDK and Hub.
  - Action-level audit events.

## 5) Key Metrics

Primary:
- `% assisted journey completion`
- `average completion time (assisted vs manual)`
- `drop-off rate delta`

Secondary:
- `confirmation acceptance rate`
- `fallback-to-manual rate`
- `support tickets per journey`

## 6) Risks & Mitigations

1. Over-triggering / intrusive bot
- Mitigation: trigger cooldown, per-page frequency cap, user mute.

2. Wrong intent classification
- Mitigation: confidence threshold + clarifying question + safe fallback.

3. Security abuse from non-AG origins
- Mitigation: signed auth, origin allowlist, nonce, rate limiting.

4. Latency on complex tasks
- Mitigation: staged responses, async job model, timeline updates.

5. Low trust due to opaque behavior
- Mitigation: explain each action, show data sources, require confirmation.

## 7) Team Ownership

- Product Frontend Team:
  - SDK embedding + UI integration + context adapters.
- AG AI Hub Team:
  - Orchestration, intent/action engine, policy, audit.
- Platform/SRE:
  - scaling, observability, incident response.
- Security/Compliance:
  - approval of permission model and audit retention.

## 8) Deliverables Checklist

- [ ] Central SDK package ready.
- [ ] AG AI Hub Experion APIs deployed.
- [ ] Draw.io architecture diagram validated.
- [ ] Action policy matrix approved.
- [ ] Audit schema and dashboards live.
- [ ] POC journey completed in production-like environment.
