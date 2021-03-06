﻿// using System;
// using System.Threading.Tasks;
// using KaguyaProjectV2.KaguyaBot.DataStorage.DbData.Models;
//
// namespace KaguyaProjectV2.KaguyaBot.Core.Commands.Currency.Poker
// {
//     public static class PokerEvent
//     {
//         public static event Func<PokerGameEventArgs, Task> OnTurnEnd;
//         public static event Func<Task> OnGameFinished;
//
//         public static void TurnTrigger(PokerGameEventArgs e)
//         {
//             // todo: Revoke invocation of the event when a player is finished with a game of poker.
//             OnTurnEnd?.Invoke(e);
//         }
//
//         public static void GameFinishedTrigger() => OnGameFinished?.Invoke();
//     }
//     public class PokerGameEventArgs : EventArgs
//     {
//         public User User { get; }
//         public int RaisePoints { get; }
//         public PokerGameAction Action { get; }
//
//         public PokerGameEventArgs(User user, int raisePoints, PokerGameAction action)
//         {
//             this.User = user;
//             this.RaisePoints = raisePoints;
//             this.Action = action;
//         }
//     }
// }

