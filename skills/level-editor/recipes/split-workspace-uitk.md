# Split workspace bằng UI Toolkit

## Khi nào dùng

Dùng khi tool có canvas chính và các pane hierarchy/inspector/validation cần nhìn đồng thời.

## Vấn đề

Fixed column hoặc floating overlay thường che canvas, không resize được hoặc vỡ ở cửa sổ hẹp.

## Cách làm

- Dùng `TwoPaneSplitView` cho pane thật.
- Pane chính là canvas, pane phụ có min width và scroll riêng.
- Global actions ở toolbar cố định.
- Khi hẹp, collapse inspector trước; không ẩn Save/Play Test/tool đang active.
- Lưu độ rộng pane bằng `SessionState` hoặc ViewState editor.
- Dùng `GeometryChangedEvent` cho layout phụ thuộc kích thước thật.

## Skeleton

Implementation note: measure resolved content after layout, not during node creation.
The canvas must remain usable when the inspector reaches its minimum width.
Splitter movement must not rebuild the document model.

```xml
<ui:TwoPaneSplitView name="outer" fixed-pane-index="0" fixed-pane-initial-dimension="220">
    <ui:ScrollView name="hierarchy" />
    <ui:TwoPaneSplitView name="inner" fixed-pane-index="1" fixed-pane-initial-dimension="280">
        <ui:VisualElement name="canvas" />
        <ui:ScrollView name="inspector" />
    </ui:TwoPaneSplitView>
</ui:TwoPaneSplitView>
```

```uss
.canvas { flex-grow: 1; min-width: 280px; }
.inspector { min-width: 220px; flex-shrink: 1; }
.global-toolbar { flex-shrink: 0; }
```

```csharp
split.RegisterCallback<GeometryChangedEvent>(_ => RepositionOverlaysFromContentRect());
SessionState.SetFloat("tool.inspectorWidth", inspector.resolvedStyle.width);
```

## Bẫy thường gặp

- Dùng absolute panel cho inspector persistent.
- Pane min width bằng 0 làm canvas không dùng được.
- Rebuild data model khi chỉ kéo splitter.
- Hardcode vị trí tick/label theo pixel ban đầu.
- Nested ScrollView cùng trục không có ownership rõ.

## Cách kiểm chứng

- Kéo từng splitter đến min/max và reset nếu có.
- Resize window về khoảng 640px; canvas, active tool, Save và Play Test vẫn dùng được.
- Scroll inspector không đẩy canvas và không scroll cả workspace.
- Đóng/mở window: pane width được giữ.
