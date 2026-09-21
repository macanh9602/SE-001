# RUN STORY PROMPT — generic launcher / fallback

> Story mới nên có sẵn `## 12. Implementation handoff`.
> File này dùng khi cần một prompt generic để chạy story cũ hoặc khi host không tiện đọc embedded handoff.
> Story chưa viết ⇒ viết trước bằng `templates/story.md`.

---

```text
Chạy:
handoff/story-XXX-<slug>/story.md

Ưu tiên đọc và làm theo section:
`## 12. Implementation handoff`

Nếu story cũ chưa có section đó, dùng fallback sau:

ĐỌC:
1. AGENTS.md
2. Docs/project-context.md
3. standards/system-design.md
4. standards/code-style.md
5. story.md
6. Skill story chỉ định
7. standards/anti-patterns.md nếu story chạm vùng từng có bug
8. workflow/frontier-collaboration.md nếu story/project bật Frontier collaboration

Không scan toàn project.

WORKER MODE:
- Story rõ, không blocker → implement ngay; không chờ confirm, không re-plan.
- Decision local/reversible, không đổi contract/gameplay/data/scope → tự quyết + ghi notes.
- Chỉ escalate theo `AGENTS.md` khi chạm project architecture, gameplay semantics, serialization/data compatibility, hard performance budget, hoặc story scope.
- Không hardcode tunable.
- Không CreatePrimitive / new Material / Shader.Find trong runtime path.
- Pooling theo project contract.
- Vừa code vừa update `implementation-notes.html`.
- Không claim PASS nếu chưa chạy.

FRONTIER COLLABORATION:
- Story override `INHERIT / AUTO / OFF / REQUIRED`; `INHERIT` hoặc thiếu field dùng default trong `Docs/project-context.md`.
- Provider tự map current workspace → exact connector; không yêu cầu dev nhập connector name.
- Full preflight trước frontier call đầu tiên; lightweight preflight trước REVIEW.
- Sau một doctor/repair + retry: AUTO → local fallback có reason; REQUIRED → BLOCKED.
- Executor vẫn là current Codex session/model; không auto-switch model.

Nếu story có `worker/` và execution mode là AUTONOMOUS_PACKETS:
- chạy packet theo thứ tự;
- self-verify sau mỗi packet;
- PASS + Semantic deviations = NONE → tự chạy tiếp;
- không chờ human approval giữa packet.

Trước khi báo xong:
- điền Verification bằng evidence thật;
- inspect diff;
- kiểm acceptance;
- chạy Harvest.

CLOSE STORY:
- follow the Story closure section if present;
- update ROADMAP only after required gates pass;
- if any required gate is pending/fail/partial, do not mark DONE.

BÁO CÁO:
- Observable result
- Files changed
- Verification
- Acceptance status
- Semantic deviations
- Open risks/debt
- Harvest
- Roadmap update
- Final story status: DONE / DOING / BLOCKED
```

---

## Khi nào dùng mode nào

| Mode | Dùng khi |
|---|---|
| `DIRECT` | Story S/M, contract rõ, implementation local |
| `AUTONOMOUS_PACKETS` | Story L, migration, high-risk, lower-model worker cần locality |
| `SINGLE_PACKET` | Debug/risk cao, frontier model/dev muốn cô lập một packet |
