using System.Diagnostics;

namespace GenericRpc.UnitTests.TestServices
{
    internal sealed class ServerExampleService : IExampleService
    {
        private readonly ClientContext _context;

        public ServerExampleService(ClientContext context)
        {
            _context = context;
        }

        public void Apply() => Debug.WriteLine("Applied");

        public string Concat(string text1, string text2) => string.Concat(text1, text2);

        public int GetIndex() => 4412;

        public void ShowMessage(string message) => Debug.WriteLine(message);

        public int Sum(int number1, int number2) => number1 + number2;
    }
}