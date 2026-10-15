# SDD ledger — plan: docs/plan.md
Execution: inline approved. Landing page and GitHub publication additionally authorized.
Isolation: fresh repository under work/meydan, develop branch; no existing code or user changes.
Pre-flight: tasks 1–5 share JS message protocol; native transports forward messages without interpreting rules.
Ruling: manager engine runs in the local UI on both platforms — identical tested rules and snapshots — requires keeping manager UI open.
Ruling: unavailable local skill helper scripts replaced by equivalent explicit task ledger and source packaging steps; no credentials persisted.
Task 1: started.
Task 1 engine: 16 public behavior tests RED (missing Engine/validator) → GREEN, including variable count, team consensus, deadlines, privacy, restore and double scoring.
Task 2 capacity: 5 RED → GREEN; initial limits differ from verified stable-device counts.
Ruling: Node test process isolation unavailable in sandbox; use --test-isolation=none, which preserves independent tests and assertions.
Task 1 UI: local Arabic RTL interface, editor/import/export, variable question selection and four-team demo implemented; browser integration pending.
Task 2 protocol: 7 behavior checks green, including heartbeat deadline broadcasts and manager impersonation rejection.
Task 3 build: Android debug APK built; FrameReader validates Arabic, malformed UTF-8, EOF, truncated and oversized frames. Native framing tests added after implementation; not claimed as test-first.
Task 4 build: desktop library compiles; real TCP integration test passes. Bluetooth compile failure traced to newer library PairedDevices API and corrected.
Task 5 relay: 3 RED→GREEN real WebSocket tests for room isolation, Arabic routing and rejected unauthorized routing.
Ruling: preserve complete Bluetooth assembly when trimming portable runtime to avoid dynamically accessed platform members being removed; hardware compatibility still requires device tests.
Final review: independent read-only reviewer found six Important issues; all entered one fix pass.
Final: fixed awarded buzz restore double scoring — failing restored-judge regression → green.
Final: fixed initial state ordering — joined now carries a sanitized snapshot/server time; failing regression → green.
Final: fixed host relay rate amplification — 16-player/160-message regression failed with dropped frames, then passed with host allowance proportional to room clients.
Final: fixed desktop persistence across random loopback origins — authenticated native StateStore bootstrap and atomic writes; fresh-origin regression included in 36/36 full suite.
Final: fixed stale peer cleanup — exact-instance removal and generation suppression; C# contract test RED (missing RemoveExact) → GREEN.
Final: Windows Bluetooth package4.2.5 selected stub provider; pinned4.1.44 with net8.0-windows7.0 for Windows publish. Runtime asset verification is required before packaging.
Verification: npm test 36/36 pass; real browser QA passes two TCP peers, private answers, doubled score, mobile layout, editor and31 questions; native C# peer contract passes.
Preview signing now retains a local private key/password under ignored artifacts/.signing-private for repeat builds. CI runners still need configured stable signing for updates across independent runs.
Ruling: Windows stores state in application data and hydrates it before UI initialization — random UI ports remain private without losing saves; test data directories are explicitly scoped to artifacts.
