# Document, ViewState, DerivedState

## Khi nào dùng

Dùng cho mọi editor có dữ liệu cần Save/Open và có trạng thái hiển thị tạm thời.

## Vấn đề

Nếu selection hoặc cache preview bị serialize vào file, cùng một document có thể mở ra khác nhau
trên máy khác. Nếu validation được coi là source of truth, cache cũ có thể che lỗi mới.

## Cách làm

- `Document`: dữ liệu authoring duy nhất được serialize.
- `ViewState`: selection theo stable ID, mode, zoom, pan, filter, pane và scroll.
- `DerivedState`: validation, preview cache, metrics và stale flags được tính lại.
- Undo/Redo chỉ snapshot Document.
- `ApplyEdit` mutate một phần Document và invalidate DerivedState liên quan.
- `ReloadDocument` thay cả Document khi Open/Undo/đổi file.
- ViewState chỉ được reset có kiểm soát và cố gắng remap selection theo ID.

## Skeleton

```csharp
[Serializable]
public sealed class AuthoringDocument
{
    public int schemaVersion;
    public List<EntityData> entities = new List<EntityData>();
}

public sealed class AuthoringViewState
{
    public string selectedId;
    public float zoom = 1f;
    public Vector2 pan;
    public string activeMode;
}

public sealed class AuthoringDerivedState
{
    public IReadOnlyList<ValidationIssue> issues;
    public PreviewCache preview;
    public bool IsStale { get; private set; }

    public void Invalidate() { IsStale = true; }
    public void Rebuild(AuthoringDocument document) { /* derive, then clear stale */ }
}
```

## Bẫy thường gặp

- Serialize `selectedIndex` thay vì stable ID.
- Gọi `ReloadDocument` cho mỗi keystroke.
- Dùng preview mesh làm dữ liệu authoring.
- Rebuild DerivedState nhưng quên invalidate khi add/remove.
- Undo snapshot cả zoom, pane width hoặc scroll.

## Cách kiểm chứng

- Save rồi mở lại: file không có selection, zoom, pan hoặc cache preview.
- Add/remove/sort entity không chọn nhầm entity khác.
- Sửa field: issue và preview cập nhật, ViewState không nhảy.
- Undo/Redo giữ nguyên ViewState nhưng thay đúng Document.
- Mở file lỗi không làm mất Document đang mở.
