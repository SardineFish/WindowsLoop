using System;
using System.IO.MemoryMappedFiles;

namespace WindowSnap
{
    public class SharedMemoryDataPage
    {
        public static SharedMemoryDataPage Self => new SharedMemoryDataPage(SharedMemory.Self);
        
        public int PID
        {
            get => accessor.ReadInt32(Address.PID);
            set
            {
                accessor.Write(Address.PID, value);
                accessor.Flush();
            }
        }
        public int WindowHandle
        {
            get => accessor.ReadInt32(Address.WindowHandle);
            set
            {
                accessor.Write(Address.WindowHandle, value);
                accessor.Flush();
            }
        }
        public int ScreenSnapRectX
        {
            get => accessor.ReadInt32(Address.ScreenSnapRectX);
            set
            {
                accessor.Write(Address.ScreenSnapRectX, value);
                accessor.Flush();
            }
        }
        public int ScreenSnapRectY
        {
            get => accessor.ReadInt32(Address.ScreenSnapRectY);
            set
            {
                accessor.Write(Address.ScreenSnapRectY, value);
                accessor.Flush();
            }
        }
        public bool AttachmentChanged
        {
            get => accessor.ReadBoolean(Address.AttachmentChanged);
            set
            {
                accessor.Write(Address.AttachmentChanged, value);
                accessor.Flush();
            }
        }
        public bool DetachmentChanged
        {
            get => accessor.ReadBoolean(Address.DetachmentChanged);
            set
            {
                accessor.Write(Address.DetachmentChanged, value);
                accessor.Flush();
            }
        }
        public int MessageParam1
        {
            get => accessor.ReadInt32(Address.MessageParam1);
            set
            {
                accessor.Write(Address.MessageParam1, value);
                accessor.Flush();
            }
        }
        public int MessageParam2
        {
            get => accessor.ReadInt32(Address.MessageParam2);
            set
            {
                accessor.Write(Address.MessageParam2, value);
                accessor.Flush();
            }
        }
        public int MessageParam3
        {
            get => accessor.ReadInt32(Address.MessageParam3);
            set
            {
                accessor.Write(Address.MessageParam3, value);
                accessor.Flush();
            }
        }
        public int MessageParam4
        {
            get => accessor.ReadInt32(Address.MessageParam4);
            set
            {
                accessor.Write(Address.MessageParam4, value);
                accessor.Flush();
            }
        }

        readonly MemoryMappedViewAccessor accessor;

        public SharedMemoryDataPage(int index)
        {
            if (index == 0)
                throw new ArgumentOutOfRangeException("index", "Page 0 is not accessible from this class.");

            accessor = SharedMemory.GetPage(index);
        }
        public SharedMemoryDataPage(MemoryMappedViewAccessor accessor) => this.accessor = accessor;
    }
}
