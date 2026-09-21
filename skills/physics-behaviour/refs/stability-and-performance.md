# Stability and mobile performance

Review dynamic body count, collider complexity, contact generation, sleep, solver iterations, CCD, fixed timestep, joints, interpolation, pooling, material reuse, and callback/query allocations. Avoid MeshCollider on dynamic bodies unless justified. Do not invent numeric budgets; measure on target devices. Release must reset velocity, kinematic/constraints baseline, settle/contact state, callbacks, and generation; Acquire/Bind reapplies the full expected state.
