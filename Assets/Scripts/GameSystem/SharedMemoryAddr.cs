using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class GameDataAddr : WindowSnap.Address
{
    public const int ViewRectMin =      PreserveEnd + 0x0;
    public const int ViewRectMax =      PreserveEnd + 0x8;
    public const int PlayerPosition =   PreserveEnd + 0x10;
    public const int PlayerVelocity =   PreserveEnd + 0x18;
    public const int PlayerAnimTime =   PreserveEnd + 0x20;
    public const int CameraPos =        PreserveEnd + 0x24;
}
