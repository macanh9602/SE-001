# Authority modes

- Presentation: Domain remains authoritative; Physics/Visual supplies feel and semantic feedback only.
- Hybrid: Domain issues a command, Simulation runs a bounded physical phase, then emits a semantic signal; Domain decides the result.
- Simulation-driven: physical runtime state is authoritative within the selected contract; Domain consumes semantic state/signals.

Each mode may use authored, real, hybrid, or custom simulation. Do not hide the choice and do not make Simulation mandatory for ordinary deterministic games.
