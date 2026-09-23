# EditorWindow partials

## Khi nào dùng

Dùng khi một `EditorWindow` có từ ba vùng UI trở lên: toolbar, workspace, inspector,
validation hoặc status. Recipe này dành cho tool authoring generic, không chứa rule của game.

## Vấn đề

Một `EditorWindow` quá lớn thường trộn state, event wiring và layout. Khi một field thay đổi,
tool dễ rebuild toàn bộ cửa sổ, mất selection, scroll hoặc tạo callback trùng sau domain reload.

## Cách làm

- Một partial giữ lifecycle và state ownership.
- Một partial dựng toolbar/global actions.
- Một partial dựng canvas/workspace.
- Một partial dựng inspector/validation.
- `CreateGUI` chỉ gọi các builder theo thứ tự cố định.
- Callback được đăng ký một lần trong `CreateGUI` hoặc constructor path.
- Refresh chỉ cập nhật vùng bị ảnh hưởng, không gọi lại `CreateGUI`.
- Document, ViewState và DerivedState nằm ở owner riêng.

## Skeleton

```csharp
public sealed partial class AuthoringWindow : EditorWindow
{
    private AuthoringDocument document;
    private AuthoringViewState viewState;
    private AuthoringDerivedState derivedState;

    public void CreateGUI()
    {
        BuildState();
        BuildToolbar(rootVisualElement);
        BuildWorkspace(rootVisualElement);
        BuildInspector(rootVisualElement);
        BuildStatus(rootVisualElement);
        RegisterCallbacksOnce();
        RefreshAll();
    }
}

public sealed partial class AuthoringWindow
{
    private void BuildToolbar(VisualElement root) { /* global actions */ }
    private void BuildWorkspace(VisualElement root) { /* canvas and panes */ }
}
```

```xml
<ui:VisualElement name="workspace">
    <ui:Toolbar name="global-toolbar" />
    <ui:TwoPaneSplitView name="main-split" fixed-pane-index="0" />
    <ui:VisualElement name="status-bar" />
</ui:VisualElement>
```

## Bẫy thường gặp

- Partial chỉ để chia file nhưng vẫn để mọi partial mutate mọi state.
- `RefreshInspector` gọi lại `CreateGUI`.
- Global action biến mất khi đổi mode.
- Callback nằm trong hàm refresh và tăng gấp đôi sau mỗi edit.
- ViewState được ghi chung vào snapshot undo của Document.

## Cách kiểm chứng

- Mở lại cửa sổ sau domain reload và kiểm tra callback chỉ chạy một lần.
- Đổi mode khi đang chọn entity: selection, zoom, scroll và pane width không tự reset.
- Sửa một field chỉ rebuild vùng liên quan.
- Undo chỉ khôi phục Document, không làm nhảy ViewState.
- Global Save, Undo, Redo và Play Test giữ nguyên vị trí giữa các mode.
