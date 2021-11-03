using VkNet.Abstractions;

namespace Vetinari.Core
{
    public class VkPipeline
    {
        public VkPipeline(IVkApi vkApi)
        {
            VkApi = vkApi;
        }

        public IVkApi VkApi { get; set; }
    }
}
