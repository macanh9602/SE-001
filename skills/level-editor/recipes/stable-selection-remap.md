# Stable selection remap

## Khi nào dùng

Dùng cho list entity có thể add, remove, sort, filter, hide hoặc đổi layout.

## Vấn đề

Selection theo array index sẽ trỏ nhầm entity sau structural edit. Selection theo object reference
cũng hỏng sau reload document hoặc domain reload.

## Cách làm

- Mỗi entity có stable ID trong Document.
- ViewState chỉ lưu stable ID và selection mode.
- Render list map ID sang row/index bằng dictionary mỗi lần document reload.
- Add/remove/sort remap theo ID, không theo vị trí cũ.
- Xóa selected entity thì chọn nearest sensible target hoặc empty state rõ.
- Hidden/non-editable entity bị loại khỏi hit-test và selection.

## Skeleton

Implementation note: the dictionary is only an index; the stable ID remains authoritative.
List rendering can use row indices, but commands resolve the ID at execution time.
Keep visible, editable and selected as separate state values.
Do not use a filtered row number as a persisted target.
An invalid target must become an explicit empty state, never an accidental neighbor.
Undo snapshots must not contain row index or filter state.
Hide and editability changes must update selection immediately.

```csharp
private readonly Dictionary<string, int> rowById = new Dictionary<string, int>();

private void RebuildIndex(IReadOnlyList<EntityData> entities)
{
    rowById.Clear();
    for (int i = 0; i < entities.Count; i++) rowById[entities[i].StableId] = i;
}

private void RemapSelection()
{
    if (!document.Contains(viewState.selectedId) || !IsEditable(viewState.selectedId))
        viewState.selectedId = string.Empty;
}
```

## Bẫy thường gặp

- Lưu `selectedIndex` trong SessionState.
- Dùng row index làm command target.
- Filter list nhưng quên filter hit-test.
- Ghost object vẫn bị Delete.
- Sort làm selection nhảy sang entity kế bên.

## Cách kiểm chứng

- Chọn item B, thêm item trước B: vẫn chọn B.
- Xóa item trước B hoặc sort: vẫn chọn đúng stable ID.
- Hide/chuyển layer: item rời selection hoặc có feedback rõ.
- Reload file: selection được khôi phục nếu ID còn tồn tại.
