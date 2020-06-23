using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Nekoyume.Game.Controller;
using Nekoyume.State;
using Nekoyume.UI.Module;
using Nekoyume.Manager;
using Nekoyume.Model.Item;
using UnityEngine;
using Player = Nekoyume.Game.Character.Player;
using Random = UnityEngine.Random;
using mixpanel;

namespace Nekoyume.UI
{
    public class Menu : Widget
    {
        private const string FirstOpenShopKeyFormat = "Nekoyume.UI.Menu.FirstOpenShopKey_{0}";

        private const string FirstOpenCombinationKeyFormat =
            "Nekoyume.UI.Menu.FirstOpenCombinationKey_{0}";

        private const string FirstOpenRankingKeyFormat = "Nekoyume.UI.Menu.FirstOpenRankingKey_{0}";
        private const string FirstOpenQuestKeyFormat = "Nekoyume.UI.Menu.FirstOpenQuestKey_{0}";

        public MainMenu btnQuest;
        public MainMenu btnCombination;
        public MainMenu btnShop;
        public MainMenu btnRanking;
        public ArenaPendingNCG arenaPendingNCG;
        public SpeechBubble[] SpeechBubbles;
        public GameObject shopExclamationMark;
        public GameObject combinationExclamationMark;
        public GameObject rankingExclamationMark;
        public GameObject questExclamationMark;

        private Coroutine _coroutine;
        private Player _player;

        private Coroutine _coLazyClose;

        protected override void Awake()
        {
            base.Awake();

            SpeechBubbles = GetComponentsInChildren<SpeechBubble>();
            Game.Event.OnRoomEnter.AddListener(b => Show());

            CloseWidget = null;
        }
        
        private void UpdateButtons()
        {
            btnQuest.Update();
            btnCombination.Update();
            btnShop.Update();
            btnRanking.Update();

            var addressHax = ReactiveAvatarState.Address.Value.ToHex();
            var firstOpenCombinationKey = string.Format(FirstOpenCombinationKeyFormat, addressHax);
            var firstOpenShopKey = string.Format(FirstOpenShopKeyFormat, addressHax);
            var firstOpenRankingKey = string.Format(FirstOpenRankingKeyFormat, addressHax);
            var firstOpenQuestKey = string.Format(FirstOpenQuestKeyFormat, addressHax);
            combinationExclamationMark.gameObject.SetActive(
                btnCombination.IsUnlocked &&
                PlayerPrefs.GetInt(firstOpenCombinationKey, 0) == 0);
            shopExclamationMark.gameObject.SetActive(
                btnShop.IsUnlocked &&
                PlayerPrefs.GetInt(firstOpenShopKey, 0) == 0);
            rankingExclamationMark.gameObject.SetActive(
                btnRanking.IsUnlocked &&
                PlayerPrefs.GetInt(firstOpenRankingKey, 0) == 0);
            questExclamationMark.gameObject.SetActive(
                btnQuest.IsUnlocked &&
                PlayerPrefs.GetInt(firstOpenQuestKey, 0) == 0);
        }

        private void HideButtons()
        {
            btnQuest.gameObject.SetActive(false);
            btnCombination.gameObject.SetActive(false);
            btnShop.gameObject.SetActive(false);
            btnRanking.gameObject.SetActive(false);
        }

        public void ShowWorld()
        {
            Show();
            HideButtons();
        }

        public void QuestClick()
        {
            if (!btnQuest.IsUnlocked)
            {
                btnQuest.JingleTheCat();
                return;
            }

            if (questExclamationMark.gameObject.activeSelf)
            {
                var addressHax = ReactiveAvatarState.Address.Value.ToHex();
                var key = string.Format(FirstOpenQuestKeyFormat, addressHax);
                PlayerPrefs.SetInt(key, 1);
            }

            Mixpanel.Track("Unity/Enter Dungeon");
            _coLazyClose = StartCoroutine(CoLazyClose());
            var avatarState = States.Instance.CurrentAvatarState;
            Find<WorldMap>().Show(avatarState.worldInformation);
            AudioController.PlayClick();
            AnalyticsManager.Instance.OnEvent(AnalyticsManager.EventName.ClickMainBattle);
        }

        public void ShopClick()
        {
            if (!btnShop.IsUnlocked)
            {
                btnShop.JingleTheCat();
                return;
            }

            if (shopExclamationMark.gameObject.activeSelf)
            {
                var addressHax = ReactiveAvatarState.Address.Value.ToHex();
                var key = string.Format(FirstOpenShopKeyFormat, addressHax);
                PlayerPrefs.SetInt(key, 1);
            }

            Close();
            Find<Shop>().Show();
            AudioController.PlayClick();
            AnalyticsManager.Instance.OnEvent(AnalyticsManager.EventName.ClickMainShop);
        }

        public void CombinationClick(int slotIndex = -1)
        {
            if (!btnCombination.IsUnlocked)
            {
                btnCombination.JingleTheCat();
                return;
            }

            if (combinationExclamationMark.gameObject.activeSelf)
            {
                var addressHax = ReactiveAvatarState.Address.Value.ToHex();
                var key = string.Format(FirstOpenCombinationKeyFormat, addressHax);
                PlayerPrefs.SetInt(key, 1);
            }

            Close();
            if (slotIndex >= 0)
            {
                Find<Combination>().Show(slotIndex);
            }
            else
            {
                Find<Combination>().Show();
            }

            AudioController.PlayClick();
            AnalyticsManager.Instance.OnEvent(AnalyticsManager.EventName.ClickMainCombination);
        }

        public void RankingClick()
        {
            if (!btnRanking.IsUnlocked)
            {
                btnRanking.JingleTheCat();
                return;
            }

            if (rankingExclamationMark.gameObject.activeSelf)
            {
                var addressHax = ReactiveAvatarState.Address.Value.ToHex();
                var key = string.Format(FirstOpenRankingKeyFormat, addressHax);
                PlayerPrefs.SetInt(key, 1);
            }

            Close();
            Find<RankingBoard>().Show();
            AudioController.PlayClick();
        }

        public override void Show(bool ignoreShowAnimation = false)
        {
            if (!(_coLazyClose is null))
            {
                StopCoroutine(_coLazyClose);
                _coLazyClose = null;
            }

            base.Show(ignoreShowAnimation);

            StartCoroutine(CoStartSpeeches());
            UpdateButtons();
            arenaPendingNCG.Show();
        }

        public override void Close(bool ignoreCloseAnimation = false)
        {
            StopSpeeches();

            Find<Inventory>().Close(ignoreCloseAnimation);
            Find<StatusDetail>().Close(ignoreCloseAnimation);
            Find<Quest>().Close(ignoreCloseAnimation);

            Find<BottomMenu>().Close(true);
            Find<Status>().Close(true);
            base.Close(ignoreCloseAnimation);
        }

        private IEnumerator CoLazyClose(float duration = 1f, bool ignoreCloseAnimation = false)
        {
            StopSpeeches();

            Find<Inventory>().Close(ignoreCloseAnimation);
            Find<StatusDetail>().Close(ignoreCloseAnimation);
            Find<Quest>().Close(ignoreCloseAnimation);

            Find<BottomMenu>().Close(true);
            Find<Status>().Close(true);
            yield return new WaitForSeconds(duration);
            base.Close(ignoreCloseAnimation);
        }

        private IEnumerator CoStartSpeeches()
        {
            yield return new WaitForSeconds(2.0f);

            while (true)
            {
                var n = SpeechBubbles.Length;
                while (n > 1)
                {
                    n--;
                    var k = Mathf.FloorToInt(Random.value * (n + 1));
                    var value = SpeechBubbles[k];
                    SpeechBubbles[k] = SpeechBubbles[n];
                    SpeechBubbles[n] = value;
                }

                foreach (var bubble in SpeechBubbles)
                {
                    yield return StartCoroutine(bubble.CoShowText());
                    yield return new WaitForSeconds(Random.Range(2.0f, 4.0f));
                }
            }
        }

        private void StopSpeeches()
        {
            StopCoroutine(CoStartSpeeches());
            foreach (var bubble in SpeechBubbles)
            {
                bubble.Hide();
            }
        }
    }
}
