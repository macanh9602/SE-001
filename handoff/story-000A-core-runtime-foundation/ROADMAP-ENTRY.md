# ROADMAP patch sau khi Story 000A được đặt vào project

Không cần mark DONE trước khi implementation/closure pass.

Đề xuất sửa phase:

```md
- P1 Bootstrap + Core Runtime Foundation: Story 000–000A
- P2 Technical slice: Story 001
```

Thêm row ngay sau Story 000:

```md
| 000A | Core Runtime Foundation — LevelManager lifecycle | M | TODO | 000 | Runtime lifecycle / legacy audit |
```

Dependency nên hiểu lại:

- Story 001 phụ thuộc `000A` thay vì chỉ `000`.
- Story 004 phụ thuộc `000A,002`.
- Milestone Foundation ready = `000 + 000A`.

Chỉ apply canonical roadmap status sau khi Story 000A closure gate PASS; dependency row có thể thêm trước để thể hiện thứ tự thực thi.
