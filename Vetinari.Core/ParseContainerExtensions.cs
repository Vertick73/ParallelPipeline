using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using VkNet.Enums.Filters;
using VkNet.Enums.SafetyEnums;
using VkNet.Model;
using VkNet.Model.Attachments;
using VkNet.Model.RequestParams;

namespace Vetinari.Core
{
    public static class ParseContainerExtensions
    {
        public static void GetGroups(this ParseContainer container, int queueLength, int threadCount, long userId,
            GroupsGetParams initParams = null)
        {
            var perStep = 1000;
            initParams ??= new GroupsGetParams
            {
                Extended = true,
                Fields = GroupsFields.All
            };
            var totalNeedCount = initParams.Count;
            initParams.Count = initParams.Count == null ? perStep :
                initParams.Count > perStep ? perStep : initParams.Count;
            initParams.Offset ??= 0;

            async Task<IEnumerable<Group>> ParseFunc(long userId)
            {
                var funkParams = initParams.Clone();
                funkParams.UserId = userId;
                var res = await container.Vk.Groups.GetAsync(funkParams).ConfigureAwait(false);
                var needCount = totalNeedCount ?? (long)res.TotalCount;

                if (needCount > res.Count)
                {
                    List<Group> newRes = new(res) { Capacity = (int)needCount };
                    for (var i = res.Count; i < needCount; i += perStep)
                    {
                        funkParams.Count = i + perStep > needCount ? needCount - i : perStep;
                        funkParams.Offset += i;
                        var nextRes = await container.Vk.Groups.GetAsync(funkParams).ConfigureAwait(false);
                        newRes.AddRange(nextRes);
                    }

                    return newRes;
                }

                return res;
            }

            var next = container.AddNext<long, Group>(queueLength, threadCount, ParseFunc);
            next.SetInput(new[] { userId });
        }

        public static ParseContainer<Group, Post> GetPosts<T>(this ParseContainer<T, Group> container, int queueLength,
            int threadCount, WallGetParams initParams = null)
        {
            var perStep = 100u;
            initParams ??= new WallGetParams
            {
                Count = 300
            };
            var totalNeedCount = initParams.Count;
            initParams.Count = initParams.Count > perStep ? perStep : initParams.Count;

            async Task<IEnumerable<Post>> ParseFunc(Group group)
            {
                var funkParams = initParams.Clone();
                funkParams.OwnerId = -group.Id;
                var res = await container.Vk.Wall.GetAsync(funkParams).ConfigureAwait(false);
                var needCount = totalNeedCount == 0 ? res.TotalCount : totalNeedCount;

                if (needCount > (ulong)res.WallPosts.Count)
                {
                    List<Post> newRes = new(res.WallPosts) { Capacity = (int)needCount };
                    for (var i = (ulong)res.WallPosts.Count; i < needCount; i += perStep)
                    {
                        funkParams.Count = i + perStep > needCount ? needCount - i : perStep;
                        funkParams.Offset += i;
                        var nextRes = await container.Vk.Wall.GetAsync(funkParams).ConfigureAwait(false);
                        newRes.AddRange(nextRes.WallPosts);
                    }

                    return newRes;
                }

                return res.WallPosts;
            }

            return container.AddNext(queueLength, threadCount, ParseFunc);
        }

        public static ParseContainer<Post, Post> PostsIsLiked<T>(this ParseContainer<T, Post> container,
            int queueLength, int threadCount, long userId)
        {
            async Task<IEnumerable<Post>> ParseFunc(Post post)
            {
                if (await container.Vk.Likes.IsLikedAsync(LikeObjectType.Post, post.Id.Value, userId, post.OwnerId))
                    return new[] { post };
                return Array.Empty<Post>();
            }

            return container.AddNext(queueLength, threadCount, ParseFunc);
        }
    }
}
