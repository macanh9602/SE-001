# Story 015 — GD tunes Source and Bowl per level

Status: IMPLEMENTING  
Size: L  
Execution mode: DIRECT  
Frontier collaboration: INHERIT (project AUTO)

## Goal

GD can adjust Source and Bowl scale plus Source stream rate and width in the Level Editor, save them to level JSON, and see matching runtime geometry.

## Contract

- Schema 5 has one `sourceScale`, `bowlScale`, `sourceEmissionRate`, and `sourceStreamWidth` per level.
- Source nozzle and Bowl bottom centre remain authored positions. Scale changes visual and gameplay geometry.
- Bowl scale does not change grains per logical unit. Source rate and width are independent of Source scale.
- Schema 3/4 continue to load with legacy Profile values; Editor upgrades on open and saves schema 5.
- No per-entity scale, new art, or device quality tier in this story.

## Acceptance

- [ ] Schema 3/4 load unchanged, and Editor save/reopen persists four schema-5 values.
- [ ] Source visual/tap area and Bowl visual/sink/capacity agree with Editor preview.
- [ ] Source stream rate/width are independent of scale; larger Bowl needs the same grains for a fixed Required Amount.
- [ ] Editor shows four controls with GD tooltips and blocks invalid values/bounds/capacity.
- [ ] Unity compile, relevant EditMode tests, Play Test and console smoke pass.
- [ ] Redmi 9A CPU/GC/draw-call/memory impact measured or reported PENDING with manual step.

## Implementation handoff

Data contract → runtime resolution → Editor controls/preview/validation → tests → verification → closure. Record evidence in `implementation-notes.html`; do not close until manual Editor walkthrough and device gate are evaluated.
