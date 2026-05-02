using System;

namespace Typedown.Core.Models
{
    [Flags]
    public enum KeyboardModifiers
    {
        None = 0,
        Control = 1,
        Menu = 2,
        Shift = 4,
        Windows = 8,
    }

    public enum KeyboardKey
    {
        None = 0,
        Back = 8,
        Enter = 13,
        Shift = 16,
        Control = 17,
        Menu = 18,
        Delete = 46,
        Number0 = 48,
        Number1 = 49,
        Number2 = 50,
        Number3 = 51,
        Number4 = 52,
        Number5 = 53,
        Number6 = 54,
        A = 65,
        B = 66,
        C = 67,
        F = 70,
        H = 72,
        I = 73,
        K = 75,
        L = 76,
        M = 77,
        N = 78,
        O = 79,
        P = 80,
        Q = 81,
        S = 83,
        T = 84,
        U = 85,
        V = 86,
        W = 87,
        X = 88,
        Y = 89,
        Z = 90,
        LeftWindows = 91,
        RightWindows = 92,
        F1 = 112,
        F3 = 114,
        F8 = 119,
        F9 = 120,
        F12 = 123,
    }
}
