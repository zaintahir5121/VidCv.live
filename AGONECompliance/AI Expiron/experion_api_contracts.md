# Experion API Contracts (POC)

This defines the minimal API surface between the central frontend bot SDK and AG AI Hub.

## 1) Session Bootstrap

Endpoint:

- `POST /api/experion/session/bootstrap`

Purpose:

- Validates caller and context.
- Creates/returns a short-lived bot session.
- Returns permitted capabilities and policies for this user + product + page.

Request:

- Headers:
  - `X-AGONE-Product`: `work|learn|safe|hire|spot|sentiment`
  - `X-AGONE-WorkspaceId`: tenant/workspace identifier
  - `Authorization`: AG ONE user bearer token
  - `X-AGONE-SDK-Signature`: HMAC signature (optional in Phase 1, required in Phase 2+)
- Body:
  - `pageUrl`
  - `pageTitle`
  - `screenContext`
  - `locale`
  - `userTimezone`

Response:

- `sessionId`
- `conversationId`
- `allowedActions`: list of action names (e.g., `billing.updatePlan`, `hire.createJob`)
- `requiresCriticalConfirmation`: true/false
- `consentFlags`
- `ttlSeconds`

## 2) Context Trigger (Circle Gesture / Manual Open / Auto Open)

Endpoint:

- `POST /api/experion/context/trigger`

Purpose:

- Sends event when bot is opened by circle gesture, manual hotkey, or auto trigger.

Request:

- `sessionId`
- `triggerType`: `circle|manual|auto`
- `pageUrl`
- `domContext`
- `selectionText`
- `boundingBox`: `{ x, y, width, height }`
- `screenshotRef` (optional)

Response:

- `detectedIntent`
- `confidence`
- `suggestedPrompts`
- `recommendedActions`
- `uiMode`: `compact|expanded`

## 3) User Message

Endpoint:

- `POST /api/experion/conversation/message`

Purpose:

- Sends user message and returns bot response + execution proposals.

Request:

- `sessionId`
- `message`
- `contextVersion`

Response:

- `assistantMessage`
- `explanation`
- `proposedActions[]`
- `missingInputs[]`
- `requiresConfirmation`

## 4) Action Execute (Guarded)

Endpoint:

- `POST /api/experion/action/execute`

Purpose:

- Executes approved action through AG AI Hub orchestration.

Request:

- `sessionId`
- `actionName`
- `actionPayload`
- `confirmationToken` (required for critical actions)
- `idempotencyKey`

Response:

- `executionId`
- `status`: `accepted|running|completed|failed|cancelled`
- `progressMessage`
- `undoToken` (if reversible)

## 5) Execution Status (Polling or SSE)

Endpoint:

- `GET /api/experion/action/{executionId}/status`
- Optional SSE: `GET /api/experion/action/{executionId}/stream`

Response:

- `status`
- `step`
- `stepCount`
- `lastUpdatedUtc`
- `auditRef`

## 6) Audit Trail Query

Endpoint:

- `GET /api/experion/audit?sessionId=...`

Purpose:

- Gives full trace for transparency, compliance, and support.

Response:

- Ordered events:
  - trigger event
  - intent detection
  - prompts used
  - actions proposed
  - user confirmations
  - API calls executed
  - outputs and reversals

## Error Model

Standard error response:

- `code`
- `message`
- `traceId`
- `isRetryable`
- `resolutionHint`

Codes:

- `EXPERION_UNAUTHORIZED_SOURCE`
- `EXPERION_PERMISSION_DENIED`
- `EXPERION_CONFIRMATION_REQUIRED`
- `EXPERION_DATA_CONSENT_MISSING`
- `EXPERION_ACTION_TIMEOUT`
- `EXPERION_ORCHESTRATION_FAILURE`

## Security Requirements

- Validate AG ONE user token on every request.
- Validate product source header against allow-list.
- Require short-lived session IDs.
- Enforce per-action permissions from policy engine.
- Log all action execution in audit store.

