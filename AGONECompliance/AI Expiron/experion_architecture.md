# Experion Architecture (POC + Scalable Target)

## 1) Objective

Enable any AG ONE product page to trigger a conversational bot ("Experion") by context (including user circle gesture trigger), then guide/execute user journeys through AG AI Hub safely and transparently.

Products in scope:

- AG ONE Work
- AG ONE Learn
- AG ONE Safe
- AG ONE Hire
- AG ONE Spot
- AG ONE Sentiment

---

## 2) Recommended Architecture (What to Build)

### 2.1 Frontend: Central Experion SDK

Build one centrally hosted JavaScript SDK (for example `experion-sdk.js`) that all six Blazor WASM apps load.

Responsibilities:

1. Render bot launcher, panel, conversation UI, status indicators.
2. Capture page context (product, route, selected section, metadata).
3. Detect trigger events:
   - Explicit button click.
   - User-drawn circle gesture over page region.
   - Rule-based proactive trigger (optional, low-frequency).
4. Send requests to AG AI Hub with signed context token.
5. Display responses, confirmations, step-by-step progress, audit events.

Critical note:

- Keep UI/CSS/templates in SDK package assets (JS + CSS + optional HTML template bundle), not dynamically generated per app.
- Version SDK and load by immutable URL path (for example `/v1.2.0/experion-sdk.js`), never overwrite in-place.

### 2.2 Backend: AG AI Hub as Control Plane

AG AI Hub is the only backend that the SDK calls for AI operations.

Responsibilities:

1. Authenticate and authorize product-originated bot sessions.
2. Validate product, tenant/workspace, user, permissions, consent.
3. Classify intent and route to journey orchestrator.
4. Execute actions via product APIs under permission guardrails.
5. Integrate model services:
   - GPT-4.1
   - Azure Document Intelligence
   - Azure AI Search
6. Persist:
   - conversation state
   - action logs
   - audit trail
   - user approvals

### 2.3 Data & Infra Model

- SQL Server (shared instance): one DB, separate schema per product + `aihub` schema for shared bot runtime data.
- Blob Storage:
  - `experion-prompts/`
  - `experion-session-artifacts/`
  - large transcript attachments
  - optional replay snapshots
- AI Search:
  - centralized index aliases with per-product filters
  - document and KB retrieval for contextual answers

### 2.4 Security

Use short-lived signed token flow:

1. Product backend issues signed `experion_context_token` (JWT).
2. SDK includes token on every AG AI Hub call.
3. AG AI Hub validates:
   - signature
   - issuer
   - audience
   - nonce/session ID
   - expiry (5-10 min)
   - product ID and allowed capabilities

Never trust client-side product name/user identity directly.

---

## 3) Circle Trigger POC Design

## 3.1 Trigger UX

When user draws a circle on screen:

1. SDK captures bounded rectangle coordinates.
2. SDK extracts nearby DOM context:
   - heading text
   - labels
   - table row metadata
   - form field names
3. SDK opens Experion panel with prefilled prompt:
   - "I noticed you circled Billing > Payment Schedule area. How can I help?"

### 3.2 Context Payload to AG AI Hub

Include:

- `product`: `work|learn|safe|hire|spot|sentiment`
- `route`: current page path
- `workspaceId` / `tenantId`
- `uiContext`:
  - selected region coordinates
  - extracted text snippets
  - active entity IDs (if available)
- `sessionId`
- `contextToken`

### 3.3 LLM Prompt Layering

AG AI Hub constructs prompts from:

1. Global system prompt (safety + behavior policy).
2. Product-specific policy prompt.
3. Page-context snippet from SDK.
4. Retrieval context from AI Search/SQL/Blob.
5. User utterance.

---

## 4) Bot Lifecycle and Timing

Recommended defaults:

- Trigger response: show bot within 200-400ms after circle completion.
- Auto-hide launcher tooltip: 4s.
- Keep panel open: until user closes, no forced timeout while active.
- Idle timeout: 10 minutes then minimize (not close) with resume option.
- "Thinking" state visible if backend > 600ms.

---

## 5) User Control and Guardrails

For high-risk actions, require confirmation:

- "I will submit invoice batch #A-102. Confirm?"
- Optional second-step confirmation for financial/security actions.

Support hybrid mode:

- User can switch to manual flow any time.
- Bot must provide "Open page where I would do this manually" deep-link.

---

## 6) Scalability and Reliability Pattern

### 6.1 Runtime

- Run AG AI Hub stateless API on Azure App Service.
- Store long-running tasks in queue + worker:
  - Azure Service Bus / Storage Queue
  - background workers for action execution.

### 6.2 State

- Redis for short-lived session cache and conversation turn buffering.
- SQL Server for durable audit, approvals, executed actions.

### 6.3 Observability

Required telemetry:

- trigger source (`circle`, `manual`, `proactive`)
- intent classification latency
- action success/failure rate
- confirmation acceptance/decline rate
- fallback-to-manual rate

---

## 7) Database Schema Suggestion (`aihub`)

Core tables:

- `aihub.BotSession`
- `aihub.BotMessage`
- `aihub.BotActionPlan`
- `aihub.BotActionExecution`
- `aihub.BotApproval`
- `aihub.BotAuditEvent`
- `aihub.BotConsent`
- `aihub.BotTriggerEvent`

All records should include:

- `TenantId`
- `WorkspaceId` (if relevant)
- `ProductCode`
- `UserId`
- `CreatedAtUtc`
- `CorrelationId`

---

## 8) Integration Model for the Six Products

Each product adds only:

1. SDK script include in host page/layout.
2. Product adapter endpoint to mint `experion_context_token`.
3. Optional route metadata map (friendly page names and entity resolvers).

Everything else remains centralized in AG AI Hub.

---

## 9) Why Central SDK + AI Hub is Correct

Benefits:

- Single UX and behavior across all products.
- Lower maintenance cost than six independent bots.
- Stronger governance (audit, consent, data access control).
- Faster rollout of new bot features with SDK versioning.

Risks and mitigations:

- Risk: SDK becomes heavy -> Mitigate with lazy-loaded panel.
- Risk: Cross-product context leakage -> Strict tenant/product claims checks.
- Risk: Latency -> regional deployment + caching + streaming responses.

---

## 10) POC Scope Recommendation (First 3 Journeys)

Implement POC on one or two products first, then scale:

1. Onboarding setup journey.
2. Billing inquiry + payment action proposal.
3. Form completion flow with confirmation before submit.

Acceptance checks:

- User can complete journey conversationally.
- Critical actions require explicit confirmation.
- Full audit trail is written.
- User can switch to manual path at any step.
