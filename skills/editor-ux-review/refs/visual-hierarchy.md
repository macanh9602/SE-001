# Visual hierarchy review

## Thứ tự ưu tiên

1. Canvas hoặc authoring surface.
2. Tool đang active và context hiện tại.
3. Save, Undo/Redo, Play Test và validation blocking.
4. Inspector field thường dùng của entity đang chọn.
5. Metadata, raw ID, advanced tuning và action hiếm.

## Câu hỏi review

- Mắt có tìm thấy canvas trước metadata không?
- Entity đang chọn có khác item chưa chọn bằng shape/state, không chỉ bằng màu không?
- Active tool có thể nhận ra trong một glance không?
- Blocking validation có nổi bật hơn warning/info không?
- Save/Play Test có biến mất khi đổi mode không?
- Advanced field có đẩy field thường dùng khỏi vùng nhìn không?

## Bố cục

Global actions ở vị trí cố định. Tool controls nằm trong tool area. Context controls chỉ hiện khi
liên quan. Inspector là pane thật có scroll riêng; không dùng overlay cho thông tin cần đọc lâu.

## Evidence

Chụp ít nhất: default width, narrow width, selected entity, dirty document, blocking issue,
empty state và inspector dài. Mỗi ảnh phải kèm task đang thực hiện và expected hierarchy.

## Finding rule

Không ghi "layout chưa đẹp". Hãy ghi hậu quả quan sát được, ví dụ: "GD không thấy Save sau khi
đổi mode, phải quay lại tab cũ; task Save bị chậm và dễ bỏ sót".
