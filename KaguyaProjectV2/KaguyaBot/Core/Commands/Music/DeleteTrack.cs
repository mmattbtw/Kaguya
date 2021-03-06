using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord.Commands;
using KaguyaProjectV2.KaguyaBot.Core.Attributes;
using KaguyaProjectV2.KaguyaBot.Core.Global;
using KaguyaProjectV2.KaguyaBot.Core.KaguyaEmbed;
using Victoria;
using Victoria.Enums;
using Victoria.Interfaces;

namespace KaguyaProjectV2.KaguyaBot.Core.Commands.Music
{
    public class DeleteTrack : KaguyaBase
    {
        [MusicCommand]
        [Command("DeleteTrack")]
        [Alias("deltrack", "dt")]
        [Summary("Deletes a track from the current music player's queue.")]
        [Remarks("<track num> {...}\n3 (=> Removes track #3 from the queue)\n" +
                 "3 5 7 (=> Removes tracks #3, #5, #7 from the queue)")]
        public async Task Command(params int[] args)
        {
            LavaPlayer player = ConfigProperties.LavaNode.GetPlayer(Context.Guild);
            PlayerState playerState = player.PlayerState;
            DefaultQueue<IQueueable> queue = player.Queue;

            if (!(playerState == PlayerState.Playing || playerState == PlayerState.Paused) || !queue.Any())
            {
                await SendBasicErrorEmbedAsync("The player must be either playing " +
                                               "or paused and must have at least 1 song " +
                                               "in the queue.");

                return;
            }

            int failedAttempts = 0;
            const int LIMIT_ATTEMPTS = 3; // Maximum amount of error messages to send in one session.

            var descSb = new StringBuilder();

            foreach (int num in args.OrderByDescending(x => x))
            {
                if (failedAttempts >= LIMIT_ATTEMPTS) return;

                IQueueable match = queue.ElementAtOrDefault(num - 1);
                if (match == null)
                {
                    await SendBasicErrorEmbedAsync($"{Context.User.Mention} There is no track `#{num}` in the current queue.");
                    failedAttempts++;

                    continue;
                }

                player.Queue.RemoveAt(num - 1);
                descSb.AppendLine($"Successfully removed track `#{num}`.");
            }

            var embed = new KaguyaEmbedBuilder
            {
                Description = descSb.ToString()
            };

            await SendEmbedAsync(embed);
        }
    }
}