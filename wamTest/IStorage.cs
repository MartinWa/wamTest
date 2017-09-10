using System.ComponentModel;

namespace wamTest
{
    public interface IStorage
    {
        IContainer GetContainer(string containerName);
    }
}