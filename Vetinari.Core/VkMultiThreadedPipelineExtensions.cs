using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using VkNet.Abstractions;
using VkNet.Enums.Filters;
using VkNet.Enums.SafetyEnums;
using VkNet.Model;
using VkNet.Model.Attachments;
using VkNet.Model.RequestParams;

namespace Vetinari.Core
{
    public static class VkMultiThreadedPipelineExtensions
    {
        public static VkMultiThreadedPipeline<long, Group> GetGroups(this VkPipeline multiThreadedPipelinePart,
            int queueLength,
            int threadCount, long userId,
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
                var res = await multiThreadedPipelinePart.VkApi.Groups.GetAsync(funkParams).ConfigureAwait(false);
                var needCount = totalNeedCount ?? (long)res.TotalCount;

                if (needCount > res.Count)
                {
                    List<Group> newRes = new(res) { Capacity = (int)needCount };
                    for (var i = res.Count; i < needCount; i += perStep)
                    {
                        funkParams.Count = i + perStep > needCount ? needCount - i : perStep;
                        funkParams.Offset += i;
                        var nextRes = await multiThreadedPipelinePart.VkApi.Groups.GetAsync(funkParams)
                            .ConfigureAwait(false);
                        newRes.AddRange(nextRes);
                    }

                    return newRes;
                }

                return res;
            }

            var next = new VkMultiThreadedPipeline<long, Group>(queueLength, threadCount, ParseFunc)
                { VkApi = multiThreadedPipelinePart.VkApi };
            next.SetInput(new[] { userId });
            return next;
        }

        public static VkMultiThreadedPipeline<Group, Post> GetPosts<T>(
            this VkMultiThreadedPipeline<T, Group> multiThreadedPipeline,
            int queueLength,
            int threadCount, WallGetParams initParams = null, IVkApi vkApi = null)
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
                if (!group.CanSeeAllPosts) return Array.Empty<Post>();
                var funkParams = initParams.Clone();
                funkParams.OwnerId = -group.Id;
                var res = await multiThreadedPipeline.VkApi.Wall.GetAsync(funkParams).ConfigureAwait(false);
                var needCount = totalNeedCount == 0 ? res.TotalCount : totalNeedCount;

                if (needCount > (ulong)res.WallPosts.Count)
                {
                    List<Post> newRes = new(res.WallPosts) { Capacity = (int)needCount };
                    for (var i = (ulong)res.WallPosts.Count; i < needCount; i += perStep)
                    {
                        funkParams.Count = i + perStep > needCount ? needCount - i : perStep;
                        funkParams.Offset += i;
                        var nextRes = await multiThreadedPipeline.VkApi.Wall.GetAsync(funkParams).ConfigureAwait(false);
                        newRes.AddRange(nextRes.WallPosts);
                    }

                    return newRes;
                }

                return res.WallPosts;
            }

            return multiThreadedPipeline.AddNext(queueLength, threadCount, ParseFunc, vkApi: vkApi);
        }

        public static VkMultiThreadedPipeline<Post, Post> PostsIsLiked<T>(
            this VkMultiThreadedPipeline<T, Post> multiThreadedPipeline,
            int queueLength, int threadCount, long userId, IVkApi vkApi = null)
        {
            async Task<IEnumerable<Post>> ParseFunc(Post post)
            {
                if (await multiThreadedPipeline.VkApi.Likes.IsLikedAsync(LikeObjectType.Post, post.Id.Value, userId,
                    post.OwnerId))
                    return new[] { post };
                return Array.Empty<Post>();
            }

            return multiThreadedPipeline.AddNext(queueLength, threadCount, ParseFunc, vkApi: vkApi);
        }
    }
}
