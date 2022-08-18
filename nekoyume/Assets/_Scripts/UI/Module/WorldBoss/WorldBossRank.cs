﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Libplanet;
using Nekoyume.Helper;
using Nekoyume.L10n;
using Nekoyume.State;
using Nekoyume.UI.Model;
using Nekoyume.UI.Scroller;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI.Module.WorldBoss
{
    using UniRx;

    public class WorldBossRank : WorldBossDetailItem
    {
        public enum Status
        {
            PreviousSeason,
            Season,
        }

        private const int LimitCount = 100;

        [SerializeField]
        private WorldBossRankScroll scroll;

        [SerializeField]
        private Image bossImage;

        [SerializeField]
        private TextMeshProUGUI bossName;

        [SerializeField]
        private TextMeshProUGUI rankTitle;

        [SerializeField]
        private TextMeshProUGUI totalUsers;

        [SerializeField]
        private Button refreshButton;

        [SerializeField]
        private GameObject apiMissing;

        [SerializeField]
        private GameObject noSeasonInfo;

        [SerializeField]
        private List<GameObject> queryLoadingObjects;

        [SerializeField]
        private WorldBossRankItemView myInfo;

        private readonly Dictionary<Status, WorldBossRankItems> _cachedItems = new();
        private Status _status;
        private Address currentAvatarAddress;

        private void Awake()
        {
            refreshButton.OnClickAsObservable()
                .Subscribe(_ => RefreshAsync()).AddTo(gameObject);
        }

        public async void ShowAsync(Status status)
        {
            _status = status;
            ResetInformation(status);

            var raidId = GetRaidId(status);
            if (!WorldBossFrontHelper.TryGetRaid(raidId, out _))
            {
                noSeasonInfo.SetActive(true);
                return;
            }

            if (!Game.Game.instance.ApiClient.IsInitialized)
            {
                apiMissing.SetActive(true);
                return;
            }

            SetActiveQueryLoading(true);
            await SetItemsAsync(raidId, status);
            UpdateBossInformation(raidId);
            UpdateRecord(status);
            SetActiveQueryLoading(false);
        }

        private void RefreshAsync()
        {
            _cachedItems.Remove(_status);
            ShowAsync(_status);
        }

        private static int GetRaidId(Status status)
        {
            return status switch
            {
                Status.PreviousSeason => WorldBossFrontHelper.TryGetPreviousRow(
                    Game.Game.instance.Agent.BlockIndex, out var row)
                    ? row.Id
                    : 0,
                Status.Season => WorldBossFrontHelper.TryGetCurrentRow(
                    Game.Game.instance.Agent.BlockIndex, out var row)
                    ? row.Id
                    : 0,
                _ => throw new ArgumentOutOfRangeException(nameof(status), status, null)
            };
        }

        private void ResetInformation(Status status)
        {
            rankTitle.text = status == Status.PreviousSeason
                ? L10nManager.Localize("UI_PREVIOUS_SEASON_RANK")
                : L10nManager.Localize("UI_LEADERBOARD");
            bossName.text = "-";
            totalUsers.text = $"-";
            bossImage.enabled = false;
            myInfo.gameObject.SetActive(false);
            noSeasonInfo.SetActive(false);
            apiMissing.SetActive(false);
            SetActiveQueryLoading(false);
        }

        private void UpdateBossInformation(int raidId)
        {
            if (!WorldBossFrontHelper.TryGetRaid(raidId, out var row))
            {
                return;
            }

            if (!WorldBossFrontHelper.TryGetBossData(row.BossId, out var data))
            {
                return;
            }

            bossName.text = data.name;
            bossImage.enabled = true;
            bossImage.sprite = data.illustration;
        }

        private async Task SetItemsAsync(int raidId, Status status)
        {
            var avatarAddress = States.Instance.CurrentAvatarState.address;
            if (_cachedItems.ContainsKey(status) && _cachedItems[status].AvatarAddress == avatarAddress)
            {
                return;
            }

            var response = await WorldBossQuery.QueryRankingAsync(raidId, avatarAddress);
            var records = response?.WorldBossRanking ?? new List<WorldBossRankingRecord>();
            var userCount = response?.WorldBossTotalUsers ?? 0;
            var myRecord = records.FirstOrDefault(record => record.Address == avatarAddress.ToHex());

            if (records.Count > LimitCount)
            {
                records = records.Where(record => record.Address != avatarAddress.ToHex())
                    .ToList();
            }

            var items = new WorldBossRankItems(
                records.Select(record => new WorldBossRankItem(record)).ToList(),
                myRecord != null ? new WorldBossRankItem(myRecord) : null,
                avatarAddress,
                userCount);

            _cachedItems[status] = items;
        }

        private void UpdateRecord(Status status)
        {
            var items = _cachedItems[status];
            myInfo.gameObject.SetActive(items.MyItem != null);
            myInfo.Set(items.MyItem, null);
            scroll.UpdateData(items.UserItems);
            totalUsers.text = items.UserCount > 0 ? $"{items.UserCount:#,0}" : "-";
        }

        private void SetActiveQueryLoading(bool value)
        {
            foreach (var o in queryLoadingObjects)
            {
                o.SetActive(value);
            }
        }
    }
}
