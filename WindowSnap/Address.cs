namespace WindowSnap
{
    public class Address
    {
        public const int PID = 0x00;
        public const int ScreenSnapRectPos = 0x04;
        public const int ScreenSnapRectX = 0x04;
        public const int ScreenSnapRectY = 0x08;
        public const int AttachmentChanged = 0x10;
        public const int DetachmentChanged = 0x11;
        public const int MessageParam1 = 0x20;
        public const int MessageParam2 = 0x24;
        public const int MessageParam3 = 0x28;
        public const int MessageParam4 = 0x2c;
        public const int AttachedWindowPIDs = 0x40;


        public const int ViewRectMin = 0x14;
        public const int ViewRectMax = 0x1c;
        public const int PlayerPosition = 0x24;
        public const int PlayerVelocity = 0x2c;
        public const int PlayerAnimTime = 0x34;
        public const int CameraPos = 0x38;

        public const int Preserve = 0x40;
    }
}
