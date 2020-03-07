﻿using KaguyaProjectV2.KaguyaBot.Core.Interfaces;
using LinqToDB.Mapping;
using System;

namespace KaguyaProjectV2.KaguyaBot.DataStorage.DbData.Models
{
    [Table(Name = "supporterkeys")]
    public class SupporterKey : IKey, IKaguyaQueryable<SupporterKey>, IKaguyaUnique<SupporterKey>
    {
        [PrimaryKey]
        [Column(Name = "Key"), NotNull]
        public string Key { get; set; }
        [Column(Name = "LengthInSeconds"), NotNull]
        public long LengthInSeconds { get; set; }
        [Column(Name = "KeyCreatorId"), NotNull]
        public ulong KeyCreatorId { get; set; }
        [Column(Name = "UserId"), Nullable]
        public ulong UserId { get; set; }
        /// <summary>
        /// If this key is tied to a user, the time (as OADate) when the key expires.
        /// </summary>
        [Column(Name = "Expiration"), Nullable]
        public double Expiration { get; set; }
    }
}