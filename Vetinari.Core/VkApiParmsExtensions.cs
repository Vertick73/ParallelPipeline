using VkNet.Model.RequestParams;

namespace Vetinari.Core
{
    public static class VkApiParmsExtensions
    {
        public static GroupsGetParams Clone(this GroupsGetParams old)
        {
            return new GroupsGetParams
            {
                Count = old.Count,
                Offset = old.Offset,
                Extended = old.Extended,
                Fields = old.Fields,
                Filter = old.Filter,
                UserId = old.UserId
            };
        }

        public static WallGetParams Clone(this WallGetParams old)
        {
            return new WallGetParams
            {
                Count = old.Count,
                Offset = old.Offset,
                Extended = old.Extended,
                Fields = old.Fields,
                Filter = old.Filter,
                Domain = old.Domain,
                OwnerId = old.OwnerId
            };
        }
    }
}
