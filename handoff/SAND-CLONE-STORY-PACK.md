# SE-001 — Sand Clone Story Pack

Đây là story pack tự chứa cho project hiện tại.

## Principle

- Clone core gameplay của **Salt & Pepper, Don't mix em up**.
- Improvement chủ đích duy nhất: **Powder sand feel**.
- Không phụ thuộc `.zip` ngoài project.
- Không SDK / ads / analytics / IAP.
- Không final art polish.
- Không tự thêm gameplay gimmick chưa có source-of-truth.

## Trước tiên

1. Đọc `Docs/USER-SETUP-GATE.md`.
2. Chốt target low-end device + identity/contracts.
3. Chạy Story 000.
4. Sau Story 000, chạy tuần tự 001 → 014 theo `handoff/ROADMAP.proposed.md`.

## Story dependency

```text
000
 ├─001→002→003
 └──────→004→005
            └────────┐
003→004→006→007→008→009→010
              004────────┘
009→011→012
009→013
010+012+013→014
```

## Notes

Story 001 là technical slice thật: nếu không có device target/số đo thì không được mark DONE.
Story 012 là L và có worker packets.
