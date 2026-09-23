# ApplyEdit và ReloadDocument

## Khi nào dùng

Dùng khi editor vừa có chỉnh sửa cục bộ vừa có thao tác thay toàn bộ document.

## Vấn đề

Dùng một đường cập nhật cho cả edit và load khiến tool mất selection, rebuild quá nặng hoặc giữ
derived data cũ. Ranh giới này phải rõ ngay từ API đầu tiên.

## Cách làm

`ApplyEdit(change)` dành cho một intent của user. Nó mutate phần liên quan, push đúng một undo
entry và invalidate derived state cần thiết.

`ReloadDocument(document)` dành cho Open, đổi file, migration hoặc Undo/Redo. Nó rebuild toàn bộ
derived state và remap selection theo stable ID; không push thêm undo.

`ChangeView(viewChange)` chỉ mutate ViewState và không làm dirty Document.

## Skeleton

Implementation note: make the two paths separate commands in code review.
Keep reload reserved for document replacement, not ordinary field edits.

```csharp
public void ApplyEdit(Action<AuthoringDocument> change, string undoName)
{
    Undo.RecordObject(documentAsset, undoName);
    change(document);
    document.EnsureValidCollections();
    derivedState.Invalidate();
    RebuildAffectedViews();
}

public void ReloadDocument(AuthoringDocument next)
{
    string previousSelection = viewState.selectedId;
    document = next;
    derivedState.Rebuild(document);
    viewState.selectedId = document.Contains(previousSelection) ? previousSelection : string.Empty;
    Repaint();
}
```

## Bẫy thường gặp

- `ReloadDocument` trong `valueChanged`.
- Push undo mỗi frame kéo.
- Dirty flag bật khi chỉ zoom/pan.
- Reload xóa selection dù stable ID vẫn tồn tại.
- ApplyEdit không invalidate validation nên issue biến mất muộn.

## Cách kiểm chứng

- Gõ 12 ký tự liên tục: focus không mất và chỉ có một commit khi field kết thúc.
- Kéo một entity dài: một undo đưa về đúng vị trí ban đầu.
- Zoom/pan rồi đóng/mở: document vẫn clean.
- Open file mới: derived state đổi toàn bộ, không trộn issue của file cũ.
