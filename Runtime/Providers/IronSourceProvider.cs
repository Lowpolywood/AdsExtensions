#if SIRONSOURCE
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace AdsExtensions.Providers
{
    public class IronSourceProvider : AdProvider
    {
        public void Initialize(bool isTest, bool consent, string appOpen, string banner, string interstitial, string rewarded)
        {
            base.Initialize(isTest, consent, null, appOpen, banner, interstitial, rewarded, null, null);

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
        }

        public override bool IsReady(AdType type)
        {
            switch (type)
            {
                case AdType.Banner:
                    return false;
                case AdType.Interstitial:
                    return IronSource.Agent.isInterstitialReady();
                case AdType.Rewarded:
                    return IronSource.Agent.isRewardedVideoAvailable();
                default:
                    Debug.Log($"{type} ad not implemented in IronSource!");
                    return false;
            }
        }

        public override void Hide(AdType type)
        {
            switch (type)
            {
                case Type.Banner:
                    IronSource.Agent.hideBanner();
                    break;
            }
        }

        public override void Destroy(AdType type)
        {
            IronSource.Agent.destroyBanner();
        }

        public override void Request(AdType type)
        {
            switch (type)
            {
                case AdType.Banner:
                    IronSource.Agent.loadBanner(IronSourceBannerSize.BANNER, IronSourceBannerPosition.BOTTOM);
                    break;
                case AdType.Interstitial:
                    IronSource.Agent.loadInterstitial();
                    break;
                case AdType.Rewarded:
                    break;
            }
        }

        public override void Show(AdType type)
        {
            switch (type)
            {
                case AdType.Banner:
                    IronSource.Agent.displayBanner();
                    break;
                case AdType.Interstitial:
                    IronSource.Agent.showInterstitial(ironsourcePlacement);
                    break;
                case AdType.Rewarded:
                    IronSource.Agent.showRewardedVideo(ironsourcePlacement);
                    break;
            }
        }

        public override void OnPause(bool pause)
        {
            IronSource.Agent.onApplicationPause(pause);
        }
    }
}
#endif