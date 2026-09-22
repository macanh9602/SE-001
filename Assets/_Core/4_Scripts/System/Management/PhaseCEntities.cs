using System;
using UnityEngine;
using SE001.Data;
using SE001.Simulation.Sand;

namespace SE001.Gameplay
{
    public enum SourceValveState { Closed, Opening, Open, Empty }
    public sealed class SourceDomain
    {
        private readonly float openDelay; private readonly float rate; private readonly int width; private float opening;
        public SourceDomain(SourceData data, SourceProfile profile, int grainsPerUnit)
        { StableId=data.stableId; MaterialId=(byte)data.materialId; Remaining=Mathf.Max(0,data.logicalAmount)*Mathf.Max(1,grainsPerUnit); openDelay=profile.valveOpenDelay; rate=data.emissionRate>0?data.emissionRate:profile.emissionRate; width=Mathf.Max(1,Mathf.RoundToInt(data.streamWidth>0?data.streamWidth:profile.streamWidth)); Position=data.position; State=data.startsOpen?SourceValveState.Open:SourceValveState.Closed; if(Remaining==0) State=SourceValveState.Empty; }
        public string StableId { get; } public byte MaterialId { get; } public Vector2 Position { get; } public int Remaining { get; private set; } public SourceValveState State { get; private set; }
        public void Toggle() { if(State==SourceValveState.Empty)return; if(State==SourceValveState.Open){State=SourceValveState.Closed;return;} if(State==SourceValveState.Opening){State=SourceValveState.Closed;opening=0;return;} State=SourceValveState.Opening; opening=0; }
        public int Emit(SandSimulation sim, float dt)
        { if(State==SourceValveState.Empty||State==SourceValveState.Closed)return 0; if(State==SourceValveState.Opening){opening+=dt;if(opening<openDelay)return 0;State=SourceValveState.Open;} if(Remaining<=0){State=SourceValveState.Empty;return 0;} int cx=Mathf.FloorToInt(Position.x/sim.CellSize); int cy=Mathf.FloorToInt(Position.y/sim.CellSize); int inserted=0; int half=width/2; for(int x=cx-half;x<=cx+half&&inserted<Mathf.CeilToInt(rate*dt)&&inserted<Remaining;x++) if(sim.TryEmit(x,cy,MaterialId))inserted++; Remaining-=inserted; if(Remaining==0)State=SourceValveState.Empty; return inserted; }
    }

    public sealed class CupDomain
    {
        public CupDomain(CupData data, CupProfile profile, int grainsPerUnit, float cellSize)
        { StableId=data.stableId; AcceptedMaterialId=(byte)data.acceptedMaterialId; Position=data.position; Size=data.size; Required=Mathf.Max(1,data.requiredAmount)*Mathf.Max(1,grainsPerUnit); BuildSink(profile.wallThickness,cellSize); }
        public string StableId {get;} public byte AcceptedMaterialId {get;} public Vector2 Position {get;} public Vector2 Size {get;} public int Required {get;} public int Collected {get;private set;} public bool Full=>Collected>=Required; public bool ForeignDetected {get;private set;}
        public int MinX{get;private set;} public int MaxX{get;private set;} public int MinY{get;private set;} public int MaxY{get;private set;}
        private void BuildSink(float wall,float cell){MinX=Mathf.CeilToInt((Position.x-Size.x*.5f+wall)/cell);MaxX=Mathf.FloorToInt((Position.x+Size.x*.5f-wall)/cell);MinY=Mathf.CeilToInt((Position.y-wall+wall)/cell);MaxY=Mathf.FloorToInt((Position.y+Size.y*.5f-wall)/cell);}
        public void RegisterWalls(SandSimulation sim,float cell,float wall){int x0=Mathf.FloorToInt((Position.x-Size.x*.5f)/cell),x1=Mathf.CeilToInt((Position.x+Size.x*.5f)/cell), y0=Mathf.FloorToInt((Position.y-Size.y*.5f)/cell),y1=Mathf.CeilToInt((Position.y+Size.y*.5f)/cell);int t=Mathf.Max(1,Mathf.CeilToInt(wall/cell));for(int x=x0;x<=x1;x++){for(int k=0;k<t;k++){sim.SetCupWall(x,y0+k,true);sim.SetCupWall(x,y1-k,true);}}for(int y=y0;y<=y1;y++){for(int k=0;k<t;k++){sim.SetCupWall(x0+k,y,true);sim.SetCupWall(x1-k,y,true);}}}
        public bool Collect(SandSimulation sim){if(Full)return false;bool changed=false;for(int y=MinY;y<=MaxY&&!Full;y++)for(int x=MinX;x<=MaxX&&!Full;x++){byte m=sim.State.Cells[sim.State.Index(x,y)];if(m==0)continue;if(m!=AcceptedMaterialId){ForeignDetected=true;return false;}sim.Remove(x,y);Collected++;changed=true;}return changed;}
    }
}
