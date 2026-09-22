using UnityEngine;

namespace SE001.Geometry
{
    public readonly struct BoardSpaceMapper
    {
        public BoardSpaceMapper(Rect svgViewBox, float boardUnitsPerSvgUnit)
        {
            ViewBox = svgViewBox;
            BoardUnitsPerSvgUnit = boardUnitsPerSvgUnit;
        }

        public Rect ViewBox { get; }
        public float BoardUnitsPerSvgUnit { get; }

        public Vector2 SvgToBoard(Vector2 svgPoint)
        {
            return new Vector2((svgPoint.x - ViewBox.xMin) * BoardUnitsPerSvgUnit, (ViewBox.yMax - svgPoint.y) * BoardUnitsPerSvgUnit);
        }
    }
}
