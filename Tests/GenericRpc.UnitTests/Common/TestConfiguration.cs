using System;

namespace GenericRpc.UnitTests.Common
{
    internal static class TestConfiguration
    {
        public static TimeSpan Delay = TimeSpan.FromMilliseconds(250);

        public const string ServerIp = "127.0.0.1";
        public const ushort ServerPort = 21337;
    }
}