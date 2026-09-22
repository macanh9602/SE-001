# PROJECT READINESS AUDIT

> Chạy sau khi Template được copy vào project, trước story đang được ROADMAP đánh dấu `EXECUTABLE`.
> Chỉ audit và auto-fix mechanical/reversible setup; không implement gameplay.

```text
PROJECT READINESS AUDIT

Đầu tiên phải tự phân loại project:
- `NEW_PROJECT`: repo/Assets còn là skeleton Template, chưa có identity hoặc gameplay project-specific.
- `EXISTING_PROJECT_ADOPTION`: đã có product name, namespace/code, asset/scene/module hoặc history project-specific.

Không được mặc định `NEW_PROJECT` chỉ vì prompt được chạy sau khi copy Template. Với
`EXISTING_PROJECT_ADOPTION`, audit phải inspect và harvest baseline hiện có; không bắt chạy lại
toàn bộ p1-bootstrap như một project trắng.

MỤC TIÊU:
- Kiểm tra project đã đủ workflow/tooling để chạy Story 001 Architecture Foundation Alignment chưa.
- Tách riêng Template/workflow readiness và Project-specific readiness.
- Không implement gameplay.
- Không tự quyết project-level decision còn thiếu.

ĐỌC THEO THỨ TỰ:
1. AGENTS.md
2. Docs/project-context.md
3. workflow/loop.md
4. workflow/verification.md
5. workflow/frontier-collaboration.md
6. playbooks/p1-bootstrap.md
7. handoff/ROADMAP.md
8. skills/README.md

AUDIT:

A. TEMPLATE INTEGRITY
- Kiểm tra AGENTS.md, Docs/, standards/, workflow/, skills/, playbooks/, templates/, handoff/.
- Kiểm tra dangling internal references.
- Chạy tools/template-lint.ps1 nếu tồn tại.
- Chạy git diff --check.
- Báo file/folder Template thiếu hoặc sai.

B. PROJECT IDENTITY
- Ghi `Project mode: 



 / EXISTING_PROJECT_ADOPTION` và evidence.
- Docs/project-context.md đã thay placeholder chưa.
- Internal name / namespace root / Unity version / render pipeline đã có chưa.
- Identity sentinel có đủ để phân biệt project này với project Unity khác chưa.
- Không cho Frontier READY nếu project identity vẫn placeholder/rỗng.

C. REQUIRED PROJECT CONTRACTS
Phân loại mỗi mục là PASS / MISSING / NEED DEV DECISION / N/A:
- Namespace root.
- Root scripts folder.
- Assembly/asmdef strategy.
- Level serialization.
- Async / tween / pool.
- Target device và frame budget.
- Physics authority / dimension / determinism nếu project dùng Physics.
- Gameplay/data source of truth.

Không tự chọn mục NEED DEV DECISION.

D. FRONTIER / C2C
Đọc Frontier collaboration mode trong Docs/project-context.md.

Nếu OFF: ghi N/A; không chạy provider discovery/preflight/frontier call.

Nếu AUTO hoặc REQUIRED:
1. Check capability/provider có tồn tại.
2. Check workspace đã setup/pair chưa.
3. Chạy provider status/preflight nếu khả dụng.
4. Verify provider map đúng current workspace.
5. Đọc AGENTS.md qua provider như readability sentinel.
6. Đọc Docs/project-context.md qua provider như identity sentinel.
7. Verify project identity match.
8. Verify branch/HEAD match nếu provider expose được.

Tách kết quả thành các dòng độc lập:
- `C2C provider`: capability/provider health.
- `Connector transport`: bridge/tunnel/auth.
- `Workspace mapping`: provider map đúng workspace hiện tại.
- `Workspace readability`: đọc được sentinel files.
- `Project identity`: PASS hoặc `PENDING BOOTSTRAP` nếu placeholder.
- `Frontier readiness`: READY / NOT READY.

Placeholder/rỗng trong project identity là project bootstrap issue, không phải provider failure.
Không chạy doctor/repair và không ghi `LOCAL FALLBACK` cho riêng lỗi này.

Chỉ khi provider/transport/mapping/readability thực sự fail mới chạy provider-native doctor/repair
một lần và retry đúng một lần.
- AUTO: nếu provider failure vẫn còn, ghi `LOCAL FALLBACK` + reason.
- REQUIRED: nếu provider failure vẫn còn, ghi `BLOCKED`.
- Identity `PENDING BOOTSTRAP`: giữ Frontier `NOT READY`, không đổi thành `LOCAL FALLBACK`.

Không yêu cầu dev nhập connector name.

E. UNITY TOOLING
- Kiểm tra Unity project có mở/đọc được không.
- Unity version có match project-context không.
- Unity MCP/plugin/session có available không.
- Kiểm tra package/asmdef compile state nếu có thể.
- Không claim compile PASS nếu chưa chạy Unity.

F. GIT / REPO
- Có Git repo không; branch hiện tại.
- Working tree có unexpected change từ trước bootstrap không.
- .gitignore có bỏ Unity Library/, Temp/, Logs/, obj/ đúng không.
- Không tự commit.

G. WORKFLOW READINESS
Kiểm tra ROADMAP, templates/story.md, Story frontier default = INHERIT,
implementation-notes template, verification + harvest + Story Closure workflow,
debug-audit route, và migration history không cần thiết.

H. BOOTSTRAP STATE
Với `NEW_PROJECT`, đánh giá toàn bộ p1-bootstrap: bước đã PASS, chưa làm, cần dev trả lời,
và agent tự xử lý được.

Với `EXISTING_PROJECT_ADOPTION`, thay bằng adoption baseline:
- inferred identity/architecture/contracts;
- existing compile issues và evidence;
- contracts đã harvest được;
- contracts cần dev confirm;
- phần p1-bootstrap thực sự còn thiếu.

Không báo `Bootstrap: 0/20` như blocker tổng quát nếu project đã có baseline; báo riêng
`Bootstrap baseline` và `Adoption gaps`.

AUTO-FIX được:
- Placeholder path sai rõ ràng.
- Missing generated folder/file từ Template.
- Broken internal reference.
- Formatting/lint issue.
- Generic setup không cần Product Owner decision.

KHÔNG auto-fix:
- Gameplay semantics, architecture contract, serialization choice.
- Assembly/asmdef strategy nếu có trade-off.
- Target device/frame budget.
- Frontier mode nếu dev chưa chọn.

OUTPUT CUỐI:

PROJECT READINESS
Project mode: NEW_PROJECT / EXISTING_PROJECT_ADOPTION
Template integrity: PASS / ...
Project identity: PASS / ...
Project contracts: PASS / ...
C2C provider: PASS / ...
Connector transport: PASS / ...
Workspace mapping: PASS / ...
Workspace readability: PASS / ...
Project identity gate: PASS / PENDING BOOTSTRAP / ...
Frontier readiness: READY / NOT READY / N/A
Provider failure: NONE / LOCAL FALLBACK / BLOCKED / N/A
Unity tooling: PASS / PARTIAL / ...
Git: PASS / ...
Workflow: PASS / ...
Bootstrap/adoption: X/Y complete hoặc baseline + N adoption gaps

READY FOR ACTIVE STORY: YES / NO

Nếu NO: chỉ liệt kê blockers theo thứ tự cần xử lý.
Nếu cần Dev quyết: hỏi tối đa 2–4 câu multiple-choice, mỗi câu có đúng một option recommend
và trade-off ngắn.
Nếu YES, nói chính xác:
"Project workflow ready. Có thể chạy Story 001 Architecture Foundation Alignment."
```

## Cách dùng

1. Copy Template vào Unity project.
2. Mở project trong Codex.
3. Yêu cầu agent chạy file này.
4. Agent audit và auto-fix mechanical setup.
5. Dev trả lời blocker/decision còn thiếu.
6. Chỉ sau khi `READY FOR ACTIVE STORY: YES` mới chạy story được ROADMAP đánh dấu `EXECUTABLE`.
