namespace WindowSnap
{
    public class Address
    {
        public const int PID = 0;
        public const int ScreenSnapRectPos = 4;
        public const int ScreenSnapRectX = 4;
        public const int ScreenSnapRectY = 8;
        public const int AttachedWindowPID = 12;
        public const int AttachmentChanged = 16;


        public const int ViewRectMin = 0x14;
        public const int ViewRectMax = 0x1c;
        public const int PlayerPosition = 0x24;
        public const int PlayerVelocity = 0x2c;
        public const int PlayerAnimTime = 0x34;
        public const int CameraPos = 0x38;

        public const int Preserve = 0x40;
    }
}
