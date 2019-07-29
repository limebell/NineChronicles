using System;
using System.Collections;
using System.Collections.Generic;
using Nekoyume.BlockChain;
using Nekoyume.Model;
using Nekoyume.Pattern;
using UniRx;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Nekoyume.Game.Controller
{
    public class AudioController : MonoSingleton<AudioController>
    {
        public struct AudioInfo
        {
            public readonly AudioSource source;
            public readonly float volume;

            public AudioInfo(AudioSource source)
            {
                this.source = source;
                volume = source.volume;
            }
        }
        
        public struct MusicCode
        {
            public const string Title = "bgm_title";
            public const string Prologue = "bgm_prologue";
            public const string SelectCharacter = "bgm_selectcharacter";
            public const string Main = "bgm_main";
            public const string Shop = "bgm_shop";
            public const string Ranking = "bgm_ranking";
            public const string Combination = "bgm_combination";
            public const string StageGreen = "bgm_stage_green";
            public const string StageBlue = "bgm_stage_blue";
            public const string StageOrange = "bgm_stage_orange";
            public const string Boss1 = "bgm_boss1";
            public const string Win = "bgm_win";
            public const string Lose = "bgm_lose";
        }

        public struct SfxCode
        {
            public const string Select = "sfx_select";
            public const string Cash = "sfx_cash";
            public const string InputItem = "sfx_inputitem";
            public const string Success = "sfx_success";
            public const string Failed = "sfx_failed";
            public const string ChainMail2 = "sfx_chainmail2";
            public const string Equipment = "sfx_equipmount";
            public const string FootStepLow = "sfx_footstep-low";
            public const string FootStepHigh = "sfx_footstep-high";
            public const string DamageNormal = "sfx_damage_normal";
            public const string Critical01 = "sfx_critical01";
            public const string Critical02 = "sfx_critical02";
            public const string LevelUp = "sfx_levelup";
            public const string Cancel = "sfx_cancel";
            public const string Popup = "sfx_popup";
            public const string Click = "sfx_click";
            public const string Swing = "sfx_swing";
            public const string Swing2 = "sfx_swing2";
            public const string Swing3 = "sfx_swing3";
            public const string BattleCast = "sfx_battle_cast";
            public const string RewardItem = "sfx_reward_item";
            public const string BuyItem = "sfx_buy_item";
        }

//        4. 스킬 sfx
//        - 무속성/물/불/바람/대지 일격/연사/범위공격 15가지 경우의 수 있음
//        - 연사나 범위공격의 경우, 히트 시마다 별도의 sfx를 삽입할 수 있어야함

        private enum State
        {
            None = -1,
            InInitializing,
            Idle
        }

        private State CurrentState { get; set; }

        protected override bool ShouldRename => true;

        private readonly Dictionary<string, AudioSource> _musicPrefabs = new Dictionary<string, AudioSource>();
        private readonly Dictionary<string, AudioSource> _sfxPrefabs = new Dictionary<string, AudioSource>();

        private readonly Dictionary<string, Stack<AudioInfo>> _musicPool =
            new Dictionary<string, Stack<AudioInfo>>();

        private readonly Dictionary<string, Stack<AudioInfo>> _sfxPool =
            new Dictionary<string, Stack<AudioInfo>>();

        private readonly Dictionary<string, List<AudioInfo>> _musicPlaylist =
            new Dictionary<string, List<AudioInfo>>();

        private readonly Dictionary<string, List<AudioInfo>> _sfxPlaylist =
            new Dictionary<string, List<AudioInfo>>();

        private readonly Dictionary<string, List<AudioInfo>> _shouldRemoveMusic =
            new Dictionary<string, List<AudioInfo>>();

        private readonly Dictionary<string, List<AudioInfo>> _shouldRemoveSfx =
            new Dictionary<string, List<AudioInfo>>();

        private Coroutine _fadeInMusic;
        private readonly List<Coroutine> _fadeOutMusics = new List<Coroutine>();

        #region Mono

        protected override void Awake()
        {
            base.Awake();

            CurrentState = State.None;

#if !UNITY_EDITOR
            ReactiveAgentState.Gold.ObserveOnMainThread().Subscribe(_ => PlaySfx(SfxCode.Cash)).AddTo(this);
#endif
        }

        private void Update()
        {
            CheckPlaying(_musicPool, _musicPlaylist, _shouldRemoveMusic);
            CheckPlaying(_sfxPool, _sfxPlaylist, _shouldRemoveSfx);
        }

        private void CheckPlaying<T1, T2>(T1 pool, T2 playList, T2 shouldRemove)
            where T1 : IDictionary<string, Stack<AudioInfo>> where T2 : IDictionary<string, List<AudioInfo>>
        {
            foreach (var pair in playList)
            {
                foreach (var audioInfo in pair.Value)
                {
                    if (audioInfo.source.isPlaying)
                    {
                        continue;
                    }

                    if (!shouldRemove.ContainsKey(pair.Key))
                    {
                        shouldRemove.Add(pair.Key, new List<AudioInfo>());
                    }

                    shouldRemove[pair.Key].Add(audioInfo);
                }
            }

            foreach (var pair in shouldRemove)
            {
                foreach (var audioSource in pair.Value)
                {
                    playList[pair.Key].Remove(audioSource);
                    Push(pool, pair.Key, audioSource);

                    if (playList[pair.Key].Count == 0)
                    {
                        playList.Remove(pair.Key);
                    }
                }
            }

            shouldRemove.Clear();
        }

        #endregion

        #region Initialize

        public void Initialize()
        {
            if (CurrentState != State.None)
            {
                throw new FsmException("Already initialized.");
            }

            CurrentState = State.InInitializing;

            InitializeInternal("Audio/Music/Prefabs", _musicPrefabs);
            InitializeInternal("Audio/Sfx/Prefabs", _sfxPrefabs);

            CurrentState = State.Idle;
        }

        private void InitializeInternal(string folderPath, IDictionary<string, AudioSource> collection)
        {
            var assets = Resources.LoadAll<GameObject>(folderPath);
            foreach (var asset in assets)
            {
                var audioSource = asset.GetComponent<AudioSource>();
                if (ReferenceEquals(audioSource, null))
                {
                    throw new NotFoundComponentException<AudioSource>();
                }

                collection.Add(asset.name, audioSource);
            }
        }
        
        #endregion

        #region Play

        public void PlayMusic(string audioName, float fadeIn = 0.8f)
        {
            if (CurrentState != State.Idle)
            {
                throw new FsmException("Not initialized.");
            }

            if (string.IsNullOrEmpty(audioName))
            {
                throw new ArgumentNullException();
            }

            StopMusicAll(0.5f);

            var audioInfo = PopFromMusicPool(audioName);
            Push(_musicPlaylist, audioName, audioInfo);
            _fadeInMusic = StartCoroutine(CoFadeIn(audioInfo, fadeIn));
        }

        public void PlaySfx(string audioName)
        {
            if (CurrentState != State.Idle)
            {
                throw new FsmException("Not initialized.");
            }

            if (string.IsNullOrEmpty(audioName))
            {
                throw new ArgumentNullException();
            }

            var audioInfo = PopFromSfxPool(audioName);
            Push(_sfxPlaylist, audioName, audioInfo);
            audioInfo.source.Play();
        }

        public void StopAll(float musicFadeOut = 1f)
        {
            StopMusicAll(musicFadeOut);
            StopSfxAll();
        }

        private void StopMusicAll(float fadeOut = 1f)
        {
            if (CurrentState != State.Idle)
            {
                throw new FsmException("Not initialized.");
            }

            if (_fadeInMusic != null)
            {
                StopCoroutine(_fadeInMusic);
            }
            foreach (var fadeOutMusic in _fadeOutMusics)
            {
                if (fadeOutMusic != null)
                {
                    StopCoroutine(fadeOutMusic);   
                }
            }
            _fadeOutMusics.Clear();

            foreach (var pair in _musicPlaylist)
            {
                foreach (var audioSource in pair.Value)
                {
                    _fadeOutMusics.Add(StartCoroutine(CoFadeOut(audioSource, fadeOut)));
                }
            }
        }

        private void StopSfxAll()
        {
            foreach (var stack in _sfxPlaylist)
            {
                foreach (var audioInfo in stack.Value)
                {
                    audioInfo.source.Stop();
                }
            }
        }

        #endregion

        #region Pool

        private AudioSource Instantiate(string audioName, IDictionary<string, AudioSource> collection)
        {
            if (!collection.ContainsKey(audioName))
            {
                throw new KeyNotFoundException($"Not found AudioSource `{audioName}`.");
            }

            return Instantiate(collection[audioName], transform);
        }

        private AudioInfo PopFromMusicPool(string audioName)
        {
            if (!_musicPool.ContainsKey(audioName))
            {
                return new AudioInfo(Instantiate(audioName, _musicPrefabs));
            }

            var stack = _musicPool[audioName];

            return stack.Count > 0 ? stack.Pop() : new AudioInfo(Instantiate(audioName, _musicPrefabs));
        }

        private AudioInfo PopFromSfxPool(string audioName)
        {
            if (!_sfxPool.ContainsKey(audioName))
            {
                return new AudioInfo(Instantiate(audioName, _sfxPrefabs));
            }

            var stack = _sfxPool[audioName];

            return stack.Count > 0 ? stack.Pop() : new AudioInfo(Instantiate(audioName, _sfxPrefabs));
        }

        private static void Push(IDictionary<string, List<AudioInfo>> collection, string audioName, AudioInfo audioInfo)
        {
            if (collection.ContainsKey(audioName))
            {
                collection[audioName].Add(audioInfo);
            }
            else
            {
                var list = new List<AudioInfo> {audioInfo};
                collection.Add(audioName, list);
            }
        }

        private static void Push(IDictionary<string, Stack<AudioInfo>> collection, string audioName, AudioInfo audioInfo)
        {
            if (collection.ContainsKey(audioName))
            {
                collection[audioName].Push(audioInfo);
            }
            else
            {
                var stack = new Stack<AudioInfo>();
                stack.Push(audioInfo);
                collection.Add(audioName, stack);
            }
        }

        #endregion

        #region Fade

        private static IEnumerator CoFadeIn(AudioInfo audioInfo, float duration)
        {
            audioInfo.source.volume = 0f;
            audioInfo.source.Play();

            var deltaTime = 0f;
            while (deltaTime < duration)
            {
                deltaTime += Time.deltaTime;
                audioInfo.source.volume += audioInfo.volume * Time.deltaTime / duration;

                yield return null;
            }

            audioInfo.source.volume = audioInfo.volume;
        }

        private static IEnumerator CoFadeOut(AudioInfo audioInfo, float duration)
        {
            var deltaTime = 0f;
            while (deltaTime < duration)
            {
                deltaTime += Time.deltaTime;
                audioInfo.source.volume -= audioInfo.volume * Time.deltaTime / duration;

                yield return null;
            }

            audioInfo.source.Stop();
            audioInfo.source.volume = audioInfo.volume;
        }

        #endregion

        #region Shortcut

        public static void PlayClick()
        {
            instance.PlaySfx(SfxCode.Click);
        }

        public static void PlaySelect()
        {
            instance.PlaySfx(SfxCode.Select);
        }

        public static void PlayCancel()
        {
            instance.PlaySfx(SfxCode.Cancel);
        }

        public static void PlayPopup()
        {
            instance.PlaySfx(SfxCode.Popup);
        }

        public static void PlaySwing()
        {
            var random = Random.value;
            if (random < 0.33f)
            {
                instance.PlaySfx(SfxCode.Swing);
            }
            else if (random < 0.66f)
            {
                instance.PlaySfx(SfxCode.Swing2);
            }
            else
            {
                instance.PlaySfx(SfxCode.Swing3);
            }
        }

        public static void PlayFootStep()
        {
            var random = Random.value;
            instance.PlaySfx(random < 0.5f ? SfxCode.FootStepLow : SfxCode.FootStepHigh);
        }
        
        public static void PlayDamaged()
        {
            var random = Random.value;
            instance.PlaySfx(SfxCode.DamageNormal);
        }
        
        public static void PlayDamagedCritical()
        {
            var random = Random.value;
            instance.PlaySfx(random < 0.5f ? SfxCode.Critical01 : SfxCode.Critical02);
        }

        #endregion
    }
}
