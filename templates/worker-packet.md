# Worker Packet — [XX] [Name]

## 0. WORKER MODE

You are an implementation worker.

- Do not redesign architecture.
- Do not invent requirements.
- Follow steps in order.
- If repo differs from expected, inspect the nearest local equivalent before changing.
- Prefer minimal patch.
- Do not modify unrelated systems.

## 1. GOAL

[3–6 lines]

## 2. NON-GOALS

- ...

## 3. FILES TO TOUCH

- ...

Do not modify other files unless required for compilation/reference repair.

## 4. LOCAL REQUIRED RULES

Copy only rules relevant to this packet.

## 5. CURRENT STATE

Only facts required to execute this packet.

## 6. TARGET STATE

```text
Current:
...

After:
...
```

## 7. IMPLEMENTATION STEPS

### Step 1 — ...

File:
`...`

IMPORTANT:
- local instruction here

Expected after step:
- ...

### Step 2 — ...

## 8. DO NOT DO

- ...

## 9. PACKET ACCEPTANCE

- [ ] ...

## 10. VERIFY BEFORE NEXT PACKET

- lint/diff/check specific to packet

## 11. STOP / ESCALATE ONLY IF

- contract conflict
- required file/path fundamentally absent
- change would alter story scope

## 12. REPORT FORMAT

```text
Packet:
Changed:
Verification:
Semantic deviations:
Open risk:
Next packet ready: YES / NO
```
