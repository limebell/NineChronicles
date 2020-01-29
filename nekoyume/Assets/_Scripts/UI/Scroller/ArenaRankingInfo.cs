using Nekoyume.Game.Controller;
using Nekoyume.Helper;
using Nekoyume.State;
using TMPro;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI.Scroller
{
    public class ArenaRankingInfo : MonoBehaviour
    {
        public Button avatarInfoButton;
        public Button challengeButton;
        public TextMeshProUGUI rankText;
        public Image icon;
        public TextMeshProUGUI levelText;
        public TextMeshProUGUI idText;
        public TextMeshProUGUI ratingText;
        public TextMeshProUGUI cpText;
        public Image flag;
        public Tween.DOTweenRectTransformMoveBy tweenMove;
        public Tween.DOTweenGroupAlpha tweenAlpha;

        public System.Action<ArenaRankingInfo> onClickChallenge;
        
        public State.ArenaInfo AvatarInfo { get; private set; }

        private void Awake()
        {
            challengeButton.OnClickAsObservable().Subscribe(_ =>
            {
                AudioController.PlayClick();
                onClickChallenge.Invoke(this);
            }).AddTo(gameObject);
        }

        public void Set(int ranking, ArenaInfo arenaInfo)
        {
            AvatarInfo = arenaInfo;
            
            rankText.text = ranking.ToString();
            icon.sprite = SpriteHelper.GetItemIcon(arenaInfo.ArmorId);
            icon.SetNativeSize();
            levelText.text = arenaInfo.Level.ToString();
            idText.text = arenaInfo.AvatarName;
            ratingText.text = arenaInfo.Score.ToString();
            cpText.text = arenaInfo.CombatPoint.ToString();
            tweenMove.StartDelay = ranking * 0.16f;
            tweenAlpha.StartDelay = ranking * 0.16f;
        }
    }
}
