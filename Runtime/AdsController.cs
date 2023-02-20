using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using AdsExtensions.Providers;

#if GAMEANALYTICS
using GameAnalyticsSDK;
#endif

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace AdsExtensions
{
    public enum AdType 
    {
        AppOpen,
        Banner,
        Interstitial,
        Rewarded,
        RewardedInterstitial,
        MRec
    }

    [Serializable]
    public struct Revenue
    {
        public string provider;
        public string adUnit;
        public string placement;
        public string countryCode;
        public string network;
        public double value;
        public string currencyCode;
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(AdsController))]
    [ExecuteInEditMode]
    public class AdsControllerEditor : Editor
    {
        AdsController.Provider tempProvider;

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            var script = (AdsController)target;

#if ADMOB
            EditorGUILayout.HelpBox("Set app ID's in GoogleMobileAdsSettings", MessageType.Warning);
#endif

#if APPLOVINMAX
            EditorGUILayout.HelpBox("Set adapters and additionals ID's in Integration manager window", MessageType.Warning);
#endif

            if (!Application.isPlaying && tempProvider != script.provider)
            {
                tempProvider = script.provider;

                script.ApplySdks();

                Debug.Log("Ad provider: " + tempProvider);
            }

            //if (GUILayout.Button("Apply SDK's"))
            //{
            //    script.ApplySdks();
            //}
        }
    }
#endif

            /// <summary>
            /// Implemented providers AdMob, IronSource, Clever Ads, Applovin MAX
            /// </summary>
        public class AdsController : MonoBehaviour
    {
        public Provider provider;

        public enum Provider 
        {
            None,
            AdMob,
            Ironsource,
            CleverAds,
            ApplovinMAX
        }

#if ADMOB
        [SerializeField]
        bool
            ironSource,
            adColony,
            maxSdk,
            tapjoy,
            vungle,
            unityAds,
            myTarget,
            dtExchange;
#endif

        public static AdsController Instance { get; private set; }

        [Header("ID's")]
#if ADMOB
        [Header("Set app ID's in GoogleMobileAdsSettings")]
        [SerializeField] Settings admob_android;
        [SerializeField] Settings admob_ios;
#endif

#if APPLOVINMAX
        [SerializeField] Settings applovin;
#endif

#if IRONSOURCE
        [SerializeField] Settings ironsource;
#endif

#if CLEVERADS
        [SerializeField] Settings applovin;
#endif

        AdProvider adProvider;

        public string AdvertisingId { get; private set; }

        [SerializeField] bool dontDestroyOnLoad;
        [SerializeField] bool initializeOnStart;
        [SerializeField] bool initializeOnSetConsent;

        [SerializeField] int appOpenAdDelay;
        [SerializeField] int interstitialDelay;
        [SerializeField] int rewardedDelay;
        [SerializeField] int interstitialAfterRewardedDelay;

        public Action OnGetAdvertissingId;
        public Action OnInitialized;
        public Action<AdType> OnError;
        public Action<AdType> OnLoaded;
        public Action<Placement> OnOpen;
        public Action<Placement> OnClose;
        public Action<Placement> OnRewarded;
        public Action<Placement> OnRewardedFailed;
        public Action<Placement, Revenue> OnPaid;

        [SerializeField] bool isTest;

        [SerializeField] Placement[] placements;
        public Placement[] Placements => placements;

        public bool skipInterstitial { get; private set; }

        bool setConsent;
        bool consentEnabled;

        static bool fetch;

        public DateTime LastAppOpenAdWatch { get; private set; }
        public DateTime LastInterstitialShow { get; private set; }
        public DateTime LastRewardedShow { get; private set; }

        string gameAnalyticsSDKName;

        Dictionary<AdType, Placement> currentPlacements;

        public bool RemovedAds { get; private set; }

        public List<AdType> enabledAd = new List<AdType>();
        public List<AdType> skipAd = new List<AdType>();

        bool earnedReward;

        public bool IsInitialized { get; private set; }

        [Tooltip("Enable additional GA data collection")]
        [Space(50)][SerializeField] bool gameAnalytics;

        List<string> defineSymbols = new List<string>();

        private void SetDefineSymbols(string key, bool add)
        {
            if (add)
            {
                if (!defineSymbols.Contains(key))
                    defineSymbols.Add(key);
            }
            else
            {
                defineSymbols.Remove(key);
            }
        }

        public void ApplySdks()
        {
            // Ad networks
            SetDefineSymbols("ADMOB", provider == Provider.AdMob);
            SetDefineSymbols("IRONSOURCE", provider == Provider.Ironsource);
            SetDefineSymbols("CLEVERADS", provider == Provider.CleverAds);
            SetDefineSymbols("APPLOVINMAX", provider == Provider.ApplovinMAX);

#if ADMOB
            SetDefineSymbols("ADMOB_IRONSOURCE", ironSource);
            SetDefineSymbols("ADMOB_ADCOLONY", adColony);
            SetDefineSymbols("ADMOB_APPLOVIN", maxSdk);
            SetDefineSymbols("ADMOB_TAPJOY", tapjoy);
            SetDefineSymbols("ADMOB_VUNGLE", vungle);
            SetDefineSymbols("ADMOB_UNITYADS", unityAds);
            SetDefineSymbols("ADMOB_MYRAGET", myTarget);
            SetDefineSymbols("ADMOB_DTEXCHANGE", dtExchange);
#endif

            // Other SDK's
            SetDefineSymbols("GAMEANALYTICS", gameAnalytics);

            string definesString = PlayerSettings.GetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup);
            List<string> allDefines = definesString.Split(';').ToList();
            allDefines.AddRange(defineSymbols.Except(allDefines));
            PlayerSettings.SetScriptingDefineSymbolsForGroup(
                EditorUserBuildSettings.selectedBuildTargetGroup,
                string.Join(";", allDefines.ToArray()));
        }

        public Placement GetCurrentPlacement(AdType type) 
        {
            return currentPlacements[type];
        }

        public Placement GetPlacement(string placement)
        {
            Placement value = null;

            value = placements.FirstOrDefault(x => x.placement.Equals(placement));

            if (value == null)
                Debug.LogWarning($"Placement '{placement}' not found!");

            return value;
        }

        public void SkipAd(params AdType[] types)
        {
            if (types == null) skipAd = new List<AdType>();
            else skipAd = types.ToList();
        }

        public void EnableAd(AdType adType, bool enable)
        {
            if (enable)
            {
                if (!enabledAd.Contains(adType))
                    enabledAd.Add(adType);
            }
            else 
            {
                enabledAd.Remove(adType);
            }
        }

        public bool IsReady(string placement)
        {
            var p = GetPlacement(placement);

            return p != null ? IsReady(p) : false;
        }

        public void SetInterstitialDelay(int delay)
        {
            interstitialDelay = delay;
        }

        public void SetRewardedDelay(int delay)
        {
            rewardedDelay = delay;
        }

        public void SetInterstitialAfterRewardedDelay(int delay)
        {
            interstitialAfterRewardedDelay = delay;
        }

        public void Request(string placement) 
        {
            var p = GetPlacement(placement);

            if (p != null)
                Request(p);
        }

        public void Request(string placement, float timeout, Action<bool> isReady = null)
        {
            var p = GetPlacement(placement);

            if (p != null)
            {
                if (requestAdCoroutine != null)
                    StopCoroutine(requestAdCoroutine);

                requestAdCoroutine = StartCoroutine(RequestCoroutine(p, timeout, result =>
                {
                    isReady?.Invoke(result);
                }));
            }
            else
            {
                isReady?.Invoke(false);
            }
        }

        public void Show(string placement, Action callback = null)
        {
            var p = GetPlacement(placement);

            if (p != null)
                Show(p, callback);
        }

        public void Show(string placement, Action<bool> success)
        {
            var p = GetPlacement(placement);

            if (p != null)
            {
                if (showAdCoroutine != null)
                    StopCoroutine(showAdCoroutine);

                showAdCoroutine = StartCoroutine(ShowCoroutine(p, result =>
                {
                    success?.Invoke(result);
                }));
            }
            else
            {
                success?.Invoke(false);
            }
        }

        public void Show(string placement, Action<bool, bool> successAndRewarded)
        {
            var p = GetPlacement(placement);

            if (p != null)
            {
                if (showAdCoroutine != null)
                    StopCoroutine(showAdCoroutine);

                showAdCoroutine = StartCoroutine(ShowCoroutine(p, result =>
                {
                    successAndRewarded?.Invoke(result, earnedReward);
                    earnedReward = false;
                }));
            }
            else
            {
                successAndRewarded?.Invoke(false, false);
            }
        }

        public bool IsAppOpenDelayed
        {
            get
            {
                return (DateTime.Now - LastAppOpenAdWatch).TotalSeconds < appOpenAdDelay;
            }
        }

        Placement waitForShowPlacement;
        bool waitForShowAd;
        bool waitForEarnReward;
        bool showAdResult;
        Coroutine showAdCoroutine;

        IEnumerator ShowCoroutine(Placement placement, Action<bool> success)
        {
            showAdResult = false;

            if (IsReady(placement))
            {
                waitForShowPlacement = placement;
                waitForShowAd = true;
                waitForEarnReward = true;

                Show(placement);

                yield return new WaitUntil(() => !waitForShowAd);

                if (placement.type == AdType.Rewarded)
                    yield return new WaitUntil(() => !waitForEarnReward);

                success?.Invoke(showAdResult);
                waitForShowPlacement = null;
            }
            else
            {
                success?.Invoke(showAdResult);
                waitForShowPlacement = null;
            }
        }

        Coroutine requestAdCoroutine;
        IEnumerator RequestCoroutine(Placement placement, float timeout, Action<bool> success)
        {
            Request(placement);

#if UNITY_EDITOR
            yield return new WaitForSeconds(1.0f);
#endif

            while (!IsReady(placement))
            {
                timeout -= Time.deltaTime;
                yield return null;

                if (timeout <= 0)
                    break;
            }

            success?.Invoke(IsReady(placement));
        }

        void OnApplicationPause(bool paused)
        {
            if(adProvider != null)
                adProvider.OnPause(paused);

#if GAMEANALYTICS
		if (paused)
		{
			if (currentPlacement != null)
			{
				GameAnalytics.PauseTimer(currentPlacement);
			}
		}
		else
		{
			if (currentPlacement != null)
			{
				GameAnalytics.ResumeTimer(currentPlacement);
			}
		}
#endif
        }

        private void Start()
        {
            //FirebaseManager.OnFetch += () =>
            //{
            //    fetch = true;
            //};

            if (dontDestroyOnLoad)
            {
                if (Instance != null && Instance != this)
                    Destroy(this);
                else
                {
                    Instance = this;
                    DontDestroyOnLoad(this);
                }
            }
            else
            {
                Instance = this;
            }

            if (initializeOnStart && !IsInitialized)
            {
                Initialize();
            }
        }

        public void ShowMediationDebuger()
        {
#if !ADMOB && APPLOVIN
           if (maxSdkProvider == null || !maxSdkProvider.IsInitialized)
           {
               Debug.LogWarning("MAX SDK not initialized yet!");
               return;
           }
           
           maxSdkProvider.ShowMediationDebugger();
#else
            Debug.Log($"Mediation debugger is only Applovin MAX SDK feature!");
#endif
        }

        public string GetAndroidAdvertiserId()
        {
            string advertisingID = "";
            try
            {
                AndroidJavaClass up = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
                AndroidJavaObject currentActivity = up.GetStatic<AndroidJavaObject>("currentActivity");
                AndroidJavaClass client = new AndroidJavaClass("com.google.android.gms.ads.identifier.AdvertisingIdClient");
                AndroidJavaObject adInfo = client.CallStatic<AndroidJavaObject>("getAdvertisingIdInfo", currentActivity);

                advertisingID = adInfo.Call<string>("getId").ToString();
            }
            catch (Exception e)
            {
                Debug.LogWarning($"OnAdGetAdvertisingId  error: {e}");
            }
            return advertisingID;
        }

        public void Initialize()
        {
#if UNITY_ANDROID
            AdvertisingId = GetAndroidAdvertiserId();
#else
        //  iOS and Windows Store
        Application.RequestAdvertisingIdentifierAsync((string id, bool trackingEnabled, string error) => 
        {
            if (string.IsNullOrEmpty(error))
            {
                Debug.Log($"OnAdGetAdvertisingId id: {id} trackingEnabled: {trackingEnabled}");

                AdvertisingId = id;
                OnGetAdvertissingId?.Invoke();
            }
            else
            {
                Debug.LogWarning($"OnAdGetAdvertisingId  error: {error}");
            }
        });
#endif

            StartCoroutine(InitializeCoroutine());
        }

        IEnumerator InitializeCoroutine()
        {
            if (!initializeOnSetConsent)
                SetConsent(false);

            yield return new WaitUntil(() => setConsent);

            //Debug.Log($"Advertisement consent: {consentEnabled}");
            //
            //yield return new WaitUntil(() => FirebaseManager.IsFetchedRemoteConfig);
            //
            //var useConsent = FirebaseManager.GetRemoteConfigBoolean("consent");
            //
            //if (!useConsent)
            //    consentEnabled = false;

            Settings settings = null;

#if ADMOB

#if UNITY_EDITOR || UNITY_ANDROID
            settings = admob_android;
#elif UNITY_IOS
            settings = admob_ios;
#endif

            adProvider = new AdMobProvider();
#endif

#if APPLOVINMAX
            settings = applovin;

            adProvider = new MaxSdkProvider();
#endif

#if IRONSOURCE
            adProvider = new IronSourceProvider();
#endif

#if CLEVERADS
            adProvider = new CleverAdsProvider();
#endif

            adProvider.OnError += (adType, adUnit, error) => { OnAdError(adType, adUnit, error); };
            adProvider.OnLoad += (adType, adUnit) => { OnAdLoaded(adType, adUnit); };
            adProvider.OnShow += (adType, adUnit) => { OnAdOpen(GetCurrentPlacement(adType)); };
            adProvider.OnClose += (adType, adUnit) => { OnAdClose(GetCurrentPlacement(adType)); };
            adProvider.OnEarnReward += (adType, adUnit) => { OnAdReward(GetCurrentPlacement(adType)); };
            adProvider.OnPaid += (adType, adUnit, revenue) => { OnAdPaid(GetCurrentPlacement(adType), revenue); };

            adProvider.OnInitialize += () =>
            {
                IsInitialized = true;

                RequestAll();
            };

#if ADMOB
            adProvider.Initialize(isTest, consentEnabled, null, settings.AppOpenId, settings.BannerId, settings.InterstitialId, settings.RewardedId, null, null);
#endif

#if APPLOVINMAX
            adProvider.Initialize(isTest, consentEnabled, settings.ApiKey, settings.AppOpenId, settings.BannerId, settings.InterstitialId, settings.RewardedId, settings.RewardedInterstitialId, settings.MrecId);
#endif
            currentPlacements = new Dictionary<AdType, Placement>();

            foreach (var ad in adProvider.InitializedAdTypes)
                currentPlacements.Add(ad, null);
        }

        public void RemoveAds(bool enable)
        {
            RemovedAds = enable;

            HideAll();
            DestroyAll();

            if (enable)
                RequestAll();

#if CleverAds
            var cleverads = (CleverAdsProvider)adProvider;
            cleverads.SetAppReturnAdsEnabled(enable);
#endif

            Debug.Log("NOADS Remove ads: " + enable);
        }

        public void SetConsent(bool consent)
        {
            setConsent = true;
            consentEnabled = consent;
        }

        public void SkipInterstitial(bool skip)
        {
            skipInterstitial = skip;
        }

        public void RequestAll()
        {
            if (!IsInitialized)
                return;

            foreach (var placement in placements)
                Request(placement);
        }

        public void HideAll()
        {
            foreach (var placement in placements)
                Hide(placement);
        }

        public void DestroyAll()
        {
            foreach (var placement in placements)
                Destroy(placement);
        }

        public bool Skip(Placement placement) 
        {
            bool skip = skipAd.Any(x => x == placement.type);

            if (skip)
                Debug.Log($"OnAdSkiped: {placement}, Skip ad types: {string.Join(',', skipAd)}");

            return skip;
        }

        public bool IsEnabled(AdType adType) 
        {
            if (enabledAd.Contains(adType))
            {
                return true;
            }
            else
            {
                Debug.Log($"{adType} is disabled!");
                return false;
            }
        }

        public bool IsDelayed(Placement placement)
        {
            switch (placement.type)
            {
                case AdType.Interstitial:
                    if ((DateTime.Now - LastInterstitialShow).TotalSeconds < interstitialDelay)
                    {
                        Debug.Log($"OnAdDelayed: {placement.type} Last interstitial displayed {(DateTime.Now - LastInterstitialShow).TotalSeconds}sec ago. Timeout: {interstitialDelay}sec");
                        return true;
                    }

                    if ((DateTime.Now - LastRewardedShow).TotalSeconds < interstitialAfterRewardedDelay)
                    {
                        Debug.Log($"OnAdDelayed: {placement.type} Last rewarded displayed {(DateTime.Now - LastRewardedShow).TotalSeconds}sec ago. Timeout: {interstitialAfterRewardedDelay}sec");
                        return true;
                    }

                    break;
                case AdType.Rewarded:
                    if ((DateTime.Now - LastRewardedShow).TotalSeconds < rewardedDelay)
                    {
                        Debug.Log($"OnAdDelayed: {placement.type} Last rewarded displayed {(DateTime.Now - LastRewardedShow).TotalSeconds}sec ago. Timeout: {rewardedDelay}sec");
                        return true;
                    }
                    break;
            }

            return false;
        }

        public bool IsReady(Placement placement)
        {
            if (!IsEnabled(placement.type))
                return false;

            if (Skip(placement))
                return false;

            if (IsDelayed(placement))
                return false;

            return adProvider.IsReady(placement.type);
        }

        public void Request(Placement placement)
        {
            if (IsReady(placement))
                return;

            if (RemovedAds && placement.type != AdType.Rewarded)
                return;

            adProvider.Request(placement.type);
        }

        public void Show(Placement placement, Action callback = null)
        {
            if (RemovedAds && placement.type != AdType.Rewarded)
                return;

            if (!IsReady(placement))
                return;

            currentPlacements[placement.type] = placement;

            earnedReward = false;

            adProvider.Show(placement.type);

            callback?.Invoke();
        }

        public void Hide(Placement placement)
        {
            adProvider.Hide(placement.type);
        }

        public void Destroy(Placement placement)
        {
            adProvider.Destroy(placement.type);
        }


#region Callbacks
        private void OnAdPaid(Placement placement, Revenue revenue)
        {
            OnPaid?.Invoke(placement, revenue);
            Debug.Log($"OnAdPaid: {placement} AdUnit:{revenue.adUnit} Value: {revenue.value} Currency: {revenue.currencyCode}");
        }

        private void OnAdError(AdType adType, string adUnit, string errorMessage)
        {
            showAdResult = false;

            if (waitForShowAd && waitForShowPlacement.type.Equals(adType))
                waitForShowAd = false;

            waitForEarnReward = false;

            Debug.LogWarning($"OnAdError: {adType} {adUnit} {errorMessage}");
            OnError?.Invoke(adType);

#if GAMEANALYTICS
		GameAnalytics.NewAdEvent(GAAdAction.FailedShow, GAAdType.RewardedVideo, gameAnalyticsSDKName, currentPlacement);
#endif
        }

        private void OnAdLoaded(AdType adType, string adUnit)
        {
            Debug.Log($"OnAdLoaded: {adType} {adUnit}");
            OnLoaded?.Invoke(adType);
        }

        private void OnAdOpen(Placement placement)
        {
            showAdResult = true;

            switch (placement.type)
            {
                case AdType.Interstitial:
                    LastInterstitialShow = DateTime.Now;
                    break;
                case AdType.Rewarded:
                    LastRewardedShow = DateTime.Now;
                    break;
            }

            Debug.Log($"OnAdOpen: {placement.placement}");
            OnOpen?.Invoke(placement);

#if GAMEANALYTICS
		GameAnalytics.NewAdEvent(GAAdAction.Show, GAAdType, gameAnalyticsSDKName, currentPlacement);
#endif
        }

        private void OnAdClicked(Placement placement)
        {
            Debug.Log($"OnAdClicked: {placement.placement}");

#if GAMEANALYTICS
		GameAnalytics.NewAdEvent(GAAdAction.Clicked, GAAdType, gameAnalyticsSDKName, currentPlacement);
#endif
        }

        private void OnAdClose(Placement placement)
        {
            Debug.Log($"OnAdClose: {placement.placement}");
            OnClose?.Invoke(placement);

            placement.lastShow = DateTime.Now;

            if (placement.type == AdType.Rewarded)
            {
                StopCoroutine("GetReward");
                StartCoroutine("GetReward", placement);
            }

            if (waitForShowAd && waitForShowPlacement.Equals(placement))
                waitForShowAd = false;

            RequestAll();

#if GAMEANALYTICS
		long elapsedTime = GameAnalytics.StopTimer(currentPlacement);

		GameAnalytics.NewAdEvent(GAAdAction.Show, GAAdType.RewardedVideo, gameAnalyticsSDKName, currentPlacement, elapsedTime);
#endif
        }

        IEnumerator GetReward(Placement placement)
        {
            float timeout = 2.0f;

            while (timeout > 0 && !earnedReward)
            {
                timeout -= Time.deltaTime;
                yield return null;
            }

            if (earnedReward) OnRewarded?.Invoke(placement);
            else OnRewardedFailed?.Invoke(placement);

            waitForEarnReward = false;
        }

        private void OnAdReward(Placement placement)
        {
            Debug.Log($"OnAdReward: {placement.placement}");
            earnedReward = true;

#if GAMEANALYTICS
		    GameAnalytics.NewAdEvent(GAAdAction.RewardReceived, GAAdType.RewardedVideo, gameAnalyticsSDKName, currentPlacement);
#endif
        }
#endregion
    }

    [Serializable]
    public class Settings 
    {
#if !ADMOB
        [SerializeField] string apiKey;
        public string ApiKey => apiKey;
#endif

        [SerializeField] string appOpenId;
        public string AppOpenId => appOpenId;

        [SerializeField] string interstitialId;
        public string InterstitialId => interstitialId;

        [SerializeField] string rewardedId;
        public string RewardedId => rewardedId;

#if !ADMOB
        [SerializeField] string rewardedInterstitialId;
        public string RewardedInterstitialId => rewardedInterstitialId;
#endif

        [SerializeField] string bannerId;
        public string BannerId => bannerId;

#if !ADMOB
        [SerializeField] string mrecId;
        public string MrecId => mrecId;
#endif

        [SerializeField] bool adapterDebug;
        public bool AdapterDebug => adapterDebug;
    }
}
