﻿using System;
using TMPro;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UI.Extensions;

namespace Nekoyume.UI.Module.Arena.Board
{
    using UniRx;

    [Serializable]
    public class ArenaBoardPlayerItemData
    {
        public string name;
        public string cp;
        public string rating;
        public string plusRating;
    }

    public class ArenaBoardPlayerScrollContext : FancyScrollRectContext
    {
        public int selectedIndex = -1;
        public Action<int> onCellClicked;
    }

    public class ArenaBoardPlayerCell
        : FancyScrollRectCell<ArenaBoardPlayerItemData, ArenaBoardPlayerScrollContext>
    {
        [SerializeField]
        private DetailedCharacterView _characterView;

        [SerializeField]
        private TextMeshProUGUI _nameText;

        [SerializeField]
        private TextMeshProUGUI _ratingText;

        [SerializeField]
        private TextMeshProUGUI _cpText;

        [SerializeField]
        private TextMeshProUGUI _plusRatingText;

        [SerializeField]
        private ConditionalButton _choiceButton;

        [SerializeField]
        private float _tempOffsetX;

        private ArenaBoardPlayerItemData _currentData;

#if UNITY_EDITOR
        [ReadOnly]
        public float _currentPosition;
#else
        private float _currentPosition;
#endif

        private void Awake()
        {
            _choiceButton.OnClickSubject
                .Subscribe(_ => Context.onCellClicked?.Invoke(Index))
                .AddTo(gameObject);
        }

        public override void UpdateContent(ArenaBoardPlayerItemData itemData)
        {
            _currentData = itemData;
            _nameText.text = _currentData.name;
            _cpText.text = _currentData.cp;
            _ratingText.text = _currentData.rating;
            _plusRatingText.text = _currentData.plusRating;
        }

        public override void UpdatePosition(float position)
        {
            _currentPosition = position;
        }

        protected override void UpdatePosition(float normalizedPosition, float localPosition)
        {
            base.UpdatePosition(normalizedPosition, localPosition);
            var offsetX = math.sin(_tempOffsetX);
            transform.localPosition += Vector3.right * offsetX;
        }
    }
}
