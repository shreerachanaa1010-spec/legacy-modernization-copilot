using System.Threading.Tasks;

namespace LegacySampleProject;

public class CustomerService
{
    public void BadMethod()
    {
        var task = Task.FromResult("Hello");

        var result = task.Result;

        task.Wait();
    }
}