using uMMORPG;

namespace uMMORPG.Storage
{
    public interface IStorageAccessRule
    {
        bool CanAccess(Player player);
    }
}