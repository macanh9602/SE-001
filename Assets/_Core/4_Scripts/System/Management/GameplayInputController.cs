using UnityEngine;
using SE001.Gameplay;

namespace SE001.System.Management
{
    [DisallowMultipleComponent]
    public sealed class GameplayInputController : MonoBehaviour, ILevelLifecycleParticipant
    {
        private GameplayManager manager; private Camera inputCamera; private LevelContext context; private Vector2 down; private bool held;
        public void Bind(LevelContext value){CleanupForLevelUnload();context=value;manager=GetComponent<GameplayManager>();inputCamera=Camera.main;}
        private void Update(){if(manager==null||context==null||manager.State!=GameState.Playing)return;if(Input.GetMouseButtonDown(0)){down=Input.mousePosition;held=true;}if(held&&Input.GetMouseButtonUp(0)){held=false;if(((Vector2)Input.mousePosition-down).sqrMagnitude<400f){Ray ray=inputCamera.ScreenPointToRay(Input.mousePosition);if(Mathf.Abs(ray.direction.z)<.0001f)return;Vector3 p=context.BoardRoot.InverseTransformPoint(ray.origin+ray.direction*((-ray.origin.z)/ray.direction.z));for(int i=0;i<manager.Sources.Count;i++){SourceDomain s=manager.Sources[i];if(Vector2.Distance((Vector2)p,s.Position)<.5f){manager.ToggleSource(s.StableId);break;}}}}}
        public void CleanupForLevelUnload(){manager=null;context=null;inputCamera=null;held=false;}
    }
}
