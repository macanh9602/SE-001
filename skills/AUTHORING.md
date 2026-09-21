# Skill Authoring Contract

## Roles

- `SKILL.md`: trigger metadata, decision flow, hard rules, routing, and output contract. Keep it concise and actionable.
- `refs/`: deep reference material loaded only when the task needs it.
- `recipes/`: copy/adapt implementation patterns. A recipe is guidance, not a production framework.
- `knowledge/`: reusable domain knowledge, vocabulary, or catalogues; it is not a workflow.

## Create a new skill when

Create a skill only when a topic has distinct triggers, decisions, hard rules, and an output contract that cannot be routed cleanly through an existing skill. Prefer adding a ref, recipe, or knowledge file when the topic is detail rather than a new workflow.

## Migration rules

Preserve frontmatter names, trigger meaning, hard rules, semantics, and output shape. Move content before deduplicating it. Every moved section needs one clear canonical owner and repaired local links.

## Progressive disclosure

Keep the first-load path small: classify the request, apply the core decision flow, state constraints, route to the exact ref/recipe, and define verification. Put long examples, terminology, and implementation detail behind local links.

## Recipe contract

Each recipe should state:

1. When to use it and its non-goals.
2. Inputs, ownership, lifecycle, and tunables.
3. A minimal copy/adapt baseline.
4. Pooling, cancellation, and cleanup requirements where relevant.
5. Verification and known limitations.

Recipes must not silently introduce project-wide architecture or claim universal ownership semantics.
