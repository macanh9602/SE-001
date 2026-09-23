# GD Level Authoring Guide

## Tools

- Open `SE001/Level Editor` for level JSON authoring.
- Open `SE001/Layout Bake` for converting an SVG layout into a baked layout asset.

## Bake a layout

1. In Layout Bake, choose an SVG source and confirm the Layout ID.
2. Review the preview and metrics.
3. Bake and confirm the success message.
4. Use `Clear SVG` to return to the empty state without changing existing baked layouts.

## Author a level

1. Use `New Level` or open a row from `Saved Levels`.
2. Choose the baked Layout, then add or edit Sources, Cups and Obstacles.
3. Fix validation messages shown in the Inspector before saving.
4. Save, then use `Play Test` to preview the current document, including unsaved changes.
5. Delete removes only the selected level JSON and its sequence entry after confirmation. It does not delete layouts or profile assets.

Malformed unrelated JSON files are shown as unreadable and do not replace the current document. A dirty document must be saved or discarded before opening or deleting another level.
