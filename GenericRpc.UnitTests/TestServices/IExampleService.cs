namespace GenericRpc.UnitTests.TestServices
{
    public interface IExampleService
    {
        int Sum(int number1, int number2);
        void ShowMessage(string message);
    }
}