# Delayed fields và callback lifecycle

## Khi nào dùng

Dùng cho TextField, IntegerField, FloatField hoặc slider mà edit có thể rebuild preview hoặc
validation tốn chi phí.

## Vấn đề

Rebuild theo từng ký tự làm mất focus, tạo GC và khiến validation nặng chạy hàng chục lần. Đăng ký
callback trong refresh lại làm một thay đổi chạy nhiều lần sau reopen/domain reload.

## Cách làm

- `isDelayed = true` cho field text/number có commit semantics.
- Field nhẹ có thể preview trong lúc gõ nhưng chỉ `ApplyEdit` khi commit.
- Callback đăng ký một lần trong `CreateGUI`.
- `Refresh` chỉ cập nhật value, class, text và enabled state.
- Unregister external callbacks trong `OnDisable`/`OnDestroy`.
- Mixed value blur không nhập gì là no-op, không commit zero.

## Skeleton

Implementation note: document the commit boundary in the test name.
Do not make delayed commit behavior an invisible convention.

```csharp
private FloatField durationField;
private bool callbacksRegistered;

private void BuildInspector()
{
    durationField = new FloatField("Duration (s)") { isDelayed = true };
    inspector.Add(durationField);
}

private void RegisterCallbacksOnce()
{
    if (callbacksRegistered) return;
    callbacksRegistered = true;
    durationField.RegisterValueChangedCallback(OnDurationCommitted);
    Undo.undoRedoPerformed += OnUndoRedo;
}

private void OnDisable() { Undo.undoRedoPerformed -= OnUndoRedo; }
```

## Bẫy thường gặp

- `valueChanged` gọi `ReloadDocument`.
- Callback đăng ký trong `RefreshInspector`.
- Empty/mixed field parse thành 0.
- Unregister thiếu khiến callback giữ window cũ.
- Slider vừa preview vừa push undo theo frame.

## Cách kiểm chứng

- Nhập chuỗi dài: focus không mất và không có rebuild mỗi ký tự.
- Reopen/domain reload: một commit chỉ tạo một thay đổi.
- Undo/Redo callback không tăng số lần sau mỗi lần mở.
- Blur field mixed mà không nhập: Document không dirty.
