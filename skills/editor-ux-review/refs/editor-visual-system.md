# Mini editor visual system

## Control roles

- Primary action: Save hoặc Play Test của workflow hiện tại.
- Secondary action: Open, duplicate, fit, hoặc action hỗ trợ.
- Danger action: delete, discard, overwrite; tách khỏi primary.
- Toolbar tool toggle: mode/tool có active state rõ.
- Icon action: chỉ dùng cho action đã quen thuộc và có tooltip.
- Section header: nhóm field theo task, không theo class.
- Inline validation: đặt ngay dưới field/target.
- Status badge: dirty, valid, warning, blocking, saved.
- Empty state: nói vì sao rỗng và bước tiếp theo.

## Semantic roles

`primary`, `active`, `success`, `warning`, `danger`, `info`, `muted`.

## Emphasis tiers

- High: canvas, task hiện tại, Save/Play Test, blocking state.
- Normal: field và action authoring thường dùng.
- Low: raw ID, metadata, advanced hoặc action hiếm.

## Frequency tiers

- Always visible: global actions và trạng thái document.
- Contextual: tool options và field của entity đang chọn.
- Advanced/collapsible: raw data, debug metric và tuning hiếm.

## 13 rules

1. Save, Play Test và blocking status không được biến mất.
2. Primary nổi bật hơn secondary.
3. Danger không đặt sát primary nếu không có khoảng cách.
4. Icon-only chỉ dành cho action hiển nhiên.
5. Action quan trọng dùng icon + label.
6. Active tool và selected entity phải nhận ra ngay.
7. Advanced/raw field không lấn inspector chính.
8. Canvas là trọng tâm thị giác.
9. Màu semantic truyền state, không dùng để trang trí.
10. Tránh nhiều accent color cạnh tranh.
11. Disabled control nói rõ lý do qua tooltip hoặc status.
12. Empty state nói bước tiếp theo.
13. Tooltip chứa action và shortcut khi shortcut tồn tại.

## Generic USS names

`.btn-primary`, `.btn-secondary`, `.btn-danger`, `.tool-toggle`, `.badge-warning`,
`.badge-blocking`, `.field-error`, `.empty-state`, `.status-dirty`.
