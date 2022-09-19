namespace GenericRpc.UnitTests.TestServices
{
    public interface IExampleService
    {
        int Sum(int number1, int number2);
        string Concat(string text1, string text2);
        void ShowMessage(string message);
        int GetIndex();
        void Apply();
        void MethodWithException();
    }
}