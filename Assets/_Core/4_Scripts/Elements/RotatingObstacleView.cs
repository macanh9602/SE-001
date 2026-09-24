using SE001.Simulation.Sand;
using UnityEngine;

namespace SE001.Elements.Layout
{
    [DisallowMultipleComponent]
    public sealed class RotatingObstacleView : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer firstBar;
        [SerializeField] private SpriteRenderer secondBar;
        [SerializeField] private Transform pivot;
        private RotatingObstacleSystem system;
        private int index;

        public void Bind(RotatingObstacleSystem value, int obstacleIndex, float barWidth, Material sharedMaterial)
        {
            if (firstBar == null || secondBar == null || pivot == null)
                throw new MissingComponentException("RotatingObstacle prefab requires two SpriteRenderers and a pivot.");
            system = value;
            index = obstacleIndex;
            RotatingObstacleState state = system.GetState(index);
            transform.localPosition = new Vector3(state.Position.x, state.Position.y, 0f);
            firstBar.drawMode = SpriteDrawMode.Sliced;
            secondBar.drawMode = SpriteDrawMode.Sliced;
            firstBar.size = secondBar.size = new Vector2(state.BarLength * state.Scale, barWidth * state.Scale);
            firstBar.sharedMaterial = secondBar.sharedMaterial = sharedMaterial;
            firstBar.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);
            secondBar.transform.localRotation = Quaternion.Euler(0f, 0f, -45f);
            RenderAngle();
        }

        private void LateUpdate()
        {
            if (system != null) RenderAngle();
        }

        private void RenderAngle()
        {
            // Interpolate between the last two fixed steps: sim runs at a fixed 60 Hz, frames do not (Movie_006 stutter).
            RotatingObstacleState state = system.GetState(index);
            float angle = Mathf.LerpAngle(state.PreviousAngle, state.Angle, Mathf.Clamp01(system.RenderAlpha));
            pivot.localRotation = Quaternion.Euler(0f, 0f, angle);
        }
    }
}
