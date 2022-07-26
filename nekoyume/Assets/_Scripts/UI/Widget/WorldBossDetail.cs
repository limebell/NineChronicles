﻿using System;
using System.Collections.Generic;
using System.Linq;
using Nekoyume.Game.Controller;
using Nekoyume.Model.State;
using Nekoyume.UI.Module;
using Nekoyume.UI.Module.WorldBoss;
using UnityEngine;
using UnityEngine.UI;
using Toggle = Nekoyume.UI.Module.Toggle;

namespace Nekoyume.UI
{
    using UniRx;

    public class WorldBossDetail : Widget
    {
        public enum ToggleType
        {
            Information,
            PreviousRank,
            Rank,
            Reward
        }

        [Serializable]
        private struct CategoryToggle
        {
            public Toggle Toggle;
            public ToggleType Type;
            public WorldBossDetailItem Item;
        }

        [SerializeField]
        private List<CategoryToggle> categoryToggles;

        [SerializeField]
        private Button backButton;

        private readonly ReactiveProperty<ToggleType> _selectedItemSubType = new(ToggleType.Reward);

        protected override void Awake()
        {
            base.Awake();

            CloseWidget = () => { Close(); };
            backButton.OnClickAsObservable().Subscribe(_ => { Close(); }).AddTo(gameObject);
            foreach (var categoryToggle in categoryToggles)
            {
                categoryToggle.Toggle.onValueChanged.AddListener(value =>
                {
                    if (!value) return;
                    AudioController.PlayClick();
                    _selectedItemSubType.SetValueAndForceNotify(categoryToggle.Type);
                });
            }

            _selectedItemSubType.Subscribe(UpdateView).AddTo(gameObject);
        }

        public void Show(ToggleType toggleType)
        {
            categoryToggles.FirstOrDefault(x => x.Type.Equals(toggleType)).Toggle.isOn = true;
            _selectedItemSubType.SetValueAndForceNotify(toggleType);
            base.Show();
        }

        public void UpdateReward()
        {
            var reward = categoryToggles.FirstOrDefault(x => x.Type == ToggleType.Reward);
            if (reward.Item is WorldBossReward worldBossReward)
            {
                worldBossReward.ShowAsync(false);
            }
        }

        public override void Close(bool ignoreCloseAnimation = false)
        {
            var header = Find<HeaderMenuStatic>();
            header.Show(HeaderMenuStatic.AssetVisibleState.WorldBoss);
            base.Close(ignoreCloseAnimation);
        }

        private void UpdateView(ToggleType toggleType)
        {
            foreach (var toggle in categoryToggles)
            {
                if (toggle.Type.Equals(toggleType))
                {
                    toggle.Item.gameObject.SetActive(true);
                    switch (toggle.Item)
                    {
                        case WorldBossReward reward:
                            reward.ShowAsync();
                            break;
                        case WorldBossInformation information:
                            break;
                        case WorldBossRank rank:
                            break;
                        case WorldBossPreviousRank previousRank:
                            break;
                    }
                }
                else
                {
                    toggle.Item.gameObject.SetActive(false);
                }
            }
        }
    }
}
