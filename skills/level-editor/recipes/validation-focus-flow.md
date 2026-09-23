# Validation focus flow

## Khi nào dùng

Dùng khi validation issue cần xuất hiện đồng thời trên canvas, inspector, summary panel, Save và
load path.

## Vấn đề

Một message chỉ có ở console hoặc một panel không giúp GD tìm đúng entity/field để sửa.

## Cách làm

Mỗi issue chứa severity, stable target ID, optional field key, message GD-facing và fix hint.
Message theo công thức: WHAT sai, WHERE, HOW to fix. Nút issue phải gọi một flow duy nhất:
đổi mode nếu cần, select theo ID, frame target, focus field nếu có.

Blocking luôn chặn Save/Play Test và có tooltip giải thích. Warning cho phép save. Info chỉ gợi ý.

## Skeleton

Implementation note: keep GD copy separate from exception text.
Keep the first blocking issue visible without opening a debug console.
An internal field key may route focus, but must not leak into the user message.
The summary should group by severity and place blocking issues first.
Heavy validation belongs to an explicit action or cancellable background path.

```csharp
public readonly struct ValidationIssue
{
    public readonly Severity Severity;
    public readonly string TargetId;
    public readonly string FieldKey;
    public readonly string Message;
}

private void FocusIssue(ValidationIssue issue)
{
    viewState.activeMode = ResolveMode(issue.TargetId);
    viewState.selectedId = issue.TargetId;
    canvas.Frame(issue.TargetId);
    inspector.FocusField(issue.FieldKey);
}
```

## Bẫy thường gặp

- Message dùng tên class/serialized field.
- Chỉ đổi màu, không có icon hoặc text severity.
- Click issue không select đúng target.
- Save disabled nhưng không nói blocker đầu tiên.
- Validation nặng chạy mỗi pointer move.

## Cách kiểm chứng

- Cố ý tạo một issue ở từng severity.
- Issue xuất hiện ở cả canvas, field, summary và Save state.
- Click summary đưa đúng mode, entity, field và frame.
- Sửa xong issue biến mất ngay.
- Load file cũ sai rule: báo lỗi mà không tự sửa im lặng.
