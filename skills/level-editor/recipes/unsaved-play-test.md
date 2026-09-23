# Unsaved Play Test

## Khi nào dùng

Dùng khi GD cần chạy document hiện tại dù chưa Save, qua đúng runtime loader/spawner.

## Vấn đề

Nếu Play Test chỉ đọc file đã lưu, GD không thể kiểm tra thay đổi hiện tại. Nếu ghi đè Resources,
tool làm bẩn source of truth và dễ để lại temp data.

## Cách làm

- Validate Document trước khi Play Test.
- Serialize một temp payload trong session/editor cache, không ghi level authoring.
- Đặt one-shot session override có token/generation.
- Runtime loader consume đúng một lần rồi clear override.
- Spawn path vẫn là loader/validator/spawner production.
- Thoát Play khôi phục document path, dirty state, mode, selection và view.

## Skeleton

```csharp
public static class PlayTestOverride
{
    private static string payload;
    private static int generation;

    public static void Set(string json)
    {
        payload = json;
        generation++;
        SessionState.SetInt("editor.playTestGeneration", generation);
    }

    public static bool TryConsume(out string json)
    {
        json = payload;
        payload = null;
        return !string.IsNullOrEmpty(json);
    }
}
```

```csharp
string json = PlayTestOverride.TryConsume(out string overrideJson)
    ? overrideJson
    : LoadNormalLevel();
```

## Bẫy thường gặp

- Ghi temp JSON vào Resources.
- Loader thứ hai cho Editor Preview.
- Bỏ qua validator hoặc spawner production.
- Override còn sót, làm lần Play sau chạy nhầm document.
- Dirty document bị mất sau Play Mode.

## Cách kiểm chứng

- Sửa document, không Save, Play Test và xác nhận runtime thấy sửa đổi.
- Chạy Play Test lần hai không có override cũ.
- Exit Play: document vẫn dirty và view state vẫn đúng.
- Blocking validation disable Play Test với lý do rõ.
