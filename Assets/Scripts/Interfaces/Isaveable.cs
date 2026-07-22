using Ashfall.Core;

namespace Ashfall.Interfaces
{
    // anything that reads/writes its state to the save file
    public interface ISaveable
    {
        void Save(SaveData data);
        void Load(SaveData data);
    }
}