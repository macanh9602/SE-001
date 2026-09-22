using System;
using System.Collections.Generic;
using UnityEngine;
using SE001.Gameplay;

namespace SE001.System.Management
{
    public enum GameState { Playing, Won, Lost }
    public enum LoseReason { WrongCup, NotFilled }
    [DisallowMultipleComponent]
    public sealed class GameplayManager : MonoBehaviour, ILevelLifecycleParticipant
    {
        [SerializeField] private float fixedStepHz = 60f;
        private readonly List<SourceDomain> sources=new List<SourceDomain>(); private readonly List<CupDomain> cups=new List<CupDomain>(); private LevelContext context; private int stableSteps; private bool bound;
        public event Action<GameState> GameStateChanged; public event Action<string> SourceStateChanged; public event Action<string> CupChanged; public event Action<LoseReason> LevelLost;
        public GameState State {get;private set;}=GameState.Playing; public IReadOnlyList<SourceDomain> Sources=>sources; public IReadOnlyList<CupDomain> Cups=>cups;
        public void Bind(LevelContext value){CleanupForLevelUnload();context=value;State=GameState.Playing;fixedStepHz=Mathf.Max(1f,fixedStepHz);bound=true;}
        public void Configure(IEnumerable<SourceDomain> sourceValues,IEnumerable<CupDomain> cupValues,float cellSize,float wallThickness){sources.Clear();cups.Clear();sources.AddRange(sourceValues);cups.AddRange(cupValues);for(int i=0;i<cups.Count;i++)cups[i].RegisterWalls(context.SandSimulation,cellSize,wallThickness);}
        public void ToggleSource(string id){if(!bound||State!=GameState.Playing)return;for(int i=0;i<sources.Count;i++)if(sources[i].StableId==id){sources[i].Toggle();SourceStateChanged?.Invoke(id);return;}}
        private void FixedUpdate(){if(!bound||context==null||State!=GameState.Playing)return;float dt=1f/Mathf.Max(1f,fixedStepHz);for(int i=0;i<sources.Count;i++)sources[i].Emit(context.SandSimulation,dt);int moved=context.SandSimulation.Step();bool collected=false;for(int i=0;i<cups.Count;i++){collected|=cups[i].Collect(context.SandSimulation);if(cups[i].ForeignDetected){Lose(LoseReason.WrongCup);return;}if(collected)CupChanged?.Invoke(cups[i].StableId);}if(moved==0&&!collected)stableSteps++;else stableSteps=0;bool allFull=cups.Count>0;for(int i=0;i<cups.Count;i++)allFull&=cups[i].Full;if(allFull){Win();return;}bool allEmpty=true;for(int i=0;i<sources.Count;i++)allEmpty&=sources[i].State==SourceValveState.Empty;if(allEmpty&&stableSteps>=30)Lose(LoseReason.NotFilled);}
        private void Win(){State=GameState.Won;GameStateChanged?.Invoke(State);} private void Lose(LoseReason reason){State=GameState.Lost;LevelLost?.Invoke(reason);GameStateChanged?.Invoke(State);}
        public void CleanupForLevelUnload(){bound=false;context=null;sources.Clear();cups.Clear();State=GameState.Playing;stableSteps=0;}
    }
}
