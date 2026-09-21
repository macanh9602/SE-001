# Frontier collaboration

> Generic runtime contract cho C2C/frontier collaboration. Project lưu policy, không lưu connector name.

## 1. Inputs và ownership

- Project defaults: `Docs/project-context.md §Frontier collaboration`.
- Story override: `Frontier collaboration: INHERIT / AUTO / OFF / REQUIRED`.
- `INHERIT` hoặc không có story field → dùng project default.
- Executor luôn là current Codex session/model; không auto-switch model.
- Connector selection là provider-managed: current workspace → mapped connector → identity verification.
- Provider-specific operations route qua adapter trong `workflow/providers/`.

Không ghi `Connector = ...` vào project, story hoặc prompt. Đổi connector/tunnel không được buộc sửa project config.

## 2. Modes

| Mode | Behaviour |
|---|---|
| `INHERIT` | Story-only value; resolve thành project default trước khi routing. |
| `AUTO` | Router gọi frontier khi Story L, architecture/high-risk hoặc review có giá trị; preflight fail sau retry thì local fallback và ghi reason. |
| `OFF` | Tuyệt đối local; không chạy provider discovery/preflight/frontier call. |
| `REQUIRED` | Story không chạy frontier-less; preflight/call fail sau retry thì `BLOCKED`. Chỉ hỏi user khi cần auth/consent. |

## 3. Routing và budget

- PLAN: tối đa 1 lần cho mỗi Story L/high-risk, trước implementation; không gọi lại ở từng packet.
- REVIEW: tối đa 1 lần sau local verification.
- Nếu REVIEW trả `PLAN`, được thêm một implementation/review iteration có scope rõ.
- Source, diff và log đi qua connector read-only access; không paste bundle vào prompt.
- Retry = 1. `AUTO` fallback và `REQUIRED` block là generic workflow contract, không phải project input.

## 4. Full preflight

Chạy trước frontier call đầu tiên của story:

1. Từ current Codex workspace, lấy expected repo/workspace root, project identity/internal name trong local `Docs/project-context.md`, và branch/HEAD khi Git cung cấp được.
2. Yêu cầu provider map current workspace tới candidate connector.
3. Qua provider adapter, kiểm provider, tunnel và auth status.
4. Gọi `workspace_info` trên candidate connector; repo/workspace phải khớp current Codex workspace.
5. Đọc `AGENTS.md` trên connector làm readability sentinel.
6. Đọc `Docs/project-context.md` trên connector làm identity sentinel; internal name/project identity phải khớp expected local project.
7. So khớp branch/HEAD khi available. Bất kỳ workspace hoặc identity mismatch nào = `FAIL` trước PLAN/REVIEW.
8. Phân loại failure trước khi repair:
   - provider/bridge/tunnel/auth/workspace mapping/readability failure → provider failure;
   - placeholder/rỗng hoặc project identity mismatch → project identity gate failure.
9. Chỉ provider failure mới chạy provider-native doctor/repair một lần rồi retry đúng một lần.

Không yêu cầu user nhập runtime state mà Codex/C2C tự lấy được.
Internal name còn placeholder/rỗng thì identity sentinel chưa hợp lệ: `Project identity = PENDING BOOTSTRAP`
và `Frontier readiness = NOT READY`. Đây không phải connector/provider failure, nên không chạy doctor
và không ghi `LOCAL FALLBACK`. Khi cần frontier, `AUTO` chỉ fallback nếu provider failure thực sự xảy ra;
`REQUIRED` chỉ block do provider failure hoặc do identity gate khi story yêu cầu frontier.

## 5. Review preflight

Trước frontier REVIEW, chạy lightweight check:

- mapped connector vẫn là workspace và project identity đã pass full preflight;
- provider/tunnel/auth vẫn healthy;
- workspace identity và sentinel vẫn đọc được;
- branch/HEAD chưa lệch ngoài expected story state.

Fail → áp dụng policy `AUTO` hoặc `REQUIRED`; không âm thầm review nhầm workspace.

## 6. Failure policy

- `AUTO`: provider failure → doctor/repair → retry 1 lần → vẫn fail thì tiếp tục local, ghi `LOCAL FALLBACK` + reason.
- `REQUIRED`: provider failure → doctor/repair → retry 1 lần → vẫn fail thì `BLOCKED`; chỉ hỏi user nếu cần auth/consent hoặc external state change.
- `OFF`: không có failure policy vì không gọi frontier.

## 7. Provider adapter

Sau khi resolve provider từ project context, đọc adapter tương ứng. Với default provider, dùng
`workflow/providers/codex-with-chatgpt.md`.

## 8. Evidence

Ghi ngắn trong `implementation-notes.html`:

```text
Frontier Preflight
Provider: codex-with-chatgpt
Mode: AUTO
Project mode: EXISTING_PROJECT_ADOPTION
C2C provider: PASS
Connector transport: PASS
Workspace mapping: PASS
Workspace readability: PASS
Project identity: PASS
Branch: main
HEAD match: PASS
Frontier readiness: READY
Provider failure: NONE
```

Provider failure fallback example:

```text
Connector transport: FAIL
Repair: attempted
Retry: FAIL
Mode: AUTO
Provider failure: LOCAL FALLBACK
Reason: connector workspace unreadable
```

Identity pending example:

```text
Project mode: EXISTING_PROJECT_ADOPTION
C2C provider: PASS
Connector transport: PASS
Workspace mapping: PASS
Workspace readability: PASS
Project identity: PENDING BOOTSTRAP
Frontier readiness: NOT READY
Provider failure: NONE
Reason: Docs/project-context.md still contains placeholder identity
```

Evidence phải là runtime result thực tế. Không ghi `PASS` nếu command/call chưa chạy.

## 9. Runtime acceptance cho provider/connector changes

Trước khi release thay đổi routing, mapping hoặc preflight, chạy và record:

| Case | Expected |
|---|---|
| Project A khi nhiều connector đang connected | provider map đúng connector A; workspace + identity sentinels PASS |
| Cố tình dùng connector B | identity mismatch; FAIL trước PLAN/REVIEW |
| Tunnel/bridge unhealthy | provider-native doctor/repair chạy rồi retry đúng một lần |
| Vẫn fail ở `AUTO` | `LOCAL FALLBACK` + reason |
| Vẫn fail ở `REQUIRED` | `BLOCKED`; chỉ hỏi user nếu cần auth/consent |

Không giả lập PASS bằng textual inspection. Thiếu connector thứ hai hoặc không thể làm tunnel unhealthy an toàn thì ghi `PENDING` + lý do và không gọi feature DONE.
