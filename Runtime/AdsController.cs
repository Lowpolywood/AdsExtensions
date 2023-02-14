using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

#if ADMOB
using GoogleMobileAds.Common;
using GoogleMobileAds.Api;
using GoogleMobileAds.Api.Mediation;
#endif

#if ADMOB && ADCOLONY
using GoogleMobileAds.Api.Mediation.AdColony;
#endif

#if ADMOB && APPLOVIN
using GoogleMobileAds.Api.Mediation.AppLovin;
#endif

#if ADMOB && TAPJOY
using GoogleMobileAds.Api.Mediation.Tapjoy;
#endif

#if ADMOB && VUNGLE
using GoogleMobileAds.Api.Mediation.Vungle;
#endif

#if ADMOB && UNITYADS
using GoogleMobileAds.Api.Mediation.UnityAds;
#endif

#if ADMOB && MYTARGET
using GoogleMobileAds.Api.Mediation.MyTarget;
#endif

#if ADMOB && DTEXCHANGE
using GoogleMobileAds.Api.Mediation.DTExchange;
#endif

#if ADMOB && IRONSOURCE
using GoogleMobileAds.Api.Mediation.IronSource;
#endif

#if CLEVERADS
using CAS;
#endif

#if GAMEANALYTICS
using GameAnalyticsSDK;
#endif

// Facebook renamed to Meta
// Fyber renamed to DTExchange
// Vungle renamed to Liftoff
#if UNITY_EDITOR
using UnityEditor;

[CustomEditor(typeof(AdsController))][ExecuteInEditMode]
public class AdsControllerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        var script = (AdsController)target;

        if (GUILayout.Button("Apply SDK's")) 
        {
            script.ApplySdks();
        }
    }
}
#endif

/// <summary>
/// Implemented providers AdMob, IronSource, Clever Ads, Applovin MAX
/// </summary>
public class AdsController : MonoBehaviour
{
    private static AdsController instance;

    public static string AdvertisingId { get; private set; }

#if !ADMOB && APPLOVIN
    [Header("MAX SDK")]
    [SerializeField] string maxSdkKey = "ENTER_MAX_SDK_KEY_HERE";
    [SerializeField] string maxSdkInterstitialId = "ENTER_INTERSTITIAL_AD_UNIT_ID_HERE";
    [SerializeField] string maxSdkRewardedId = "ENTER_REWARD_AD_UNIT_ID_HERE";
    [SerializeField] string maxSdkRewardedInterstitialId = "ENTER_REWARD_INTER_AD_UNIT_ID_HERE";
    [SerializeField] string maxSdkBannerId = "ENTER_BANNER_AD_UNIT_ID_HERE";
    [SerializeField] string maxSdkMRecId = "ENTER_MREC_AD_UNIT_ID_HERE";

    MaxSdkProvider maxSdkProvider;
#endif

    [SerializeField] bool dontDestroyOnLoad;
    [SerializeField] bool initializeOnStart;
    [SerializeField] bool initializeOnSetConsent;

    [SerializeField] int appOpenAdDelay;
    [SerializeField] int interstitialDelay;
    [SerializeField] int rewardedDelay;
    [SerializeField] int interstitialAfterRewardedDelay;

    public static Action OnGetAdvertissingId;
    public static Action OnInitialized;
    public static Action<Placement> OnError;
    public static Action<Placement> OnLoaded;
    public static Action<Placement> OnOpen;
    public static Action<Placement> OnClose;
    public static Action<Placement> OnRewarded;
    public static Action<Placement> OnRewardedFailed;

    [SerializeField] bool isTest;
    [SerializeField] bool showAppOpenAd;
    [SerializeField] bool adapterDebug;

    [SerializeField] string androidAppKey;
    [SerializeField] string iosAppKey;
    [SerializeField] string appOpenAndroidId;
    [SerializeField] string appOpenIOS;

    [SerializeField] Placement[] placements;
    public Placement[] Placements => placements;

    public static bool skipInterstitial { get; private set; }

    static bool setConsent;
    static bool consentEnabled;

    static bool fetch;

    public static DateTime LastAppOpenAdWatch { get; private set; }
    public static DateTime LastInterstitialShow { get; private set; }
    public static DateTime LastRewardedShow { get; private set; }

    string gameAnalyticsSDKName;

    Placement currentPlacement;

    public static bool RemovedAds { get; private set; }

    public List<Placement.Type> skipAd = new List<Placement.Type>();

    [SerializeField] int gameSession;
    public int GameSession
    {
        get => gameSession;
        private set
        {
            if (gameSession != value)
            {
                PlayerPrefs.SetInt("game_session_count", value);
                PlayerPrefs.Save();
            }

            gameSession = value;
        }
    }

#if ADMOB
    [SerializeField] bool testRewardedSuccess = true;

    static AppOpenAd appOpenAd;
    static string appOpenAdId;
    static bool isShowingAppOpenAd;
    static DateTime appOpenAdLoadTime;

    public static bool IsAppOpenAdReady
    {
        get
        {
            return appOpenAd != null;
            //return appOpenAd != null && (System.DateTime.UtcNow - appOpenAdLoadTime).TotalHours < 4;
        }
    }
#endif

#if ADMOB && VUNGLE
    VungleInterstitialMediationExtras vungleInterstitialExtras;
    VungleRewardedVideoMediationExtras vungleRewaredVideoExtras;
#endif

#if CLEVERADS
    public ConsentStatus userConsent;
    public CCPAStatus userCCPAStatus;
    public IMediationManager manager;

    private bool isAppReturnEnable = false;
#endif

    public static bool IsInitialized { get; private set; }

    [SerializeField] bool admob, ironSource, cleverAds, adColony, maxSdk, tapjoy, vungle, unityAds, myTarget, dtExchange;

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
        SetDefineSymbols("ADMOB", admob);
        SetDefineSymbols("IRONSOURCE", ironSource);
        SetDefineSymbols("CLEVERADS", cleverAds);
        SetDefineSymbols("ADCOLONY", adColony);
        SetDefineSymbols("APPLOVIN", maxSdk);
        SetDefineSymbols("TAPJOY", tapjoy);
        SetDefineSymbols("VUNGLE", vungle);
        SetDefineSymbols("UNITYADS", unityAds);
        SetDefineSymbols("MYRAGET", myTarget);
        SetDefineSymbols("DTEXCHANGE", dtExchange);

        string definesString = PlayerSettings.GetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup);
        List<string> allDefines = definesString.Split(';').ToList();
        allDefines.AddRange(defineSymbols.Except(allDefines));
        PlayerSettings.SetScriptingDefineSymbolsForGroup(
            EditorUserBuildSettings.selectedBuildTargetGroup,
            string.Join(";", allDefines.ToArray()));
    }

    public static Placement GetPlacement(string placement)
    {
        Placement value = null;

        value = instance.placements.FirstOrDefault(x => x.placement.Equals(placement));

        if (value == null)
            Debug.LogWarning($"Placement '{placement}' not found!");

        return value;
    }

    public static void EnableAppOpenAd(bool enable)
    {
        instance.showAppOpenAd = enable;
    }

    public static void SkipAd(params Placement.Type[] types)
    {
        if (types == null) instance.skipAd = new List<Placement.Type>();
        else instance.skipAd = types.ToList();
    }

    public static bool IsReady(string placement)
    {
        var p = GetPlacement(placement);

        return p != null ? p.IsReady() : false;
    }

    public static void SetInterstitialDelay(int delay)
    {
        instance.interstitialDelay = delay;
    }

    public static void SetRewardedDelay(int delay)
    {
        instance.rewardedDelay = delay;
    }

    public static void SetInterstitialAfterRewardedDelay(int delay)
    {
        instance.interstitialAfterRewardedDelay = delay;
    }

    public static void Request(string placement, float timeout, Action<bool> isReady = null)
    {
        var p = GetPlacement(placement);

        if (p != null)
        {
            if (instance.requestAdCoroutine != null)
                instance.StopCoroutine(instance.requestAdCoroutine);

            instance.requestAdCoroutine = instance.StartCoroutine(instance.RequestCoroutine(p, timeout, result =>
            {
                isReady?.Invoke(result);
            }));
        }
        else
        {
            isReady?.Invoke(false);
        }
    }

    public static void Show(string placement, Action callback = null)
    {
        GetPlacement(placement)?.Show(callback);
    }

    public static void Show(string placement, Action<bool> success)
    {
        var p = GetPlacement(placement);

        if (p != null)
        {
            if (instance.showAdCoroutine != null)
                instance.StopCoroutine(instance.showAdCoroutine);

            instance.showAdCoroutine = instance.StartCoroutine(instance.ShowCoroutine(p, result =>
            {
                success?.Invoke(result);
            }));
        }
        else
        {
            success?.Invoke(false);
        }
    }

    public static void Show(string placement, Action<bool, bool> successAndRewarded)
    {
        var p = GetPlacement(placement);

        if (p != null)
        {
            if (instance.showAdCoroutine != null)
                instance.StopCoroutine(instance.showAdCoroutine);

            instance.showAdCoroutine = instance.StartCoroutine(instance.ShowCoroutine(p, result =>
            {
                successAndRewarded?.Invoke(result, p.earnedReward);
                p.earnedReward = false;
            }));
        }
        else
        {
            successAndRewarded?.Invoke(false, false);
        }
    }

    public static bool IsAppOpenDelayed
    {
        get
        {
            return (DateTime.Now - LastAppOpenAdWatch).TotalSeconds < instance.appOpenAdDelay;
        }
    }

    public static void ShowAppOpenAd()
    {
        if (instance.gameSession <= 1)
            return;

        Debug.Log("ONAD APPOPEN 1");

        if (!instance.showAppOpenAd)
        {
            Debug.Log("App open ad disabled!");
            return;
        }

#if ADMOB
        if (!IsAppOpenAdReady || isShowingAppOpenAd)
        {
            return;
        }

        if (RemovedAds)
            return;

        appOpenAd.OnAdFullScreenContentClosed += () =>
        {
            Debug.Log("Closed app open ad");

            appOpenAd = null;
            isShowingAppOpenAd = false;
            LoadAppOpenAd();
        };

        appOpenAd.OnAdFullScreenContentFailed += (error) =>
        {
            Debug.LogWarning($"Failed to present app open ad! {error.GetMessage()}");

            appOpenAd = null;
            LoadAppOpenAd();
        };

        appOpenAd.OnAdFullScreenContentOpened += () =>
        {
            Debug.Log("Displayed app open ad");

            LastAppOpenAdWatch = DateTime.Now;

            isShowingAppOpenAd = true;
        };

        appOpenAd.OnAdImpressionRecorded += () =>
        {
            Debug.Log("App open ad impression");
        };

        appOpenAd.OnAdPaid += (adValue) =>
        {
            instance.ReportRevenue(appOpenAdId, adValue.Value / 1000000f, adValue.CurrencyCode);
        };

        appOpenAd.Show();

        Debug.Log("ONAD APPOPEN 2");
#endif
    }

    public static void LoadAppOpenAd()
    {
        string appOpenAdId = null;

        if (RemovedAds)
            return;

#if UNITY_ANDROID
        appOpenAdId = instance.isTest ? "ca-app-pub-3940256099942544/3419835294" : instance.appOpenAndroidId;
#endif

#if UNITY_IOS
        appOpenAdId = instance.isTest ? "ca-app-pub-3940256099942544/5662855259" : instance.appOpenIOS;
#endif

#if ADMOB
        if (string.IsNullOrEmpty(appOpenAdId))
        {
            Debug.LogWarning("App open ad id is empty!");
        }
        else
        {
            if (IsAppOpenAdReady)
                return;

            AdRequest request = new AdRequest.Builder().Build();

            AppOpenAd.Load(appOpenAdId, ScreenOrientation.Portrait, request, ((ad, error) =>
            {
                if (error != null)
                {
                    // Handle the error.
                    Debug.LogWarning($"Failed to load app open ad! {error.GetMessage()}");
                    return;
                }

                Debug.Log($"App open ad is ready!");

                // App open ad is loaded.
                appOpenAd = ad;
                appOpenAdLoadTime = DateTime.UtcNow;
            }));
        }
#endif

#if IRONSOURCE
        Debug.Log("IS has no implementation for app open ad!");
#endif
    }

    bool waitForShowAd;
    bool waitForEarnReward;
    bool showAdResult;
    Coroutine showAdCoroutine;

    IEnumerator ShowCoroutine(Placement placement, Action<bool> success)
    {
        showAdResult = false;

        if (placement.IsReady())
        {
            waitForShowAd = true;
            waitForEarnReward = true;

            placement.Show();
            yield return new WaitUntil(() => !waitForShowAd);

            if (placement.type == Placement.Type.Rewarded)
                yield return new WaitUntil(() => !waitForEarnReward);

            success?.Invoke(showAdResult);
        }
        else
        {
            placement.Request();
            success?.Invoke(showAdResult);

            yield return null;
        }
    }

    Coroutine requestAdCoroutine;
    IEnumerator RequestCoroutine(Placement placement, float timeout, Action<bool> success)
    {
        placement.Request();

#if UNITY_EDITOR
        yield return new WaitForSeconds(1.0f);
#endif

        while (!placement.IsReady() && timeout > 0)
        {
            timeout -= Time.deltaTime;
            yield return null;
        }

        success?.Invoke(placement.IsReady());
    }

    void OnApplicationPause(bool paused)
    {
#if !ADMOB && IRONSOURCE
        IronSource.Agent.onApplicationPause(paused);
#endif

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
            if (instance != null && instance != this)
                Destroy(this);
            else
            {
                instance = this;
                DontDestroyOnLoad(this);
            }
        }
        else
        {
            instance = this;
        }

        LoadAppOpenAd();

#if ADMOB
        AppStateEventNotifier.AppStateChanged += (state) =>
        {
            if (state == AppState.Foreground)
                ShowAppOpenAd();
        };
#endif

        if (initializeOnStart && !IsInitialized)
        {
            Initialize();
        }
    }

    public static void Initialize()
    {
        instance.StartCoroutine(instance.InitializeCoroutine());
    }

    IEnumerator InitializeCoroutine()
    {
        instance.gameSession = PlayerPrefs.GetInt("game_session_count");
        instance.GameSession++;

        if (!initializeOnSetConsent)
            SetConsent(false);

        yield return new WaitUntil(() => setConsent);

        Debug.Log($"Advertisement consent: {consentEnabled}");

        //yield return new WaitUntil(() => FirebaseManager.IsFetchedRemoteConfig);
        //
        //var useConsent = FirebaseManager.GetRemoteConfigBoolean("consent");
        //
        //if (!useConsent)
        //    consentEnabled = false;

#if ADMOB

        // AdMob mediation adapters
#if ADCOLONY
        AdColonyAppOptions.SetGDPRRequired(true);
        AdColonyAppOptions.SetGDPRConsentString(consentEnabled ? "1" : "0");
#endif

#if APPLOVIN
        AppLovin.SetHasUserConsent(consentEnabled);
        //AppLovin.SetIsAgeRestrictedUser(true); if user age defined
        AppLovin.SetDoNotSell(true); // Do not sell CCPA
#endif

#if TAPJOY
        // Y = YES, N = No, – = Not Applicable 
        //For users where CCPA doesn't apply, the string's value will always be "1---".
        Tapjoy.SetUSPrivacy("1---");
#endif

#if VUNGLE
        // Send placements to Vungle SDK
        var interstitials = new List<string>();
        var rewardedVideos = new List<string>();

        foreach (var p in placements)
        {
            if (p.type == Placement.Type.Interstitial)
                interstitials.Add(p.Id);
            else if (p.type == Placement.Type.Rewarded)
                rewardedVideos.Add(p.Id);

            yield return new WaitForEndOfFrame();
        }

        // Required for Vungle mediation < 3.1.0
        //vungleInterstitialExtras = new VungleInterstitialMediationExtras();
        //vungleRewaredVideoExtras = new VungleRewardedVideoMediationExtras();
        //
        //vungleInterstitialExtras.SetAllPlacements(interstitials.ToArray());
        //vungleRewaredVideoExtras.SetAllPlacements(rewardedVideos.ToArray());

        Vungle.UpdateConsentStatus(consentEnabled ? VungleConsent.ACCEPTED : VungleConsent.DENIED);
#endif

#if UNITYADS
        UnityAds.SetConsentMetaData("gdpr.consent", consentEnabled);
        UnityAds.SetConsentMetaData("privacy.consent", false); // Do not sell CCPA
#endif

#if MYTARGET
        MyTarget.SetUserConsent(consentEnabled);
        MyTarget.SetUserAgeRestricted(false);
        MyTarget.SetCCPAUserConsent(false); // Do not sell CCPA
#endif

#if DTEXCHANGE
        DTExchange.SetGDPRConsent(consentEnabled);
        DTExchange.SetGDPRConsentString("myGDPRConsentString");
#endif

#if IRONSOURCE
        IronSource.SetConsent(consentEnabled);
        IronSource.SetMetaData("do_not_sell", "true");
#endif

        gameAnalyticsSDKName = "admob";

        RequestConfiguration requestConfiguration =
            new RequestConfiguration.Builder()
            .SetSameAppKeyEnabled(true).build();
        MobileAds.SetRequestConfiguration(requestConfiguration);

        MobileAds.SetiOSAppPauseOnBackground(true);

        MobileAds.Initialize(initStatus =>
        {
            IsInitialized = true;

            Dictionary<string, AdapterStatus> map = initStatus.getAdapterStatusMap();
            foreach (KeyValuePair<string, AdapterStatus> keyValuePair in map)
            {
                string className = keyValuePair.Key;
                AdapterStatus status = keyValuePair.Value;
                switch (status.InitializationState)
                {
                    case AdapterState.NotReady:
                        // The adapter initialization did not complete.
                        Debug.Log("Adapter: " + className + " not ready.");
                        break;
                    case AdapterState.Ready:
                        // The adapter was successfully initialized.
                        Debug.Log("Adapter: " + className + " is initialized.");
                        break;
                }
            }

            OnInitialized?.Invoke();

            RequestAll();

            ShowAppOpenAd();
        });
#endif

#if !ADMOB && IRONSOURCE
        gameAnalyticsSDKName = "ironsource";

#if UNITY_ANDROID
        string appKey = androidAppKey;
#elif UNITY_IPHONE
        string appKey = instance.iosAppKey;
#else
		string appKey = "unexpected_platform";
#endif
        lastAdWatch = DateTime.Now.AddSeconds(-interstitialDelay);
        lastRewardedAdWatch = DateTime.Now.AddSeconds(-rewardedDelay);

        //Dynamic config example
        IronSourceConfig.Instance.setClientSideCallbacks(true);

        IronSource.Agent.setAdaptersDebug(adapterDebug);
        IronSource.Agent.setConsent(consentEnabled);

        string id = IronSource.Agent.getAdvertiserId();
        Debug.Log("IS Advertiser Id : " + id);

        Debug.Log("IS Validate integration...");
        IronSource.Agent.validateIntegration();
        Debug.Log(IronSource.unityVersion());

        // App tracking transparrency
        IronSourceEvents.onConsentViewDidAcceptEvent += (type) => { Debug.Log($"ConsentViewDidShowSuccessEvent {type}"); };
        IronSourceEvents.onConsentViewDidLoadSuccessEvent += (type) => { IronSource.Agent.showConsentViewWithType("pre"); };
        IronSourceEvents.onConsentViewDidShowSuccessEvent += (type) => { PlayerPrefs.SetInt("iosAppTrackingTransparrencyAccepted", 1); PlayerPrefs.Save(); };

        // Errors
        IronSourceEvents.onConsentViewDidFailToLoadWithErrorEvent += (type, error) => { Debug.LogWarning($"ConsentViewDidFailToLoadWithErrorEvent {error.getCode()} | {error.getDescription()}"); };
        IronSourceEvents.onConsentViewDidFailToShowWithErrorEvent += (type, error) => { Debug.LogWarning($"ConsentViewDidFailToShowWithErrorEvent {error.getCode()} | {error.getDescription()}"); };

        IronSourceEvents.onBannerAdLoadFailedEvent += (error) => { OnAdError(currentPlacement, $"{error.getCode()} | {error.getDescription()}"); };
        IronSourceEvents.onInterstitialAdLoadFailedEvent += (error) => { OnAdError(currentPlacement, $"InterstitialAdLoadFailedEvent {error.getCode()} | {error.getDescription()}"); };
        IronSourceEvents.onInterstitialAdShowFailedEvent += (error) => { OnAdError(currentPlacement, $"InterstitialAdShowFailedEvent {error.getCode()} | {error.getDescription()}"); };
        IronSourceEvents.onRewardedVideoAdShowFailedEvent += (error) => { OnAdError(currentPlacement, $"RewardedVideoAdShowFailedEvent {error.getCode()} | {error.getDescription()}"); };

        // Add Banner Events
        IronSourceEvents.onBannerAdLoadedEvent += () => { Debug.Log($"OnAdLoaded: Banner"); };
        IronSourceEvents.onBannerAdClickedEvent += () => { Debug.Log($"OnAdClicked: Banner"); };
        IronSourceEvents.onBannerAdScreenPresentedEvent += () => { Debug.Log($"OnAdOpen: Banner"); };
        IronSourceEvents.onBannerAdScreenDismissedEvent += () => { Debug.Log($"OnAdClose: Banner"); };
        IronSourceEvents.onBannerAdLeftApplicationEvent += () => { Debug.Log("BannerAdLeftApplicationEvent"); };

        // Add Interstitial Events
        IronSourceEvents.onInterstitialAdReadyEvent += () => { Debug.Log($"OnAdLoaded: Interstitial"); };
        IronSourceEvents.onInterstitialAdShowSucceededEvent += () => { };
        IronSourceEvents.onInterstitialAdClickedEvent += () => { OnAdClicked(currentPlacement); };
        IronSourceEvents.onInterstitialAdOpenedEvent += () => { OnAdOpen(currentPlacement); };
        IronSourceEvents.onInterstitialAdClosedEvent += () => { OnAdClose(currentPlacement); };

        //Add Rewarded Video Events
        IronSourceEvents.onRewardedVideoAdOpenedEvent += () => { OnAdOpen(currentPlacement); };
        IronSourceEvents.onRewardedVideoAdClosedEvent += () => { OnAdClose(currentPlacement); };
        IronSourceEvents.onRewardedVideoAdStartedEvent += () => { };
        IronSourceEvents.onRewardedVideoAdEndedEvent += () => { };
        IronSourceEvents.onRewardedVideoAdRewardedEvent += (placement) => { OnAdReward(currentPlacement); };
        IronSourceEvents.onRewardedVideoAdClickedEvent += (placement) => { OnAdClicked(currentPlacement); };

        // Revenue
        IronSourceEvents.onImpressionSuccessEvent += (impression) => 
        {
            if (impression != null)
            {
                Debug.Log($"{impression} {impression.adNetwork} {impression.adUnit} {impression.instanceId} {impression.instanceName} {impression.placement} {impression.revenue}");

                var parameters = new Dictionary<string, object>();
                parameters.Add("ad_platform", "ironSource");
                parameters.Add("ad_source", impression.adNetwork);
                parameters.Add("ad_unit_name", impression.placement);
                parameters.Add("ad_format", impression.instanceName);
                parameters.Add("currency", "USD");
                parameters.Add("value", impression.revenue);

                FirebaseManager.ReportEvent("ad_impression", parameters);

                var value = (decimal)impression.revenue;

                ReportRevenue(impression.placement, value, "USD");
            }
        };

        //IronSource.Agent.init(appKey);
        IronSource.Agent.init(appKey, IronSourceAdUnits.REWARDED_VIDEO, IronSourceAdUnits.INTERSTITIAL, IronSourceAdUnits.BANNER);
        //IronSource.Agent.initISDemandOnly (appKey, IronSourceAdUnits.REWARDED_VIDEO, IronSourceAdUnits.INTERSTITIAL);

        // Set User ID For Server To Server Integration
        //IronSource.Agent.setUserId ("UserId");

#if UNITY_ANDROID && !UNITY_EDITOR
				AndroidJavaClass up = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
				AndroidJavaObject currentActivity = up.GetStatic<AndroidJavaObject>("currentActivity");
				AndroidJavaClass client = new AndroidJavaClass("com.google.android.gms.ads.identifier.AdvertisingIdClient");
				AndroidJavaObject adInfo = client.CallStatic<AndroidJavaObject>("getAdvertisingIdInfo", currentActivity);
		
				//advertisingIdClient.text = adInfo.Call<string>("getId").ToString();
				Debug.Log($"IRONSOURCE Android advertising ID: {adInfo.Call<string>("getId").ToString()}");
#endif

#if UNITY_IOS && !UNITY_EDITOR
				Application.RequestAdvertisingIdentifierAsync((string advertisingId, bool trackingEnabled, string error) =>
				{
					//advertisingIdClient.text = advertisingId;
					Debug.Log($"IRONSOURCE iOS advertising ID: {advertisingId}");
				});
#endif

        if (PlayerPrefs.GetInt("iosAppTrackingTransparrencyAccepted") <= 0)
            IronSource.Agent.loadConsentViewWithType("pre");

        IsInitialized = true;
        OnInitialized?.Invoke();
#endif

#if CLEVERADS
        MobileAds.settings.userConsent = consentEnabled ? ConsentStatus.Accepted : ConsentStatus.Denied;
        //MobileAds.settings.userCCPAStatus = userCCPAStatus;
        MobileAds.settings.isExecuteEventsOnUnityThread = true;
        MobileAds.settings.analyticsCollectionEnabled = true;

        manager = MobileAds.BuildManager().Initialize();

        manager.SetAppReturnAdsEnabled(showAppOpenAd);

        // Errors
        manager.OnFailedToLoadAd += (adType, error) => { OnAdError(currentPlacement, error); };
        manager.OnInterstitialAdFailedToShow += (error) => { OnAdError(currentPlacement, error); };
        manager.OnRewardedAdFailedToShow += (error) => { OnAdError(currentPlacement, error); };

        // Revenue
        manager.OnInterstitialAdOpening += (metadata) =>
        {
            if (metadata.priceAccuracy == PriceAccuracy.Undisclosed)
                Debug.Log("Begin impression " + metadata.type + " ads with undisclosed cost from " + metadata.network);
            else
                ReportRevenue(currentPlacement.placement, metadata.cpm / 1000, "USD");
        };

        manager.OnRewardedAdOpening += (metadata) =>
        {
            if (metadata.priceAccuracy == PriceAccuracy.Undisclosed)
                Debug.Log("Begin impression " + metadata.type + " ads with undisclosed cost from " + metadata.network);
            else
                ReportRevenue(currentPlacement.placement, metadata.cpm / 1000, "USD");
        };

        manager.OnAppReturnAdShown += () => Debug.Log("App return ad shown");
        manager.OnAppReturnAdFailedToShow += (error) => Debug.LogError(error);
        manager.OnAppReturnAdClicked += () => Debug.Log("App return ad clicked");
        manager.OnAppReturnAdClosed += () => Debug.Log("App return ad closed");

        manager.OnInterstitialAdShown += () => { OnAdOpen(currentPlacement); };
        manager.OnInterstitialAdClicked += () => { OnAdClicked(currentPlacement); };
        manager.OnInterstitialAdClosed += () => { OnAdClose(currentPlacement); };

        manager.OnRewardedAdShown += () => { OnAdOpen(currentPlacement); };
        manager.OnRewardedAdClicked += () => { OnAdClicked(currentPlacement); };
        manager.OnRewardedAdClosed += () => { OnAdClose(currentPlacement); };
        manager.OnRewardedAdCompleted += () => { OnAdReward(currentPlacement); };

        RequestAll();

        Debug.Log($"CAS SDK version:{MobileAds.GetSDKVersion()}");
#endif
    }

    public static void RemoveAds(bool enable)
    {
        RemovedAds = enable;

        HideAll();
        DestroyAll();

        if (enable)
            RequestAll();

#if CLEVERADS
        manager.SetAppReturnAdsEnabled(enable);
#endif

        Debug.Log("NOADS Remove ads: " + enable);
    }

    public static void SetConsent(bool consent)
    {
        setConsent = true;
        consentEnabled = consent;
    }

    public static void SkipInterstitial(bool skip)
    {
        skipInterstitial = skip;
    }

    public static void RequestAll()
    {
        if (!IsInitialized)
            return;

        foreach (var placement in instance.placements)
            placement.Request();
    }

    public static void Hide(string placement)
    {
        var p = GetPlacement(placement);
        p.Hide();
    }

    public static void HideAll()
    {
        foreach (var placement in instance.placements)
            placement.Hide();
    }

    public static void DestroyAll()
    {
        foreach (var placement in instance.placements)
            placement.Destroy();
    }

    [Serializable]
    public class Placement
    {
        public string placement;
        public string ironsourcePlacement;

        [SerializeField] string androidId;
        [SerializeField] string iosId;
        [SerializeField] bool isOpen;
        public bool IsOpen => isOpen;

        public Type type;

        public DateTime lastShow;

        public enum Type
        {
            Banner,
            Interstitial,
            Rewarded
        }

        public string Id
        {
#if UNITY_ANDROID
            get => androidId;
#endif

#if UNITY_IOS
            get => iosId;
#endif
        }

#if ADMOB
        BannerView banner;
        InterstitialAd interstitial;
        RewardedAd rewarded;
#endif

#if CLEVERADS
        IAdView bannerView;
        [SerializeField] AdSize bannerSize = AdSize.Banner;

        private bool collectBannerRevenue;
#endif

        private void CreateBanner()
        {
#if CLEVERADS
            if (type == Type.Banner && bannerView == null)
            {
                bannerView = instance.manager.GetAdView(bannerSize);

                bannerView.OnLoaded += (view) => 
                { 
                    collectBannerRevenue = true; 
                    instance.OnAdLoaded(this); 
                };

                bannerView.OnFailed += (view, error) => { instance.OnAdError(this, error.GetMessage()); };

                bannerView.OnPresented += (view, data) =>
                {
                    if (collectBannerRevenue) 
                    {
                        collectBannerRevenue = false;

                        instance.OnAdOpen(this);

                        if (data.priceAccuracy == PriceAccuracy.Undisclosed)
                            Debug.Log("Begin impression " + data.type + " ads with undisclosed cost from " + data.network);
                        else
                            instance.ReportRevenue(placement, data.cpm / 1000, "USD");
                    }
                };

                bannerView.OnClicked += (view) => { instance.OnAdClicked(this); };
                bannerView.OnHidden += (view) => { instance.OnAdClose(this); };

                bannerView.SetActive(false);
            }
#endif
        }

        public bool earnedReward;

        public bool IsDelayed()
        {
            switch (type)
            {
                case Type.Interstitial:
                    if ((DateTime.Now - LastInterstitialShow).TotalSeconds < instance.interstitialDelay)
                    {
                        Debug.Log($"OnAdDelayed: {placement} Last interstitial displayed {(DateTime.Now - LastInterstitialShow).TotalSeconds}sec ago. Timeout: {instance.interstitialDelay}sec");
                        return true;
                    }

                    if ((DateTime.Now - LastRewardedShow).TotalSeconds < instance.interstitialAfterRewardedDelay)
                    {
                        Debug.Log($"OnAdDelayed: {placement} Last rewarded displayed {(DateTime.Now - LastRewardedShow).TotalSeconds}sec ago. Timeout: {instance.interstitialAfterRewardedDelay}sec");
                        return true;
                    }

                    break;
                case Type.Rewarded:
                    if ((DateTime.Now - LastRewardedShow).TotalSeconds < instance.rewardedDelay)
                    {
                        Debug.Log($"OnAdDelayed: {placement} Last rewarded displayed {(DateTime.Now - LastRewardedShow).TotalSeconds}sec ago. Timeout: {instance.rewardedDelay}sec");
                        return true;
                    }
                    break;
            }

            return false;
        }

        public bool IsReady()
        {
            if (instance.skipAd.Any(x => x == type))
            {
                Debug.Log($"OnAdSkiped: {placement}, Skip ad types: {string.Join(',', instance.skipAd)}");
                return false;
            }

            CreateBanner();

            if (IsDelayed())
                return false;

#if ADMOB
            switch (type)
            {
                case Type.Banner:
                    return banner != null;
                case Type.Interstitial:
                    return interstitial != null ? interstitial.IsLoaded() : false;
                case Type.Rewarded:
                    return rewarded != null ? rewarded.IsLoaded() : false;
            }
#endif

#if !ADMOB && IRONSOURCE
            if(IronSource.Agent == null)
                    return false;

            switch (type) 
            {
                case Type.Banner:
                    return false;
                case Type.Interstitial:
                    return IronSource.Agent.isInterstitialReady();
                case Type.Rewarded:
                    return IronSource.Agent.isRewardedVideoAvailable();
            }
#endif

#if CLEVERADS
            switch (type)
            {
                case Type.Banner:
                    return bannerView.isReady;
                case Type.Interstitial:
                    return instance.manager.IsReadyAd(AdType.Interstitial);
                case Type.Rewarded:
                    return instance.manager.IsReadyAd(AdType.Rewarded);
            }
#endif

            return false;
        }

        public void Request()
        {
            if (IsReady())
                return;

#if ADMOB
            // Required for Vungle mediation < 3.1.0
            //if (type == Type.Interstitial)
            //{
            //    request = new AdRequest.Builder().AddMediationExtras(instance.vungleInterstitialExtras).Build();
            //}
            //else if (type == Type.Rewarded)
            //{
            //    request = new AdRequest.Builder().AddMediationExtras(instance.vungleRewaredVideoExtras).Build();
            //}

            AdRequest request = new AdRequest.Builder().Build();

            switch (type)
            {
                case Type.Banner:
                    if (RemovedAds)
                        return;

                    if (banner == null)
                    {
                        if (instance.isTest)
                            banner = new BannerView("ca-app-pub-3940256099942544/6300978111", AdSize.Banner, AdPosition.Bottom);
                        else
                            banner = new BannerView(Id, AdSize.Banner, AdPosition.Bottom);

                        banner.OnAdLoaded += (sender, args) => { Hide(); instance.OnAdLoaded(this); };
                        banner.OnAdFailedToLoad += (sender, args) => { instance.OnAdError(this, args.LoadAdError.GetMessage()); };
                        banner.OnAdOpening += (sender, args) => { instance.OnAdOpen(this); };
                        banner.OnAdClosed += (sender, args) => { instance.OnAdClose(this); };
                        banner.OnPaidEvent += (sender, args) => { OnPaid(Id, args.AdValue); };
                    }

                    banner.LoadAd(request);
                    break;
                case Type.Interstitial:
                    if (RemovedAds)
                        return;

                    if (interstitial == null)
                    {
                        if (instance.isTest)
                            interstitial = new InterstitialAd("ca-app-pub-3940256099942544/1033173712");
                        else
                            interstitial = new InterstitialAd(Id);

                        interstitial.OnAdLoaded += (sender, args) => { instance.OnAdLoaded(this); };
                        interstitial.OnAdFailedToLoad += (sender, args) => { instance.OnAdError(this, args.LoadAdError.GetMessage()); };
                        interstitial.OnAdFailedToShow += (sender, args) => { instance.OnAdError(this, args.AdError.GetMessage()); };
                        interstitial.OnAdOpening += (sender, args) => { instance.OnAdOpen(this); };
                        interstitial.OnAdClosed += (sender, args) => { instance.OnAdClose(this); };
                        interstitial.OnPaidEvent += (sender, args) => { OnPaid(Id, args.AdValue); };
                    }

                    interstitial.LoadAd(request);
                    break;
                case Type.Rewarded:
                    if (rewarded == null)
                    {
                        if (instance.isTest)
                            rewarded = new RewardedAd("ca-app-pub-3940256099942544/5224354917");
                        else
                            rewarded = new RewardedAd(Id);

                        rewarded.OnAdLoaded += (sender, args) => { instance.OnAdLoaded(this); };
                        rewarded.OnAdOpening += (sender, args) => { instance.OnAdOpen(this); };
                        rewarded.OnAdFailedToLoad += (sender, args) => { instance.OnAdError(this, args.LoadAdError.GetMessage()); };
                        rewarded.OnAdFailedToShow += (sender, args) => { instance.OnAdError(this, args.AdError.GetMessage()); };
                        rewarded.OnUserEarnedReward += (sender, args) => { instance.OnAdReward(this); };
                        rewarded.OnAdClosed += (sender, args) => { instance.OnAdClose(this); };
                        rewarded.OnPaidEvent += (sender, args) => { OnPaid(Id, args.AdValue); };
                    }

                    rewarded.LoadAd(request);
                    break;
            }
#endif


#if !ADMOB && IRONSOURCE
            switch (type) 
            {
                case Type.Banner:
                    IronSource.Agent.loadBanner(IronSourceBannerSize.BANNER, IronSourceBannerPosition.BOTTOM);
                    break;
                case Type.Interstitial:
                    IronSource.Agent.loadInterstitial();
                    break;
                case Type.Rewarded:
                    break;
            }

#endif
        }

        public void Show(Action callback = null)
        {
            if (RemovedAds && type != Type.Rewarded)
                return;

            if (!IsReady())
                return;

            instance.currentPlacement = this;

            earnedReward = false;

#if ADMOB
            switch (type)
            {
                case Type.Banner:
                    if (RemovedAds)
                        return;

                    banner.Show();
                    break;
                case Type.Interstitial:
                    if (RemovedAds)
                        return;

                    interstitial.Show();
                    break;
                case Type.Rewarded:
                    earnedReward = false;
                    rewarded.Show();
                    break;
            }
#endif


#if !ADMOB && IRONSOURCE
            switch (type)
            {
                case Type.Banner:
                    IronSource.Agent.displayBanner();
                    break;
                case Type.Interstitial:
                    IronSource.Agent.showInterstitial(ironsourcePlacement);
                    break;
                case Type.Rewarded:
                    IronSource.Agent.showRewardedVideo(ironsourcePlacement);
                    break;
            }
#endif

#if CLEVERADS
            switch (type)
            {
                case Type.Banner:
                    bannerView.SetActive(true);
                    break;
                case Type.Interstitial:
                    instance.manager.ShowAd(AdType.Interstitial);
                    break;
                case Type.Rewarded:
                    earnedReward = false;
                    instance.manager.ShowAd(AdType.Rewarded);
                    break;
            }
#endif

            if (type == Type.Banner)
                isOpen = true;

            callback?.Invoke();
        }

        public void Hide()
        {
#if ADMOB
            switch (type)
            {
                case Type.Banner:
                    banner?.Hide();
                    break;
            }
#endif


#if !ADMOB && IRONSOURCE
            switch (type)
            {
                case Type.Banner:
                    IronSource.Agent.hideBanner();
                    break;
            }
#endif

#if CLEVERADS
            switch (type)
            {
                case Type.Banner:
                    bannerView.SetActive(false);
                    break;
                
            }
#endif

            if (type == Type.Banner)
                isOpen = false;
        }

        public void Destroy()
        {
#if ADMOB
            banner?.Destroy();
            interstitial?.Destroy();
#endif


#if !ADMOB && IRONSOURCE
            IronSource.Agent.destroyBanner();
#endif
        }

#if ADMOB
        private void OnPaid(string paidAdUnit, AdValue paidAdValue)
        {
            instance.ReportRevenue(paidAdUnit, paidAdValue.Value / 1000000f, paidAdValue.CurrencyCode);
        }
#endif
    }

    private void ReportRevenue(string adUnit, double value, string currency)
    {
        //var revenue = new YandexAppMetricaRevenue((decimal)value, currency);
        //
        //revenue.ProductID = adUnit;
        //
        //AppMetrica.Instance.ReportRevenue(revenue);
        //FirebaseManager.ReportRevenue(adUnit, value, currency);
        //FacebookManager.ReportRevenue(adUnit, value, currency);
        //
        //Debug.Log($"Report revenue AdUnit: {adUnit} Value: {value} Currency: {currency}");
    }

    private void OnAdError(Placement placement, string errorMessage)
    {
        showAdResult = false;

        Debug.LogWarning($"OnAdError: {placement.placement} {errorMessage}");
        OnError?.Invoke(placement);

#if GAMEANALYTICS
		GameAnalytics.NewAdEvent(GAAdAction.FailedShow, GAAdType.RewardedVideo, gameAnalyticsSDKName, currentPlacement);
#endif
    }

    private void OnAdLoaded(Placement placement)
    {
        if (placement.type == Placement.Type.Banner)
            placement.Show();

        Debug.Log($"OnAdLoaded: {placement.placement}");
        OnLoaded?.Invoke(placement);

        if (RemovedAds && placement.type != Placement.Type.Rewarded)
            placement.Destroy();
    }

    private void OnAdOpen(Placement placement)
    {
        showAdResult = true;

        switch (placement.type)
        {
            case Placement.Type.Interstitial:
                LastInterstitialShow = DateTime.Now;
                break;
            case Placement.Type.Rewarded:
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

        if (placement.type == Placement.Type.Rewarded)
        {
            StopCoroutine("GetReward");
            StartCoroutine("GetReward", placement);
        }

        waitForShowAd = false;

        RequestAll();

#if GAMEANALYTICS
		long elapsedTime = GameAnalytics.StopTimer(currentPlacement);

		GameAnalytics.NewAdEvent(GAAdAction.Show, GAAdType.RewardedVideo, gameAnalyticsSDKName, currentPlacement, elapsedTime);
#endif
    }

    WaitForSeconds checkRewardDelay = new WaitForSeconds(0.5f);

    IEnumerator GetReward(Placement placement)
    {
        yield return checkRewardDelay;

        if (placement.earnedReward) OnRewarded?.Invoke(placement);
        else OnRewardedFailed?.Invoke(placement);
    }

    private void OnAdReward(Placement placement)
    {
        Debug.Log($"OnAdReward: {placement.placement}");
        placement.earnedReward = true;
        waitForEarnReward = false;

#if GAMEANALYTICS
		GameAnalytics.NewAdEvent(GAAdAction.RewardReceived, GAAdType.RewardedVideo, gameAnalyticsSDKName, currentPlacement);
#endif
    }
}
