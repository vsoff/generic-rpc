using System;
using System.Diagnostics;

namespace GenericRpc.UnitTests.TestServices
{
    internal sealed class ServerExampleService : ListenerService, IExampleService
    {
        public void Apply() => Debug.WriteLine("Applied");

        public string Concat(string text1, string text2) => string.Concat(text1, text2);

        public int GetIndex() => 4412;

        public void ShowMessage(string message) => Debug.WriteLine($"{ClientContext.Id}: {message}");

        public int Sum(int number1, int number2) => number1 + number2;

        public void MethodWithException() => throw new InvalidOperationException("Some error on the server side");
    }
}