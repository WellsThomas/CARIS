using Packages.Serializable;

namespace Communication.ActionPackage
{
    public class DistributeFreezeFrameAction: IAction
    {
        public byte[] Data;
        public int Height;
        public int Width;
        public int DrawingID;
        public LineSegment[] DrawingSegments;

        public DistributeFreezeFrameAction(byte[] data, int height, int width, int drawingID,
            LineSegment[] drawingSegment)
        {
            this.Data = data;
            this.Height = height;
            this.Width = width;
            this.DrawingID = drawingID;
            this.DrawingSegments = drawingSegment;
        }

        public int GetHouseID()
        {
            return 0;
        }
    }
}